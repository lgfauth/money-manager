import { create } from "zustand";

interface SettingsState {
  theme: string;
  locale: string;
  currency: string;
  setTheme: (theme: string) => void;
  setLocale: (locale: string) => void;
  setCurrency: (currency: string) => void;
}

export const useSettingsStore = create<SettingsState>((set) => ({
  theme: "system",
  locale: "pt-BR",
  currency: "BRL",

  setTheme: (theme: string) => set({ theme }),
  setLocale: (locale: string) => set({ locale }),
  setCurrency: (currency: string) => set({ currency }),
}));
