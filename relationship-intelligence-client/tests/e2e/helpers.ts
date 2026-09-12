import { Page, expect } from "@playwright/test";

export const API_URL =
  process.env.E2E_API_URL ?? "http://127.0.0.1:5156";
export const TEST_EMAIL =
  process.env.E2E_EMAIL ?? "testuser@contactsmanager.dev";
export const TEST_PASSWORD = process.env.E2E_PASSWORD ?? "Test123!";

export async function loginViaApi(page: Page): Promise<void> {
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
    },
    [body.token as string, (body.personeEmail as string) || TEST_EMAIL],
  );
  await page.reload();
}

export async function expectSignedIn(page: Page): Promise<void> {
  await expect(page.getByRole("heading", { name: "Overview" })).toBeVisible();
}
