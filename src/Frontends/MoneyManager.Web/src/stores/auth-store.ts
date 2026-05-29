import { create } from "zustand";
import { useMoneyPrivacyStore } from "@/stores/money-privacy-store";

interface DecodedUser {
  id: string;
  name: string;
  email: string;
}

interface AuthState {
  user: DecodedUser | null;
  token: string | null;
  isAuthenticated: boolean;
  isHydrated: boolean;
  login: (user: DecodedUser, token?: string) => void;
  logout: () => void;
  hydrate: () => Promise<void>;
}

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "";

const SESSION_TOKEN_KEY = "mm_session_token";

function readSessionToken(): string | null {
  if (typeof window === "undefined") return null;
  try {
    return sessionStorage.getItem(SESSION_TOKEN_KEY);
  } catch {
    return null;
  }
}

function writeSessionToken(token: string): void {
  if (typeof window === "undefined") return;
  try {
    sessionStorage.setItem(SESSION_TOKEN_KEY, token);
  } catch {
    // sessionStorage indisponível (ex: modo privado em alguns browsers)
  }
}

function clearSessionToken(): void {
  if (typeof window === "undefined") return;
  try {
    sessionStorage.removeItem(SESSION_TOKEN_KEY);
  } catch {
    // ignora falha silenciosa
  }
}

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  token: null,
  isAuthenticated: false,
  isHydrated: false,

  login: (user: DecodedUser, token?: string) => {
    if (token) writeSessionToken(token);
    useMoneyPrivacyStore.getState().setHideMoneyValues(true);
    set({ user, token: token ?? null, isAuthenticated: true, isHydrated: true });
  },

  logout: () => {
    clearSessionToken();
    useMoneyPrivacyStore.getState().setHideMoneyValues(true);
    set({ user: null, token: null, isAuthenticated: false });
  },

  hydrate: async () => {
    // Recupera o token da sessão (persiste durante a navegação na mesma aba)
    const sessionToken = readSessionToken();

    const headers: HeadersInit = {};
    if (sessionToken) {
      headers["Authorization"] = `Bearer ${sessionToken}`;
    }

    try {
      const res = await fetch(`${API_URL}/api/auth/me`, {
        credentials: "include",
        headers,
      });
      if (res.ok) {
        const user: DecodedUser = await res.json();
        useMoneyPrivacyStore.getState().setHideMoneyValues(true);
        set({ user, token: sessionToken, isAuthenticated: true, isHydrated: true });
      } else {
        clearSessionToken();
        useMoneyPrivacyStore.getState().setHideMoneyValues(true);
        set((state) =>
          state.isAuthenticated
            ? { isHydrated: true }
            : { user: null, token: null, isAuthenticated: false, isHydrated: true }
        );
      }
    } catch {
      clearSessionToken();
      useMoneyPrivacyStore.getState().setHideMoneyValues(true);
      set((state) =>
        state.isAuthenticated
          ? { isHydrated: true }
          : { user: null, token: null, isAuthenticated: false, isHydrated: true }
      );
    }
  },
}));
