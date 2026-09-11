import { expect, test } from "@playwright/test";
import { API_URL, expectSignedIn, loginViaApi } from "./helpers";

test.describe("network", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await page.getByRole("link", { name: "Network" }).click();
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
      const node = svg.getByText(/E2E Graph A/);
      await expect(node).toBeVisible();
      await node.click();
      const panel = page.locator("aside");
      await expect(panel.getByText(/E2E Graph A/)).toBeVisible();
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
    await page.getByRole("link", { name: "Digest" }).click();
    await expect(page.getByRole("heading", { name: "Weekly digest" })).toBeVisible();
  });

  test("shows the weekly selection and preferences", async ({ page }) => {
    await expect(
      page.getByText(/relationships to protect|quiet week/i).first(),
    ).toBeVisible();
    await expect(page.getByLabel("Minimum urgency")).toBeVisible();
    await expect(page.getByLabel("Contacts per digest")).toBeVisible();
  });

  test("saves preferences", async ({ page }) => {
    await page.getByLabel("Contacts per digest").fill("3");
    await page.getByRole("button", { name: "Save" }).click();
    await expect(page.getByText("Saved.")).toBeVisible();
  });
});
