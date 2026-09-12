import { expect, test } from "@playwright/test";
import { API_URL, TEST_EMAIL, TEST_PASSWORD } from "./helpers";

test.describe("onboarding", () => {
  test.beforeEach(async ({ page }) => {
    const res = await page.request.post(`${API_URL}/api/Account/PostLogin`, {
      data: { Email: TEST_EMAIL, Password: TEST_PASSWORD },
    });
    expect(res.ok()).toBeTruthy();
    const body = await res.json();
    await page.goto("/");
    await page.evaluate(
      ([token, email]) => {
        localStorage.setItem("ri.token", token);
        localStorage.setItem("ri.email", email);
        localStorage.removeItem("ri.onboarded");
      },
      [body.token as string, (body.personeEmail as string) || TEST_EMAIL],
    );
    await page.reload();
    await expect(page.getByText("How this works (1 of 4)")).toBeVisible();
  });

  test("walks through the product story and dismisses", async ({ page }) => {
    await expect(page.getByText("How this works (1 of 4)")).toBeVisible();
    await expect(page.getByText(/dated contact events/)).toBeVisible();
    await page.getByRole("button", { name: "Next" }).click();
    await expect(page.getByText("How this works (2 of 4)")).toBeVisible();
    await page.getByRole("button", { name: "Next" }).click();
    await page.getByRole("button", { name: "Next" }).click();
    await expect(page.getByText("How this works (4 of 4)")).toBeVisible();
    await expect(
      page.getByRole("button", { name: "Load demo workspace" }),
    ).toBeVisible();
    await page.getByRole("button", { name: "Start exploring" }).click();
    await expect(page.getByText("How this works")).toHaveCount(0);
  });
});
