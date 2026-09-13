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

  test("explains a score with observed-derived-result evidence", async ({
    page,
  }) => {
    await page.locator("ol > li").first().getByRole("button", { name: "Explain" }).click();
    await expect(page.getByText("Observed", { exact: true })).toBeVisible();
    await expect(page.getByText("Derived", { exact: true })).toBeVisible();
    await expect(page.getByText("Result", { exact: true })).toBeVisible();
    await expect(page.getByText(/typical rhythm/i).first()).toBeVisible();
    await expect(page.getByText("Silence against typical rhythm")).toBeVisible();
    await expect(page.getByText("Silence against own history")).toBeVisible();
  });

  test("snoozes an item out of the queue", async ({ page }) => {
    const rows = page.locator("ol > li");
    await expect(rows.first()).toBeVisible();
    const before = await rows.count();
    const name = await rows.first().locator("a").first().innerText();
    await rows.first().getByRole("button", { name: "Snooze" }).click();
    await expect(page.locator("ol > li")).toHaveCount(before - 1);
    await expect(page.getByRole("heading", { name: /Snoozed \(1\)/ })).toBeVisible();
    await expect(page.getByText(new RegExp(`Back in the ranking in ~\\d+d`))).toBeVisible();
    await page.getByRole("button", { name: "Unsnooze" }).click();
    await expect(page.locator("ol > li")).toHaveCount(before);
    await expect(page.locator("ol > li").first().locator("a").first()).toContainText(name);
  });

  test("show more loads beyond the top 7", async ({ page }) => {
    await expect(page.locator("ol > li")).toHaveCount(7);
    await page.getByRole("button", { name: "Show more" }).click();
    await expect
      .poll(async () => page.locator("ol > li").count(), { timeout: 20000 })
      .toBeGreaterThan(7);
  });
});
