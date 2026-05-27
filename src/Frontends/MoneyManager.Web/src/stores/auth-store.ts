import { create } from "zustand";
import { useMoneyPrivacyStore } from "@/stores/money-privacy-store";

interface DecodedUser {
  id: string;
  name: string;
  email: string;
}

interface AuthState {
  user: DecodedUser | null;
  isAuthenticated: boolean;
  isHydrated: boolean;
  login: (user: DecodedUser) => void;
  logout: () => void;
  hydrate: () => Promise<void>;
}

const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "";

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isAuthenticated: false,
  isHydrated: false,

  login: (user: DecodedUser) => {
    useMoneyPrivacyStore.getState().setHideMoneyValues(true);
    set({ user, isAuthenticated: true });
  },

  logout: () => {
    useMoneyPrivacyStore.getState().setHideMoneyValues(true);
    set({ user: null, isAuthenticated: false });
  },

  hydrate: async () => {
    try {
      const res = await fetch(`${API_URL}/api/auth/me`, {
        credentials: "include",
      });
      if (res.ok) {
        const user: DecodedUser = await res.json();
        useMoneyPrivacyStore.getState().setHideMoneyValues(true);
        set({ user, isAuthenticated: true, isHydrated: true });
      } else {
        useMoneyPrivacyStore.getState().setHideMoneyValues(true);
        set({ user: null, isAuthenticated: false, isHydrated: true });
      }
    } catch {
      useMoneyPrivacyStore.getState().setHideMoneyValues(true);
      set({ user: null, isAuthenticated: false, isHydrated: true });
    }
  },
}));
