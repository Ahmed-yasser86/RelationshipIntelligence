import { expect, test } from "@playwright/test";
import { API_URL, expectSignedIn, loginViaApi } from "./helpers";

test.describe("network", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await page.getByRole("link", { name: "Network", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Network" })).toBeVisible();
  });

  test("renders shared-organization edges and selectable nodes", async ({
    page,
    request,
  }) => {
    const org = `E2E Graph Org ${Date.now()}`;
    const token = await page.evaluate(() => localStorage.getItem("ri.token"));
    const headers = { Authorization: `Bearer ${token}` };
    const created: string[] = [];
    for (const name of [`E2E Graph A ${Date.now()}`, `E2E Graph B ${Date.now()}`]) {
      const res = await request.post(`${API_URL}/api/Contacts/PostQuickAddContact`, {
        headers,
        data: { Name: name, email: `${name.replace(/\W/g, "")}@example.com`, Organizations: [org] },
      });
      expect(res.ok()).toBeTruthy();
      created.push(((await res.json()) as { personId: string }).personId);
    }

    try {
      await page.reload();
      const svg = page.locator('svg[aria-label="Contact network graph"]');
      await expect(svg).toBeVisible();
      await page.getByLabel("Find person").fill("E2E Graph A");
      await page.getByRole("button", { name: /E2E Graph A/ }).click();
      const panel = page.locator("aside");
      await expect(panel.getByText(/E2E Graph A/).first()).toBeVisible();
      await expect(panel.getByText(/connections/)).toBeVisible();
    } finally {
      for (const id of created) {
        await request.post(`${API_URL}/api/Contacts/DeletePersoneObject?id=${id}`, {
          headers,
        });
      }
    }
  });
});

test.describe("digest", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await page.getByRole("link", { name: "Digest", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Weekly digest" })).toBeVisible();
  });

  test("shows the weekly selection and preferences", async ({ page }) => {
    await expect(
      page.getByText(/relationships to protect|quiet week/i).first(),
    ).toBeVisible();
    await expect(page.getByLabel("Minimum urgency")).toBeVisible();
    await expect(page.getByLabel("Contacts per digest")).toBeVisible();
  });

  test("reviews the exact email before sending", async ({ page }) => {
    await page.getByRole("button", { name: "Review email" }).click();
    await expect(page.getByText("Email preview")).toBeVisible();
    await expect(page.getByText(/^To$/)).toBeVisible();
    await expect(page.getByText(/^Subject$/)).toBeVisible();
    await page.getByRole("button", { name: "Cancel" }).click();
    await expect(page.getByRole("button", { name: "Review email" })).toBeVisible();
  });

  test("saves preferences", async ({ page }) => {
    await page.getByLabel("Contacts per digest").fill("3");
    await page.getByRole("button", { name: "Save" }).click();
    await expect(page.getByText("Saved.")).toBeVisible();
  });
});
