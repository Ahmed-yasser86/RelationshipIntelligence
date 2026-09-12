import { expect, test } from "@playwright/test";
import { expectSignedIn, loginViaApi } from "./helpers";

test.describe("overview", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await expectSignedIn(page);
  });

  test("explains the value proposition and shows a live snapshot", async ({
    page,
  }) => {
    await expect(
      page.getByText(/relationship intelligence.*not a contact manager/i),
    ).toBeVisible();
    await expect(page.getByText(/This week in your network/)).toBeVisible();
    await expect(page.getByText(/people ·/)).toBeVisible();
  });

  test("navigates to attention, network, and digest", async ({ page }) => {
    await page.getByRole("link", { name: "Open the attention queue" }).click();
    await expect(page.getByRole("heading", { name: "Needs attention" })).toBeVisible();
    await page.getByRole("link", { name: "Network", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Network" })).toBeVisible();
    await page.getByRole("link", { name: "Digest", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Weekly digest" })).toBeVisible();
  });
});
