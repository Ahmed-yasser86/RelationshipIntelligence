import { expect, test } from "@playwright/test";
import { expectSignedIn, loginViaApi } from "./helpers";

test.describe("honest terminology", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await expectSignedIn(page);
  });

  test("network explains shared context and articulation points", async ({ page }) => {
    await page.getByRole("link", { name: "Network", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Network" })).toBeVisible();
    await expect(page.getByText(/shared context/i).first()).toBeVisible();
    await expect(page.getByText(/articulation points/i).first()).toBeVisible();
    await expect(page.getByText(/not detected communities/i)).toBeVisible();
    await expect(page.getByText(/not observed contact/i)).toBeVisible();
    await expect(page.getByText(/communit/i)).toHaveCount(1);
  });

  test("overview uses shared-context groups", async ({ page }) => {
    await expect(page.getByText(/shared-context groups/i).first()).toBeVisible();
    await expect(page.getByText(/articulation points/i).first()).toBeVisible();
  });

  test("person detail explains the bridge flag", async ({ page }) => {
    await page.getByRole("link", { name: "People", exact: true }).click();
    await page.getByLabel("Search").fill("Mohamed");
    await page.getByRole("button", { name: "Apply" }).click();
    await page.locator("ul > li", { hasText: "Mohamed" }).locator("a").first().click();
    await expect(page.getByRole("heading", { level: 1 })).toContainText("Mohamed");
    await expect(page.getByTitle(/articulation point/i).first()).toBeVisible();
  });
});
