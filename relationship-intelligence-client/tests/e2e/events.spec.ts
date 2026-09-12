import { expect, test } from "@playwright/test";
import { expectSignedIn, loginViaApi } from "./helpers";

test.describe("relationship events", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await expectSignedIn(page);
    await page.getByRole("link", { name: "People", exact: true }).click();
    await page.getByLabel("Search").fill("Dina");
    await page.getByRole("button", { name: "Apply" }).click();
    await page.locator("ul > li", { hasText: "Dina" }).locator("a").first().click();
    await expect(page.getByRole("heading", { level: 1 })).toContainText("Dina");
  });

  test("creates, edits, and deletes an event", async ({ page }) => {
    const title = `E2E Milestone ${Date.now()}`;
    await page.locator("section").filter({ hasText: "Important events" }).getByRole("button", { name: "Add" }).click();
    await page.getByLabel("Title").fill(title);
    await page.getByLabel("Date").fill("2026-12-20");
    await page.getByRole("button", { name: "Add event" }).click();
    await expect(page.getByText(title)).toBeVisible();

    await page.getByRole("listitem").filter({ hasText: title }).getByRole("button", { name: "Edit" }).click();
    await page.getByLabel("Title").fill(`${title} updated`);
    await page.getByRole("button", { name: "Save changes" }).click();
    await expect(page.getByText(`${title} updated`)).toBeVisible();

    page.once("dialog", (d) => void d.accept());
    await page.getByRole("listitem").filter({ hasText: `${title} updated` }).getByRole("button", { name: "Delete" }).click();
    await expect(page.getByText(`${title} updated`)).toHaveCount(0);
  });

  test("upcoming events surface on the overview", async ({ page }) => {
    await page.getByRole("link", { name: "Overview", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Coming up" })).toBeVisible();
  });
});
