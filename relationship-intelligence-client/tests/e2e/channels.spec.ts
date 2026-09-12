import { expect, test } from "@playwright/test";
import { expectSignedIn, loginViaApi } from "./helpers";

test.describe("channels with handles", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await expectSignedIn(page);
  });

  test("detail shows channel handles and edit updates them", async ({ page }) => {
    await page.getByRole("link", { name: "People", exact: true }).click();
    await page.getByLabel("Search").fill("Salma");
    await page.getByRole("button", { name: "Apply" }).click();
    await page.locator("ul > li", { hasText: "Salma" }).locator("a").first().click();
    await expect(page.getByRole("heading", { level: 1 })).toContainText("Salma");
    await expect(page.getByRole("heading", { name: "Channels", exact: true })).toBeVisible();
    await expect(page.getByText("+20 101 234 5678")).toBeVisible();

    await page.locator("main").getByRole("link", { name: "Edit", exact: true }).click();
    await page.getByLabel("Channel 1 number or handle").fill("+20 101 234 9999");
    await page.getByRole("button", { name: "Save changes" }).click();
    await expect(page.getByText("+20 101 234 9999")).toBeVisible();

    await page.locator("main").getByRole("link", { name: "Edit", exact: true }).click();
    await page.getByLabel("Channel 1 number or handle").fill("+20 101 234 5678");
    await page.getByRole("button", { name: "Save changes" }).click();
    await expect(page.getByText("+20 101 234 5678")).toBeVisible();
  });

  test("full add form saves a channel with handle", async ({ page }) => {
    await page.goto("/people/new");
    await page.getByRole("tab", { name: "Full profile" }).click();
    await page.getByLabel("Name", { exact: true }).fill(`E2E Chan ${Date.now()}`);
    await page.getByLabel("Email", { exact: true }).fill(`chan-${Date.now()}@example.com`);
    await page.locator("#f-gender").click();
    await page.getByRole("option", { name: "Male", exact: true }).click();
    await page.getByLabel("Date of birth").fill("1993-03-03");
    await page.locator("#f-country").click();
    await page.getByRole("option").first().click();
    await page.getByRole("button", { name: "Add channel" }).click();
    await page.getByLabel("Channel 1 name").fill("Telegram");
    await page.getByLabel("Channel 1 number or handle").fill("@e2ehandle");
    await page.getByRole("button", { name: "Add person", exact: true }).last().click();
    await expect(page).toHaveURL(/\/people\/[0-9a-f-]{36}/);
    await expect(page.getByText("@e2ehandle")).toBeVisible();
  });
});
