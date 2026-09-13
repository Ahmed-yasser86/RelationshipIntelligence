import { expect, test } from "@playwright/test";
import { API_URL, expectSignedIn, loginViaApi } from "./helpers";

test.describe("co-pilot", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await expectSignedIn(page);
  });

  test("provider settings save without ever revealing the key", async ({ page }) => {
    await page.getByRole("button", { name: "Ask co-pilot" }).click();
    await page.getByRole("button", { name: "Setup" }).click();
    await page.getByLabel("Model").fill("gemini-3.1-flash-lite");
    await page.getByLabel(/API key/).fill("test-key-not-real");
    await page.getByRole("button", { name: "Save provider" }).click();
    await expect(page.getByText("(key saved)")).toBeVisible();
    await expect(page.getByLabel(/API key/)).toHaveValue("");
  });

  test("ask returns a grounded answer with citations", async ({ page }) => {
    await page.getByRole("button", { name: "Ask co-pilot" }).click();
    await page.getByLabel("Ask the co-pilot").fill("Who should I follow up with this week?");
    await page.getByRole("button", { name: "Send", exact: true }).click();
    await expect(page.getByText(/\[Observed\]/).first()).toBeVisible();
  });

  test("person-scoped ask cites the person", async ({ page }) => {
    await page.getByRole("link", { name: "People", exact: true }).click();
    await page.getByLabel("Search").fill("Salma");
    await page.getByRole("button", { name: "Apply" }).click();
    await page.locator("ul > li", { hasText: "Salma" }).locator("a").first().click();
    await page.locator("main").getByRole("button", { name: "Ask co-pilot" }).click();
    await page.getByLabel("Ask the co-pilot").fill("What happened here?");
    await page.getByRole("button", { name: "Send", exact: true }).click();
    await expect(page.getByRole("link", { name: "Salma El-Sayed" })).toBeVisible();
  });

  test("meeting follow-up questions cite recorded commitments", async ({ page }) => {
    await page.getByRole("link", { name: "People", exact: true }).click();
    await page.getByLabel("Search").fill("Salma");
    await page.getByRole("button", { name: "Apply" }).click();
    await page.locator("ul > li", { hasText: "Salma" }).locator("a").first().click();
    await page.locator("main").getByRole("button", { name: "Ask co-pilot" }).click();
    await page.getByLabel("Ask the co-pilot").fill("What did I promise Salma?");
    await page.getByRole("button", { name: "Send", exact: true }).click();
    await expect(page.getByText(/crit notes/i).first()).toBeVisible();
  });

  test("briefing block renders on the overview", async ({ page }) => {
    await expect(page.getByRole("heading", { name: "Today's briefing" })).toBeVisible();
  });

  test("natural-language outreach resolves to signal filters, never people", async ({ page, request }) => {
    const token = await page.evaluate(() => localStorage.getItem("ri.token"));
    const utterances = [
      "Prepare messages for everyone I should reconnect with this week.",
      "Who should I follow up with after last week's meetings?",
      "Prepare short LinkedIn messages for the people I've neglected recently.",
      "I want to reconnect with everyone in my attention queue. Use email.",
    ];
    for (const text of utterances) {
      const res = await request.post(`${API_URL}/api/Copilot/PostParseOutreachIntent`, {
        headers: { Authorization: `Bearer ${token}` },
        data: { Text: text },
      });
      expect(res.ok()).toBeTruthy();
      const body = await res.json();
      expect(body.signalFilters.length).toBeGreaterThan(0);
      expect(JSON.stringify(body).toLowerCase()).not.toContain("personid");
    }
    const linked = await request.post(`${API_URL}/api/Copilot/PostParseOutreachIntent`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { Text: "Prepare short LinkedIn messages for the people I've neglected recently." },
    });
    expect((await linked.json()).channel).toBe("LinkedIn");
  });
});
