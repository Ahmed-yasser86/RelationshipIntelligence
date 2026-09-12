import { expect, test } from "@playwright/test";
import { expectSignedIn, loginViaApi } from "./helpers";

test.describe("relationship memory", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await expectSignedIn(page);
    await page.getByRole("link", { name: "People", exact: true }).click();
    await page.getByLabel("Search").fill("Salma");
    await page.getByRole("button", { name: "Apply" }).click();
    await page.locator("ul > li", { hasText: "Salma" }).locator("a").first().click();
    await expect(page.getByRole("heading", { level: 1 })).toContainText("Salma");
  });

  test("creates, edits, completes, and deletes a memory entry", async ({ page }) => {
    const title = `E2E Commitment ${Date.now()}`;
    await page.getByRole("button", { name: "Add", exact: true }).first().click();
    await page.getByLabel("Title").fill(title);
    await page.getByLabel("Detail (optional)").fill("Promised during the e2e run");
    await page.getByRole("button", { name: "Save", exact: true }).click();
    const row = page.getByRole("listitem").filter({ hasText: title });
    await expect(row).toBeVisible();
    await expect(row.getByText("Your note")).toBeVisible();

    await page.getByRole("listitem").filter({ hasText: title }).getByRole("button", { name: "Edit" }).click();
    await page.getByLabel("Memory title").fill(`${title} updated`);
    await page.getByLabel("Memory status").click();
    await page.getByRole("option", { name: "Done" }).click();
    await page.getByRole("button", { name: "Save", exact: true }).click();
    await expect(page.getByText(`${title} updated`)).toBeVisible();
    await expect(page.getByText("Done", { exact: true })).toBeVisible();

    page.once("dialog", (d) => void d.accept());
    await page.getByRole("listitem").filter({ hasText: `${title} updated` }).getByRole("button", { name: "Delete" }).click();
    await expect(page.getByText(`${title} updated`)).toHaveCount(0);
  });
});
