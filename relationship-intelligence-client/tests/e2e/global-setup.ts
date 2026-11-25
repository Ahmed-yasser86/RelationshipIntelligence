import { request } from "@playwright/test";
import { API_URL, TEST_EMAIL, TEST_PASSWORD } from "./helpers";

async function warmup() {
  const ctx = await request.newContext();
  const login = await ctx.post(`${API_URL}/api/Account/PostLogin`, {
    data: { Email: TEST_EMAIL, Password: TEST_PASSWORD },
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
