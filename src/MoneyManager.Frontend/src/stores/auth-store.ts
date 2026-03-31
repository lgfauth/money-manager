import { create } from "zustand";
import { SESSION_TOKEN_KEY } from "@/config/constants";

interface DecodedUser {
  id: string;
  name: string;
  email: string;
}

function decodeToken(token: string): DecodedUser | null {
  try {
    const payload = JSON.parse(atob(token.split(".")[1]));
    return {
      id: payload.nameid ?? payload.sub,
      name: payload.unique_name ?? payload.name,
      email: payload.email,
    };
  } catch {
    return null;
  }
}

function isTokenExpired(token: string): boolean {
  try {
    const payload = JSON.parse(atob(token.split(".")[1]));
    return payload.exp * 1000 < Date.now();
  } catch {
    return true;
  }
}

interface AuthState {
  token: string | null;
  user: DecodedUser | null;
  isAuthenticated: boolean;
  login: (token: string) => void;
  logout: () => void;
  hydrate: () => void;
}

export const useAuthStore = create<AuthState>((set) => ({
  token: null,
  user: null,
  isAuthenticated: false,

  login: (token: string) => {
    if (typeof window !== "undefined") {
      sessionStorage.setItem(SESSION_TOKEN_KEY, token);
    }
    const user = decodeToken(token);
    set({ token, user, isAuthenticated: !!user });
  },

  logout: () => {
    if (typeof window !== "undefined") {
      sessionStorage.removeItem(SESSION_TOKEN_KEY);
    }
    set({ token: null, user: null, isAuthenticated: false });
  },

  hydrate: () => {
    if (typeof window === "undefined") return;
    const token = sessionStorage.getItem(SESSION_TOKEN_KEY);
    if (token && !isTokenExpired(token)) {
      const user = decodeToken(token);
      set({ token, user, isAuthenticated: !!user });
    } else {
      if (token) sessionStorage.removeItem(SESSION_TOKEN_KEY);
      set({ token: null, user: null, isAuthenticated: false });
    }
  },
}));
