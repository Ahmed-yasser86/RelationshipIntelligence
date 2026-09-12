import { expect, test } from "@playwright/test";
import { expectSignedIn, loginViaApi } from "./helpers";

test.describe("organizations", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await expectSignedIn(page);
    await page.getByRole("link", { name: "Organizations", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Organizations" })).toBeVisible();
  });

  test("lists organizations with member counts linking to filtered people", async ({
    page,
  }) => {
    await expect(page.getByRole("row", { name: /Proceedit/ })).toBeVisible();
    await page.getByRole("row", { name: /Proceedit/ }).getByRole("link").click();
    await expect(page.getByRole("heading", { name: "People" })).toBeVisible();
    await expect(page.getByLabel("Organization")).toHaveValue("Proceedit");
    await expect(page.locator("ul > li").first()).toBeVisible();
  });

  test("creates, renames, and deletes an organization", async ({ page }) => {
    const name = `E2E Org ${Date.now()}`;
    await page.getByLabel("New organization").fill(name);
    await page.getByRole("button", { name: "Add", exact: true }).click();
    await expect(page.getByRole("row", { name })).toBeVisible();

    await page.getByRole("row", { name }).getByRole("button", { name: "Rename" }).click();
    const renamed = `${name} Renamed`;
    await page.getByLabel(`Rename ${name}`).fill(renamed);
    await page.getByLabel(`Rename ${name}`).press("Enter");
    await expect(page.getByRole("row", { name: renamed })).toBeVisible();

    page.once("dialog", (d) => void d.accept());
    await page.getByRole("row", { name: renamed }).getByRole("button", { name: "Delete" }).click();
    await expect(page.getByRole("row", { name: renamed })).toHaveCount(0);
  });

  test("refuses duplicate organization names", async ({ page }) => {
    await page.getByLabel("New organization").fill("proceedit");
    await page.getByRole("button", { name: "Add", exact: true }).click();
    await expect(page.getByText(/already exists/i)).toBeVisible();
  });

  test("delete is disabled while members belong", async ({ page }) => {
    const row = page.getByRole("row", { name: /Proceedit/ });
    await expect(row).toBeVisible();
    await expect(row.getByRole("button", { name: "Delete" })).toBeDisabled();
  });

  test("add-person form picks an existing organization", async ({ page }) => {
    await page.goto("/people/new");
    await page.getByRole("tab", { name: "Full profile" }).click();
    await page.getByLabel("Name", { exact: true }).fill(`E2E OrgPick ${Date.now()}`);
    await page.getByLabel("Email", { exact: true }).fill(`orgpick-${Date.now()}@example.com`);
    await page.locator("#f-gender").click();
    await page.getByRole("option", { name: "Male", exact: true }).click();
    await page.getByLabel("Date of birth").fill("1992-02-02");
    await page.locator("#f-country").click();
    await page.getByRole("option").first().click();
    const chip = page.getByRole("button", { name: "Proceedit", exact: true });
    await expect(chip).toBeVisible();
    await chip.click();
    await expect(chip).toHaveAttribute("aria-pressed", "true");
    await page.getByRole("button", { name: "Add person", exact: true }).last().click();
    await expect(page).toHaveURL(/\/people\/[0-9a-f-]{36}/);
    await expect(page.getByRole("link", { name: "Proceedit", exact: true })).toBeVisible();
  });
});
