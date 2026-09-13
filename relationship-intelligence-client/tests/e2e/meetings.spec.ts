import { expect, test } from "@playwright/test";
import { expectSignedIn, loginViaApi } from "./helpers";

test.describe("meetings pipeline", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await expectSignedIn(page);
    await page.getByRole("link", { name: "Meetings", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Meetings" })).toBeVisible();
  });

  test("prep carries participants into logging with the same identity", async ({ page }) => {
    const title = `E2E Prep ${Date.now()}`;
    await page.getByRole("button", { name: "Plan a meeting" }).click();
    await page.getByLabel("Meeting title").fill(title);
    await page.getByLabel("Participants (comma-separated names)").fill("Salma El-Sayed");
    await page.getByRole("button", { name: "Create preparation" }).click();
    await page.getByRole("link", { name: title }).click();
    await expect(page.getByText("Preparation", { exact: true }).first()).toBeVisible();
    await expect(page.getByText("Salma El-Sayed")).toBeVisible();
    const url = page.url();

    await page.getByRole("button", { name: "Start logging this meeting" }).click();
    await expect(page.getByText("Draft", { exact: true }).first()).toBeVisible();
    expect(page.url()).toBe(url);
    await expect(page.getByText("Salma El-Sayed")).toBeVisible();
  });

  test("draft to confirmed evidence with mapping, findings, and memory", async ({ page }) => {
    const title = `E2E Meeting ${Date.now()}`;
    await page.getByRole("button", { name: "Log a meeting" }).click();
    await page.getByLabel("Meeting title").fill(title);
    await page.getByRole("button", { name: "Create draft" }).click();
    await page.getByRole("link", { name: title }).click();

    await page.getByRole("button", { name: "Add transcript / notes" }).click();
    await page.getByLabel("Notes").fill(
      "Salma El-Sayed joined the call.\nShe will send the crit notes by Friday.\nShould we move the launch?",
    );
    await page.getByRole("button", { name: "Save source" }).click();
    await page.getByRole("button", { name: "Process with co-pilot" }).click();
    await expect(page.getByText("Processed", { exact: true }).first()).toBeVisible();
    await expect(page.getByText("Send the crit notes by Friday", { exact: false }).first()).toBeVisible();

    const personRow = page.getByRole("listitem").filter({ hasText: "Salma El-Sayed" }).first();
    await expect(personRow.getByText("Suggested")).toBeVisible();
    await personRow.getByLabel("Map to person").fill("");
    await personRow.getByLabel("Map to person").fill("Salma");
    await page.getByRole("button", { name: "Salma El-Sayed", exact: true }).click();
    await page.getByRole("button", { name: "Save mappings" }).click();
    await expect(personRow.getByText("Confirmed")).toBeVisible();
    const logBox = personRow.getByLabel("Log for this relationship");
    await logBox.click();
    await expect(logBox).toBeChecked();

    const commitment = page.getByRole("listitem").filter({ hasText: "crit notes by Friday" }).first();
    await commitment.getByLabel("Map to person").fill("Salma");
    await page.getByRole("button", { name: "Salma El-Sayed", exact: true }).click();
    await commitment.getByRole("button", { name: "Accept" }).click();
    await expect(commitment.getByText("Accepted")).toBeVisible();

    await page.getByRole("button", { name: "Confirm & log" }).click();
    await expect(page.getByText("Confirmed", { exact: true }).first()).toBeVisible();

    await page.getByRole("link", { name: "Salma El-Sayed", exact: true }).first().click();
    await expect(page.getByRole("heading", { level: 1 })).toContainText("Salma");
    const historySection = page.locator("section", { has: page.getByRole("heading", { name: "History", exact: true }) });
    await expect(historySection.getByText(title).first()).toBeVisible();
    await expect(page.getByText("Send the crit notes by Friday", { exact: false }).first()).toBeVisible();
  });
});
