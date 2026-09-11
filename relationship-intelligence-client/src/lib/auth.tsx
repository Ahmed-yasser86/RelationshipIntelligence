import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from "react";
import type { ReactNode } from "react";
import { api, getToken, setToken } from "./api";
import type { AuthResponse } from "./types";

export interface SessionUser {
  name: string;
  email: string;
}

interface AuthContextValue {
  user: SessionUser | null;
  ready: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (input: {
    PersonName: string;
    Email: string;
    Phone: string;
    Password: string;
    ConfirmPassword: string;
  }) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

function decodeName(token: string): string {
  try {
    const payload = JSON.parse(atob(token.split(".")[1]));
    return (
      payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"] ??
      payload.name ??
      ""
    );
  } catch {
    return "";
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<SessionUser | null>(null);
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const token = getToken();
    if (token) {
      const email = localStorage.getItem("ri.email") ?? "";
      setUser({ name: decodeName(token) || email, email });
    }
    setReady(true);
    const onUnauthorized = () => setUser(null);
    window.addEventListener("ri:unauthorized", onUnauthorized);
    return () => window.removeEventListener("ri:unauthorized", onUnauthorized);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const res = await api.post<AuthResponse>("/api/Account/PostLogin", {
      Email: email,
      Password: password,
    });
    setToken(res.token);
    localStorage.setItem("ri.email", res.personeEmail || email);
    setUser({ name: res.personeName || email, email: res.personeEmail || email });
  }, []);

  const register = useCallback(
    async (input: {
      PersonName: string;
      Email: string;
      Phone: string;
      Password: string;
      ConfirmPassword: string;
    }) => {
      const res = await api.post<AuthResponse>("/api/Account/PostRegister", {
        ...input,
        UserType: 0,
      });
      setToken(res.token);
      localStorage.setItem("ri.email", res.personeEmail || input.Email);
      setUser({
        name: res.personeName || input.Email,
        email: res.personeEmail || input.Email,
      });
    },
    [],
  );

  const logout = useCallback(() => {
    const token = getToken();
    if (token) {
      api.post("/api/Account/PostLogout").catch(() => undefined);
    }
    setToken(null);
    localStorage.removeItem("ri.email");
    setUser(null);
  }, []);

  const value = useMemo(
    () => ({ user, ready, login, register, logout }),
    [user, ready, login, register, logout],
  );
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error("useAuth must be used within AuthProvider");
  return ctx;
}
