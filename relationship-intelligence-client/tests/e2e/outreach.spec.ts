import { expect, test } from "@playwright/test";
import { API_URL, expectSignedIn, loginViaApi } from "./helpers";

test.describe("outreach batches", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await expectSignedIn(page);
    await page.getByRole("link", { name: "Outreach", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Outreach" })).toBeVisible();
  });

  test("builds from a natural-language request, reviews, and approves", async ({ page }) => {
    await page.getByLabel("Describe who to reach").fill("I want to reconnect with everyone in my attention queue. Use email.");
    await page.getByRole("button", { name: "Build batch" }).first().click();
    await expect(page.getByRole("heading", { name: "1 · Who to contact" })).toBeVisible();
    await expect(page.locator("ul > li").first()).toContainText("—");

    await page.getByRole("button", { name: /Prepare \d+ personalized drafts?/ }).click();
    await expect(page.getByText("Grounded in:").first()).toBeVisible();

    const bodies = await page.locator("li p.whitespace-pre-wrap").allTextContents();
    expect(bodies.length).toBeGreaterThan(1);
    for (const body of bodies) {
      expect(body).not.toContain("[");
      expect(body).not.toMatch(/\d{4}-\d{2}-\d{2}/);
      expect(body.toLowerCase()).not.toContain("urgency");
    }
    expect(new Set(bodies.map((b) => b.trim())).size).toBeGreaterThan(1);

    await page.getByRole("button", { name: /Approve all reviewed/ }).click();
    await expect(page.getByText(/approved\. Nothing was sent/i)).toBeVisible();
  });

  test("call preparation produces before-during-after briefs, not messages", async ({ page }) => {
    await page.getByLabel("Describe who to reach").fill("I want to reconnect with everyone in my attention queue.");
    await page.getByRole("button", { name: "Build batch" }).first().click();
    await expect(page.getByRole("heading", { name: "1 · Who to contact" })).toBeVisible();
    await page.getByLabel("Channel").click();
    await page.getByRole("option", { name: "Prepare calls" }).click();
    await page.getByRole("button", { name: "Save settings" }).click();
    await page.getByRole("button", { name: /Prepare \d+ personalized drafts?/ }).click();
    await expect(page.getByText("Call prep").first()).toBeVisible();
    await expect(page.getByText("Before the call:").first()).toBeVisible();
    await expect(page.getByText("During the call:").first()).toBeVisible();
    await expect(page.getByText("After the call:").first()).toBeVisible();
  });

  test("member override changes one draft channel", async ({ page }) => {
    await page.getByLabel("Describe who to reach").fill("Who should I follow up with after last week's meetings?");
    await page.getByRole("button", { name: "Build batch" }).first().click();
    await expect(page.getByRole("heading", { name: "1 · Who to contact" })).toBeVisible();

    const first = page.locator("ul > li").first();
    await first.getByRole("button", { name: "Options" }).click();
    await first.getByLabel("Channel for this person").click();
    await page.getByRole("option", { name: "LinkedIn" }).click();
    await expect(first.getByText("via LinkedIn")).toBeVisible();
  });

  test("attention queue selection starts a batch", async ({ page, request }) => {
    await page.getByRole("link", { name: "Attention", exact: true }).click();
    await page.getByRole("checkbox").first().check();
    await page.getByRole("button", { name: /Contact selected/ }).click();
    await expect(page.getByRole("heading", { name: "1 · Who to contact" })).toBeVisible();

    const token = await page.evaluate(() => localStorage.getItem("ri.token"));
    const me = await request.get(`${API_URL}/api/Contacts/GetRelationshipQueue?top=1`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    const before = await me.json();
    const urgencyBefore = before[0].urgencyScore;
    expect(urgencyBefore).toBeGreaterThan(0);

    await page.getByRole("button", { name: /Prepare \d+ personalized drafts?/ }).click();
    await page.getByRole("button", { name: /Approve all reviewed/ }).click();
    await expect(page.getByText(/approved\. Nothing was sent/i)).toBeVisible();

    const after = await request.get(`${API_URL}/api/Contacts/GetRelationshipQueue?top=50`, {
      headers: { Authorization: `Bearer ${token}` },
    });
    const afterJson = await after.json();
    const same = afterJson.find((q: { personId: string }) => q.personId === before[0].personId);
    expect(same.urgencyScore).toBe(urgencyBefore);
  });
});
