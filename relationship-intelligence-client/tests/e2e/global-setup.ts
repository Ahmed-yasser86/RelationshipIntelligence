import { request } from "@playwright/test";

const API_URL = process.env.E2E_API_URL ?? "http://127.0.0.1:5156";
const EMAIL = process.env.E2E_EMAIL ?? "testuser@contactsmanager.dev";
const PASSWORD = process.env.E2E_PASSWORD ?? "Test123!";

async function warmup() {
  const ctx = await request.newContext();
  const login = await ctx.post(`${API_URL}/api/Account/PostLogin`, {
    data: { Email: EMAIL, Password: PASSWORD },
  });
  if (!login.ok()) {
    throw new Error(`Warmup login failed: ${login.status()}`);
  }
  const body = await login.json();
  const headers = { Authorization: `Bearer ${body.token as string}` };
  for (const path of [
    "/api/Contacts/GetRelationshipQueue?top=7",
    "/api/Network/GetNetworkGraph",
    "/api/Digest/GetWeeklyDigest",
  ]) {
    const res = await ctx.get(`${API_URL}${path}`, { headers });
    if (!res.ok()) {
      throw new Error(`Warmup ${path} failed: ${res.status()}`);
    }
    await res.body();
  }
  await ctx.dispose();
}

export default warmup;
