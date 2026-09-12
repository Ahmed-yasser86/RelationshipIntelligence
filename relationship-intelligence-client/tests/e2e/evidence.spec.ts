import { expect, test } from "@playwright/test";
import { API_URL, loginViaApi } from "./helpers";

test.describe("evidence honesty", () => {
  test("contact with no history is unobserved, unscored, and outside the queue", async ({
    page,
    request,
  }) => {
    await loginViaApi(page);
    const token = await page.evaluate(() => localStorage.getItem("ri.token"));
    const headers = { Authorization: `Bearer ${token}` };
    const name = `E2E Ghost ${Date.now()}`;
    const created = await request.post(`${API_URL}/api/Contacts/PostQuickAddContact`, {
      headers,
      data: { Name: name, email: `${name.replace(/\W/g, "")}@example.com` },
    });
    expect(created.ok()).toBeTruthy();
    const personId = ((await created.json()) as { personId: string }).personId;

    try {
      await page.goto(`/people/${personId}`);
      await expect(page.getByText("Not yet observed.")).toBeVisible();
      await expect(page.getByText("Relationship state.")).toHaveCount(0);

      await page.getByRole("link", { name: "Attention", exact: true }).click();
      await expect(page.getByRole("heading", { name: "Needs attention" })).toBeVisible();
      await expect(page.getByText(name)).toHaveCount(0);
    } finally {
      await request.post(`${API_URL}/api/Contacts/DeletePersoneObject?id=${personId}`, {
        headers,
      });
    }
  });
});
