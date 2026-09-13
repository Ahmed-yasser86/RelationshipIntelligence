import { expect, test } from "@playwright/test";
import { API_URL, expectSignedIn, loginViaApi } from "./helpers";

test.describe("network", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await page.getByRole("link", { name: "Network", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Network" })).toBeVisible();
  });

  test("renders shared-organization edges and selectable nodes", async ({
    page,
    request,
  }) => {
    const org = `E2E Graph Org ${Date.now()}`;
    const token = await page.evaluate(() => localStorage.getItem("ri.token"));
    const headers = { Authorization: `Bearer ${token}` };
    const created: string[] = [];
    for (const name of [`E2E Graph A ${Date.now()}`, `E2E Graph B ${Date.now()}`]) {
      const res = await request.post(`${API_URL}/api/Contacts/PostQuickAddContact`, {
        headers,
        data: { Name: name, email: `${name.replace(/\W/g, "")}@example.com`, Organizations: [org] },
      });
      expect(res.ok()).toBeTruthy();
      created.push(((await res.json()) as { personId: string }).personId);
    }

    try {
      await page.reload();
      const svg = page.locator('svg[aria-label="Contact network graph"]');
      await expect(svg).toBeVisible();
      await page.getByLabel("Find person").fill("E2E Graph A");
      await page.getByRole("button", { name: /E2E Graph A/ }).click();
      const panel = page.locator("aside");
      await expect(panel.getByText(/E2E Graph A/).first()).toBeVisible();
      await expect(panel.getByText(/connections/)).toBeVisible();
    } finally {
      for (const id of created) {
        await request.post(`${API_URL}/api/Contacts/DeletePersoneObject?id=${id}`, {
          headers,
          data: {},
        });
      }
    }
  });

  test("selecting a node does not rearrange the map", async ({ page }) => {
    const svg = page.locator('svg[aria-label="Contact network graph"]');
    await expect(svg).toBeVisible();
    await expect(page.locator("[data-node-id]").first()).toBeVisible();

    const result = await page.evaluate(
      () =>
        new Promise<{ settled: boolean; maxMove: number }>((resolve) => {
          const snap = (): Record<string, [number, number]> => {
            const out: Record<string, [number, number]> = {};
            document.querySelectorAll("[data-node-id]").forEach((el) => {
              const t = el.getAttribute("transform") ?? "";
              const m = /translate\(([-\d.]+),([-\d.]+)\)/.exec(t);
              if (m) out[el.getAttribute("data-node-id")!] = [parseFloat(m[1]), parseFloat(m[2])];
            });
            return out;
          };
          const dist = (a: Record<string, [number, number]>, b: Record<string, [number, number]>): number => {
            let max = 0;
            for (const id of Object.keys(a)) {
              if (!(id in b)) return Number.POSITIVE_INFINITY;
              max = Math.max(max, Math.hypot(a[id][0] - b[id][0], a[id][1] - b[id][1]));
            }
            return max;
          };
          let prev = snap();
          const iv = setInterval(() => {
            const cur = snap();
            if (Object.keys(cur).length === 0) return;
            if (dist(prev, cur) < 1.5) {
              clearInterval(iv);
              const first = document.querySelector("[data-node-id]");
              if (first) {
                first.dispatchEvent(new MouseEvent("click", { bubbles: true, cancelable: true }));
                const before = snap();
                setTimeout(() => resolve({ settled: true, maxMove: dist(before, snap()) }), 900);
              } else {
                resolve({ settled: false, maxMove: Number.POSITIVE_INFINITY });
              }
            } else {
              prev = cur;
            }
          }, 700);
          setTimeout(() => {
            clearInterval(iv);
            resolve({ settled: false, maxMove: Number.POSITIVE_INFINITY });
          }, 30000);
        }),
    );
    expect(result.settled).toBe(true);
    expect(result.maxMove).toBeLessThan(5);
  });
});

test.describe("digest", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await page.getByRole("link", { name: "Digest", exact: true }).click();
    await expect(page.getByRole("heading", { name: "Weekly digest" })).toBeVisible();
  });

  test("shows the weekly selection and preferences", async ({ page }) => {
    await expect(
      page.getByText(/relationships to protect|quiet week/i).first(),
    ).toBeVisible();
    await expect(page.getByLabel("Minimum urgency")).toBeVisible();
    await expect(page.getByLabel("Contacts per digest")).toBeVisible();
  });

  test("reviews the exact email before sending", async ({ page }) => {
    await page.getByRole("button", { name: "Review email" }).click();
    await expect(page.getByText("Email preview")).toBeVisible();
    await expect(page.getByText(/^To$/)).toBeVisible();
    await expect(page.getByText(/^Subject$/)).toBeVisible();
    await page.getByRole("button", { name: "Cancel" }).click();
    await expect(page.getByRole("button", { name: "Review email" })).toBeVisible();
  });

  test("saves preferences", async ({ page }) => {
    await page.getByLabel("Contacts per digest").fill("3");
    await page.getByRole("button", { name: "Save" }).click();
    await expect(page.getByText("Saved.")).toBeVisible();
  });
});
