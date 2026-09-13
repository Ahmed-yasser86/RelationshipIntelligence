import { expect, test } from "@playwright/test";
import { API_URL, expectSignedIn, loginViaApi } from "./helpers";

const PROBE = `E2E Probe ${Date.now()}`;

test.describe("person workflows", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await expectSignedIn(page);
  });

  test("quick-adds a person, logs an interaction, edits, and deletes", async ({
    page,
    request,
  }) => {
    // quick add
    await page.getByRole("link", { name: "People", exact: true }).click();
    await page.getByRole("link", { name: "Add person" }).click();
    await page.getByLabel("Name").first().fill(PROBE);
    await page.getByLabel("Email").fill(`probe-${Date.now()}@example.com`);
    await page.getByRole("button", { name: "Add person" }).click();
    await expect(page).toHaveURL(/\/people\/[0-9a-f-]{36}/);
    await expect(page.getByRole("heading", { level: 1 })).toContainText(PROBE);

    // log interaction
    await page.locator("main").getByRole("button", { name: "Log interaction" }).click();
    await page.getByLabel("What was it about").fill("E2E verification call");
    await page.getByRole("button", { name: "Save interaction" }).click();
    const historySection = page.locator("section", { has: page.getByRole("heading", { name: "History", exact: true }) });
    await expect(historySection.getByText("E2E verification call")).toBeVisible();

    // csv import validation error surfaces
    await page.getByRole("button", { name: "Import CSV" }).click();
    await page.getByPlaceholder(/Kickoff/).fill("not,a-valid-row");
    await page.getByRole("button", { name: "Import" }).click();
    await expect(page.getByText(/expected at least date, type and title/i)).toBeVisible();
    await page.keyboard.press("Escape");

    // edit
    await page.getByRole("link", { name: "Edit", exact: true }).click();
    await page.getByLabel("Address").fill("Cairo, EG");
    await page.locator("#e-status").click();
    await page.getByRole("option", { name: "High Priority" }).click();
    await page.getByRole("button", { name: "Save changes" }).click();
    await expect(page.getByText("Cairo, EG")).toBeVisible();
    await expect(page.getByText("High Priority", { exact: true }).first()).toBeVisible();

    // delete with cleanup verification via API
    const personId = page.url().split("/").pop()!;
    page.once("dialog", (d) => void d.accept());
    await page.getByRole("button", { name: "Delete" }).click();
    await expect(page).toHaveURL(/\/people$/);
    const token = await page.evaluate(() => localStorage.getItem("ri.token"));
    const check = await request.get(
      `${API_URL}/api/Contacts/GetContactByContactID?id=${personId}`,
      { headers: { Authorization: `Bearer ${token}` } },
    );
    expect([403, 404]).toContain(check.status());
  });

  test("full add form creates a contact", async ({ page }) => {
    await page.goto("/people/new");
    await page.getByRole("tab", { name: "Full profile" }).click();
    await page.getByLabel("Name", { exact: true }).fill(`E2E Full ${Date.now()}`);
    await page.getByLabel("Email", { exact: true }).fill(`full-${Date.now()}@example.com`);
    await page.locator("#f-gender").click();
    await page.getByRole("option", { name: "Male", exact: true }).click();
    await page.getByLabel("Date of birth").fill("1990-05-01");
    await page.locator("#f-country").click();
    await page.getByRole("option").first().click();
    await page.locator("#f-status").click();
    await page.getByRole("option", { name: "Follow Up" }).click();
    await page.getByRole("button", { name: "Add person", exact: true }).last().click();
    await expect(page).toHaveURL(/\/people\/[0-9a-f-]{36}/);
    await expect(page.getByText("Follow Up", { exact: true }).first()).toBeVisible();
  });

  test("next step drafts outreach and prepares meetings in place", async ({ page }) => {
    await page.goto("/people");
    await page.locator("ul > li a").first().click();
    await expect(page.getByText("Consider next")).toBeVisible();
    await page.getByRole("button", { name: "Draft a message" }).click();
    await expect(page.getByRole("heading", { name: "1 · Who to contact" })).toBeVisible();
    await page.goBack();
    await expect(page.getByText("Consider next")).toBeVisible();
    await page.getByRole("button", { name: "Prepare a meeting" }).click();
    await expect(page.getByText("Preparation", { exact: true }).first()).toBeVisible();
  });

  test("person detail tells the relationship story", async ({ page }) => {
    await page.goto("/people");
    await page.locator("ul > li a").first().click();
    await expect(page.getByRole("heading", { name: "The story so far" })).toBeVisible();
    await expect(page.getByText("Relationship context")).toBeVisible();
    await expect(page.getByText("Important events")).toBeVisible();
    await page.getByRole("button", { name: "Plan relationship" }).click();
    await expect(page.getByText(/Advisory only/i)).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(page.getByRole("heading", { name: "History", exact: true })).toBeVisible();
    await expect(page.getByRole("heading", { name: "Trajectory" })).toBeVisible();
    await expect(
      page.getByRole("heading", { name: "Network position" }),
    ).toBeVisible();
  });
});
