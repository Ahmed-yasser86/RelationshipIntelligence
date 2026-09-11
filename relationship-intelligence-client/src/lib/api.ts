const BASE_URL =
  import.meta.env.VITE_API_URL ?? "http://localhost:5156";

const TOKEN_KEY = "ri.token";

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string | null) {
  if (token) localStorage.setItem(TOKEN_KEY, token);
  else localStorage.removeItem(TOKEN_KEY);
}

function readableBody(text: string): string {
  const trimmed = text.trim();
  if (trimmed.startsWith("{")) {
    try {
      const parsed = JSON.parse(trimmed) as {
        message?: unknown;
        title?: unknown;
        errors?: unknown;
      };
      if (typeof parsed.message === "string" && parsed.message !== "") {
        return parsed.message;
      }
      if (typeof parsed.title === "string" && parsed.title !== "") {
        const details =
          parsed.errors != null ? ` ${JSON.stringify(parsed.errors)}` : "";
        return `${parsed.title}.${details}`;
      }
    } catch {
      // fall through to raw text
    }
  }
  if (trimmed.startsWith('"') && trimmed.endsWith('"')) {
    try {
      const parsed: unknown = JSON.parse(trimmed);
      if (typeof parsed === "string") return parsed;
    } catch {
      // fall through to raw text
    }
  }
  return text;
}

export class ApiError extends Error {
  status: number;
  body: string;
  constructor(status: number, body: string) {
    super(body || `Request failed (${status})`);
    this.status = status;
    this.body = body;
  }
}

async function request<T>(
  path: string,
  init: RequestInit = {},
): Promise<T> {
  const headers: Record<string, string> = {
    ...(init.headers as Record<string, string> | undefined),
  };
  const token = getToken();
  if (token) headers["Authorization"] = `Bearer ${token}`;
  if (init.body !== undefined && !(init.body instanceof FormData)) {
    headers["Content-Type"] = "application/json";
  }

  const res = await fetch(`${BASE_URL}${path}`, { ...init, headers });
  const text = await res.text();
  if (res.status === 401) {
    setToken(null);
    window.dispatchEvent(new Event("ri:unauthorized"));
    throw new ApiError(401, text !== "" ? readableBody(text) : "Session expired. Please sign in again.");
  }
  if (res.status === 204) return undefined as T;
  if (!res.ok) throw new ApiError(res.status, text !== "" ? readableBody(text) : res.statusText);
  if (!text) return undefined as T;
  const contentType = res.headers.get("content-type") ?? "";
  if (contentType.includes("json")) return JSON.parse(text) as T;
  return text as unknown as T;
}

export const api = {
  get: <T>(path: string) => request<T>(path, { method: "GET" }),
  post: <T>(path: string, body?: unknown) =>
    request<T>(path, {
      method: "POST",
      body: body === undefined ? undefined : JSON.stringify(body),
    }),
  put: <T>(path: string, body?: unknown) =>
    request<T>(path, {
      method: "PUT",
      body: body === undefined ? undefined : JSON.stringify(body),
    }),
};

export { BASE_URL };
