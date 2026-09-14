import { expect, test } from "@playwright/test";
import { API_URL, expectSignedIn, loginViaApi } from "./helpers";

test.describe("co-pilot", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await expectSignedIn(page);
  });

  test("provider settings save without ever revealing the key", async ({ page, request }) => {
    // Isolated user: saving a fake key here must never clobber shared
    // credentials used by every other test.
    const stamp = Date.now();
    const email = `e2e-settings-${stamp}@example.com`;
    const reg = await request.post(`${API_URL}/api/Account/PostRegister`, {
      data: {
        PersonName: "E2E Settings",
        Email: email,
        Phone: "01000000000",
        Password: "Test123!",
        ConfirmPassword: "Test123!",
        UserType: 0,
      },
    });
    expect(reg.ok()).toBeTruthy();
    const login = await request.post(`${API_URL}/api/Account/PostLogin`, {
      data: { Email: email, Password: "Test123!" },
    });
    const body = await login.json();
    await page.goto("/");
    await page.evaluate(
      ([token, mail]) => {
        localStorage.setItem("ri.token", token);
        localStorage.setItem("ri.email", mail);
        localStorage.setItem("ri.onboarded", "1");
      },
      [body.token as string, email],
    );
    await page.reload();
    await expect(page.getByRole("heading", { name: "Today", exact: true })).toBeVisible();
    await page.getByRole("button", { name: "Ask co-pilot" }).click();
    await page.getByRole("button", { name: "Setup" }).click();
    await page.getByLabel("Model").fill("gemini-3.1-flash-lite");
    await page.getByLabel(/API key/).fill("test-key-not-real");
    await page.getByRole("button", { name: "Save provider" }).click();
    await expect(page.getByText("(key saved)")).toBeVisible();
    await expect(page.getByLabel(/API key/)).toHaveValue("");
  });

  test("ask returns a grounded synthesis, not raw records", async ({ page }) => {
    await page.getByRole("button", { name: "Ask co-pilot" }).click();
    await page.getByLabel("Ask the co-pilot").fill("Who is losing touch right now?");
    await page.getByRole("button", { name: "Send", exact: true }).click();
    // Behavior contract across modes: real relationship signals, evidence
    // on demand, and a path into the queue — never raw internal records.
    await expect(page.getByText(/urgency|drifting|critical|at risk|silence/i).first()).toBeVisible({ timeout: 60_000 });
    await expect(page.getByText(/Why\? Show evidence/i).first()).toBeVisible();
    await expect(page.getByRole("button", { name: "Open attention queue" })).toBeVisible();
  });

  test("follow-ups keep context without repeating names", async ({ page }) => {
    await page.getByRole("link", { name: "People", exact: true }).click();
    await page.getByLabel("Search").fill("Salma");
    await page.getByRole("button", { name: "Apply" }).click();
    await page.locator("ul > li", { hasText: "Salma" }).locator("a").first().click();
    await page.locator("main").getByRole("button", { name: "Ask co-pilot" }).click();
    await page.getByLabel("Ask the co-pilot").fill("What happened here?");
    await page.getByRole("button", { name: "Send", exact: true }).click();
    await expect(page.getByRole("link", { name: "Salma El-Sayed" })).toBeVisible();
    await page.getByLabel("Ask the co-pilot").fill("Why does that matter?");
    await page.getByRole("button", { name: "Send", exact: true }).click();
    await expect(page.getByText(/Salma El-Sayed/i).first()).toBeVisible();
    await expect(page.getByText(/urgency|rhythm|silence/i).first()).toBeVisible();
  });

  test("unknown people get honesty, not invention", async ({ page }) => {
    await page.getByRole("button", { name: "Ask co-pilot" }).click();
    await page.getByLabel("Ask the co-pilot").fill("Tell me about my relationship with Ahmed.");
    await page.getByRole("button", { name: "Send", exact: true }).click();
    await expect(page.getByText(/don't have a contact matching/i).first()).toBeVisible();
  });

  test("meeting prep asks who instead of inventing", async ({ page }) => {
    await page.getByRole("button", { name: "Ask co-pilot" }).click();
    await page.getByLabel("Ask the co-pilot").fill("Prepare a meeting for tomorrow.");
    await page.getByRole("button", { name: "Send", exact: true }).click();
    await expect(page.getByText(/Who are you meeting/i).first()).toBeVisible();
  });

  test("outreach runs as a working session with approval", async ({ page }) => {
    await page.getByRole("button", { name: "Ask co-pilot" }).click();
    await page.getByLabel("Ask the co-pilot").fill("Find everyone I should reconnect with this week.");
    await page.getByRole("button", { name: "Send", exact: true }).click();
    await expect(page.getByText(/worth considering/i).first()).toBeVisible();
    await expect(page.getByText(/Working on: outreach/i)).toBeVisible();
    await page.getByLabel("Ask the co-pilot").fill("Use LinkedIn for the rest.");
    await page.getByRole("button", { name: "Send", exact: true }).click();
    await expect(page.getByText(/Channel set to LinkedIn/i).first()).toBeVisible();
    await page.getByLabel("Ask the co-pilot").fill("Prepare them.");
    await page.getByRole("button", { name: "Send", exact: true }).click();
    await expect(page.getByText(/personalized draft/i).first()).toBeVisible();
    await page.getByLabel("Ask the co-pilot").fill("Approve.");
    await page.getByRole("button", { name: "Send", exact: true }).click();
    await expect(page.getByText(/Ready to approve/i).first()).toBeVisible();
    await page.getByLabel("Ask the co-pilot").fill("Approve now.");
    await page.getByRole("button", { name: "Send", exact: true }).click();
    await expect(page.getByText(/Approved \d+ draft/i).first()).toBeVisible();
  });

  test("person-scoped ask cites the person", async ({ page }) => {
    await page.getByRole("link", { name: "People", exact: true }).click();
    await page.getByLabel("Search").fill("Salma");
    await page.getByRole("button", { name: "Apply" }).click();
    await page.locator("ul > li", { hasText: "Salma" }).locator("a").first().click();
    await page.locator("main").getByRole("button", { name: "Ask co-pilot" }).click();
    await page.getByLabel("Ask the co-pilot").fill("What happened here?");
    await page.getByRole("button", { name: "Send", exact: true }).click();
    await expect(page.getByRole("link", { name: "Salma El-Sayed" })).toBeVisible();
  });

  test("meeting follow-up questions cite recorded commitments", async ({ page }) => {
    await page.getByRole("link", { name: "People", exact: true }).click();
    await page.getByLabel("Search").fill("Salma");
    await page.getByRole("button", { name: "Apply" }).click();
    await page.locator("ul > li", { hasText: "Salma" }).locator("a").first().click();
    await page.locator("main").getByRole("button", { name: "Ask co-pilot" }).click();
    await page.getByLabel("Ask the co-pilot").fill("What did I promise Salma?");
    await page.getByRole("button", { name: "Send", exact: true }).click();
    await expect(page.getByText(/crit notes/i).first()).toBeVisible();
  });

  test("briefing block renders on the overview", async ({ page }) => {
    await expect(page.getByRole("heading", { name: "Today's briefing" })).toBeVisible();
  });

  test("natural-language outreach resolves to signal filters, never people", async ({ page, request }) => {
    const token = await page.evaluate(() => localStorage.getItem("ri.token"));
    const utterances = [
      "Prepare messages for everyone I should reconnect with this week.",
      "Who should I follow up with after last week's meetings?",
      "Prepare short LinkedIn messages for the people I've neglected recently.",
      "I want to reconnect with everyone in my attention queue. Use email.",
    ];
    for (const text of utterances) {
      const res = await request.post(`${API_URL}/api/Copilot/PostParseOutreachIntent`, {
        headers: { Authorization: `Bearer ${token}` },
        data: { Text: text },
      });
      expect(res.ok()).toBeTruthy();
      const body = await res.json();
      expect(body.signalFilters.length).toBeGreaterThan(0);
      expect(JSON.stringify(body).toLowerCase()).not.toContain("personid");
    }
    const linked = await request.post(`${API_URL}/api/Copilot/PostParseOutreachIntent`, {
      headers: { Authorization: `Bearer ${token}` },
      data: { Text: "Prepare short LinkedIn messages for the people I've neglected recently." },
    });
    expect((await linked.json()).channel).toBe("LinkedIn");
  });
});
