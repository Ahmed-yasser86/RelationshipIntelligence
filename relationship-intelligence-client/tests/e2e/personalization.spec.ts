import { expect, test } from "@playwright/test";
import { API_URL, expectSignedIn, loginViaApi } from "./helpers";

// The personalization experience end to end — profile teaches voice,
// edits preserve history, organizations and network stay consistent.
test.describe("personalization", () => {
  test.beforeEach(async ({ page }) => {
    await loginViaApi(page);
    await expectSignedIn(page);
  });

  test("taught voice changes drafts and edits preserve history", async ({ page, request }) => {
    const stamp = Date.now();
    const token = () => page.evaluate(() => localStorage.getItem("ri.token"));
    const auth = async () => ({ Authorization: `Bearer ${await token()}` });

    // 1. Create a person with a communication profile (quick add + style).
    await page.getByRole("link", { name: "People", exact: true }).click();
    await page.getByRole("link", { name: "Add person" }).click();
    await page.getByLabel("Name").first().fill(`E2E Voice ${stamp}`);
    await page.getByLabel("Email").fill(`voice-${stamp}@example.com`);
    await expect(page.getByLabel("How you talk to them (optional)")).toBeVisible();
    await page.getByRole("button", { name: "Add person" }).click();
    await expect(page).toHaveURL(/\/people\/[0-9a-f-]{36}/);
    const personId = page.url().split("/").pop()!;

    // 2. Draft before teaching: generic greeting.
    const headers = await auth();
    const batchRes = await request.post(`${API_URL}/api/Outreach/PostBatchFromPersons`, {
      headers,
      data: { PersonIds: [personId], Channel: "Email", Intent: "Reconnect" },
    });
    expect(batchRes.ok()).toBeTruthy();
    const batch = await batchRes.json();
    const draftsRes = await request.post(`${API_URL}/api/Outreach/PostBatchDrafts?id=${batch.outreachBatchId}`, { headers });
    expect(draftsRes.ok()).toBeTruthy();
    const drafts = await draftsRes.json();
    const before = drafts.drafts[0].body as string;
    expect(before.startsWith("Hi ")).toBeTruthy();

    // 3. Teach a casual voice, regenerate: greeting must change.
    const styleRes = await request.post(`${API_URL}/api/Contacts/PostMemoryEntry`, {
      headers,
      data: { PersonId: personId, Kind: 10, Title: "casual, direct", Detail: null },
    });
    expect(styleRes.ok()).toBeTruthy();
    const draftId = drafts.drafts[0].communicationDraftId as string;
    const regenRes = await request.post(
      `${API_URL}/api/Outreach/PostDraftRegenerate?id=${draftId}`, { headers, data: {} });
    expect(regenRes.ok()).toBeTruthy();
    const regen = await regenRes.json();
    expect((regen.body as string).startsWith("Hey ")).toBeTruthy();
    expect(regen.body).not.toBe(before);

    // 4. Edit naturally: original AI draft preserved in history.
    const editRes = await request.put(`${API_URL}/api/Outreach/PutDraft?id=${draftId}`, {
      headers,
      data: { Status: 1, Body: "Quick ping — still good for Thursday?" },
    });
    expect(editRes.ok()).toBeTruthy();
    const edited = await editRes.json();
    expect(edited.originalBody).toBe(regen.body);
    expect(edited.body).toBe("Quick ping — still good for Thursday?");

    // 5. A style suggestion was proposed for approval (never silent).
    const memRes = await request.get(
      `${API_URL}/api/Contacts/GetRelationshipMemory?personId=${personId}`, { headers });
    const entries = (await memRes.json()) as { kind: number; provenance: number; title: string }[];
    expect(entries.some((e) => e.kind === 8 && e.provenance === 1 && e.title.startsWith("Style:"))).toBeTruthy();

    // Cleanup so probes never pollute counts (delete is POST on this API).
    await request.delete(`${API_URL}/api/Outreach/DeleteBatch?id=${batch.outreachBatchId}`, { headers });
    const delPerson = await request.post(`${API_URL}/api/Contacts/DeletePersoneObject?id=${personId}`, { headers, data: {} });
    expect(delPerson.ok()).toBeTruthy();
  });

  test("organization opens its people with relationship state", async ({ page, request }) => {
    const stamp = Date.now();
    const org = `E2E Org ${stamp}`;
    const token = await page.evaluate(() => localStorage.getItem("ri.token"));
    const headers = { Authorization: `Bearer ${token}` };

    const personRes = await request.post(`${API_URL}/api/Contacts/PostQuickAddContact`, {
      headers,
      data: { Name: `E2E Member ${stamp}`, email: `member-${stamp}@example.com`, Organizations: [org] },
    });
    expect(personRes.ok()).toBeTruthy();
    const person = await personRes.json();

    await page.goto("/organizations");
    await page.locator("tr", { hasText: org }).getByRole("link").first().click();
    await expect(page).toHaveURL(new RegExp(`/people\\?org=${encodeURIComponent(org)}`));
    await expect(page.getByText(`E2E Member ${stamp}`).first()).toBeVisible();

    const delMember = await request.post(`${API_URL}/api/Contacts/DeletePersoneObject?id=${person.personId}`, { headers, data: {} });
    expect(delMember.ok()).toBeTruthy();
    const orgs = (await (await request.get(`${API_URL}/api/Contacts/GetOrganizations`, { headers })).json()) as {
      circleId: string; name: string;
    }[];
    const created = orgs.find((o) => o.name === org);
    if (created) await request.delete(`${API_URL}/api/Contacts/DeleteOrganization?id=${created.circleId}`, { headers });
  });

  test("network explains its semantics and navigates to people", async ({ page }) => {
    await page.goto("/network");
    await expect(page.getByText("shared-context connections").first()).toBeVisible();
    await expect(page.getByRole("button", { name: "List" })).toBeVisible();
    await page.getByRole("button", { name: "List" }).click();
    const firstPerson = page.locator("ul > li a").first();
    if (await firstPerson.count()) {
      await firstPerson.click();
      await expect(page).toHaveURL(/\/people\/[0-9a-f-]{36}/);
    }
  });
});
