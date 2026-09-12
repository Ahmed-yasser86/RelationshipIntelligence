import { expect, test } from "@playwright/test";
import { TEST_EMAIL, TEST_PASSWORD, expectSignedIn, loginViaApi } from "./helpers";

test.describe("auth", () => {
  test("redirects anonymous visitors to login", async ({ page }) => {
    await page.goto("/");
    await expect(page).toHaveURL(/\/login$/);
    await expect(page.getByRole("heading", { name: /relationship intelligence/i })).toBeVisible();
  });

  test("rejects wrong credentials with an error", async ({ page }) => {
    await page.goto("/login");
    await page.getByLabel("Email").fill(TEST_EMAIL);
    await page.getByLabel("Password").fill("wrong-password");
    await page.getByRole("button", { name: "Sign in" }).click();
    await expect(page.getByText(/Invalid email or password/)).toBeVisible();
    await expect(page).toHaveURL(/\/login$/);
  });

  test("signs in through the UI and lands on the queue", async ({ page }) => {
    await page.goto("/login");
    await page.getByLabel("Email").fill(TEST_EMAIL);
    await page.getByLabel("Password").fill(TEST_PASSWORD);
    await page.evaluate(() => localStorage.setItem("ri.onboarded", "1"));
    await page.getByRole("button", { name: "Sign in" }).click();
    await expectSignedIn(page);
  });

  test("register validates matching passwords", async ({ page }) => {
    await page.goto("/register");
    await page.getByLabel("Name").fill("E2E Probe");
    await page.getByLabel("Email").fill("probe@example.com");
    await page.getByLabel("Phone (digits only)").fill("123456");
    await page.getByLabel("Password", { exact: true }).fill("Secret123!");
    await page.getByLabel("Confirm password").fill("Different123!");
    await page.getByRole("button", { name: "Create account" }).click();
    await expect(page.getByText("Passwords do not match.")).toBeVisible();
  });

  test("signs out back to login", async ({ page }) => {
    await loginViaApi(page);
    await expectSignedIn(page);
    await page.getByRole("button", { name: "Sign out" }).click();
    await expect(page).toHaveURL(/\/login$/);
  });
});
