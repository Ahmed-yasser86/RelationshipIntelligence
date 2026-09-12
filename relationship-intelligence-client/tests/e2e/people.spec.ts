import { expect, test } from "@playwright/test";
import { expectSignedIn, loginViaApi } from "./helpers";

test.describe("people", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await page.getByRole("link", { name: "People", exact: true }).click();
    await expect(page.getByRole("heading", { name: "People" })).toBeVisible();
  });

  test("lists contacts with pagination", async ({ page }) => {
    await expect(page.locator("ul > li").first()).toBeVisible();
    await expect(page.getByText(/Page 1 of/)).toBeVisible();
    const firstName = await page.locator("ul > li").first().innerText();
    await page.getByRole("button", { name: "Next" }).click();
    await expect(page.getByText(/Page 2 of/)).toBeVisible();
    const secondPageFirst = await page.locator("ul > li").first().innerText();
    expect(secondPageFirst).not.toBe(firstName);
    await page.getByRole("button", { name: "Previous" }).click();
    await expect(page.getByText(/Page 1 of/)).toBeVisible();
  });

  test("searches by name", async ({ page }) => {
    await page.getByLabel("Search").fill("Salma");
    await page.getByRole("button", { name: "Apply" }).click();
    await expect(page.locator("ul > li").first()).toContainText("Salma");
  });

  test("shows empty state for impossible search", async ({ page }) => {
    await page.getByLabel("Search").fill("zzz-no-such-person-zzz");
    await page.getByRole("button", { name: "Apply" }).click();
    await expect(page.getByText("No people found")).toBeVisible();
  });

  test("composite filter by organization", async ({ page }) => {
    await page.getByRole("button", { name: "Filters" }).click();
    await page.getByLabel("Organization").fill("Proceedit");
    await page.getByRole("button", { name: "Filter", exact: true }).click();
    await expect(page.locator("ul > li").first()).toBeVisible();
  });

  test("composite filter by status tag dropdown", async ({ page }) => {
    await page.getByRole("button", { name: "Filters" }).click();
    await page.getByLabel("Status tag").click();
    await page.getByRole("option", { name: "High Priority" }).click();
    await page.getByRole("button", { name: "Filter", exact: true }).click();
    await expect(page.locator("ul > li").first()).toBeVisible();
  });

  test("sorts by latest interaction", async ({ page }) => {
    await page.getByLabel("Sort").click();
    await page.getByRole("option", { name: "Interactions" }).click();
    await page.getByRole("button", { name: "Apply" }).click();
    await expect(page.locator("ul > li").first()).toBeVisible();
  });

  test("opens a person detail from the list", async ({ page }) => {
    await page.locator("ul > li a").first().click();
    await expect(page).toHaveURL(/\/people\/.+/);
  });

  test("unknown person id shows not found", async ({ page }) => {
    await page.goto("/people/00000000-0000-0000-0000-000000000000");
    await expect(page.getByText("Something went wrong")).toBeVisible();
  });
});
