import { expect, test } from "@playwright/test";
import { expectSignedIn, loginViaApi } from "./helpers";

test.describe("attention queue", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await expectSignedIn(page);
    await page.getByRole("link", { name: "Attention", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Needs attention" })).toBeVisible();
  });

  test("shows ranked queue with evidence", async ({ page }) => {
    const items = page.locator("ol > li");
    await expect(items.first()).toBeVisible();
    await expect(items).toHaveCount(7);
    await expect(items.first()).toContainText(/Your rhythm:/);
    await expect(
      items.first().getByText(/Healthy|Drifting|At Risk|Critical/),
    ).toBeVisible();
  });

  test("queue entry links into the person detail", async ({ page }) => {
    const firstLink = page.locator("ol > li a").first();
    await firstLink.click();
    await expect(page).toHaveURL(/\/people\/.+/);
    await expect(page.getByRole("heading", { level: 1 })).toBeVisible();
  });

  test("refresh reloads the queue", async ({ page }) => {
    await page.getByRole("button", { name: "Refresh" }).click();
    await expect(page.locator("ol > li").first()).toBeVisible();
  });
});
