import { create } from "zustand";

interface MoneyPrivacyState {
  hideMoneyValues: boolean;
  toggleMoneyVisibility: () => void;
  setHideMoneyValues: (hide: boolean) => void;
}

export const useMoneyPrivacyStore = create<MoneyPrivacyState>((set) => ({
  hideMoneyValues: true,
  toggleMoneyVisibility: () =>
    set((state) => ({ hideMoneyValues: !state.hideMoneyValues })),
  setHideMoneyValues: (hide: boolean) => set({ hideMoneyValues: hide }),
}));