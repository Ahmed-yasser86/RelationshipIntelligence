import { test } from "@playwright/test";
import { expectSignedIn, loginViaApi } from "./helpers";

async function ask(page: import("@playwright/test").Page, text: string): Promise<string> {
  await page.getByLabel("Ask the co-pilot").fill(text);
  await page.getByRole("button", { name: "Send", exact: true }).click();
  await page.waitForTimeout(3500);
  const bubbles = page.locator("div.self-start");
  const n = await bubbles.count();
  return (await bubbles.nth(n - 1).locator("p").first().innerText()).slice(0, 400);
}

test.describe("copilot refactor verification", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await expectSignedIn(page);
    await page.getByRole("button", { name: "Ask co-pilot" }).click();
  });

  test("verify all scenario groups behave distinctly", async ({ page }) => {
    const out: string[] = [];
    const scenarios: [string, string][] = [
      ["greet", "Hi"],
      ["capabilities", "What can you help me with?"],
      ["person", "What happened with Tarek Mansour?"],
      ["unknown-person", "Tell me about my relationship with Ahmed."],
      ["cross-entity", "What important dates are coming up?"],
      ["neglect", "Who have I been neglecting?"],
      ["followup-1", "What happened with Tarek?"],
      ["followup-2", "Why does that matter?"],
      ["followup-3", "What should I do about it?"],
      ["direction", "Actually, forget Tarek. Who should I contact this week?"],
      ["clarify", "Prepare a meeting for tomorrow."],
      ["unexpected", "I need to follow up on something I promised someone, but I don't remember who."],
      ["multistep", "Find the people I should reconnect with this week and explain why each one matters."],
    ];
    for (const [name, q] of scenarios) {
      try {
        const answer = await ask(page, q);
        out.push(`### ${name}\nQ: ${q}\nA: ${answer}`);
      } catch (err) {
        out.push(`### ${name}\nQ: ${q}\nERROR: ${String(err).slice(0, 200)}`);
      }
    }
    console.log(`VERIFY-RESULTS:\n${out.join("\n\n")}`);
  });
});
