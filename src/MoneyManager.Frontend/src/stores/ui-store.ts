import { create } from "zustand";

interface UIState {
  sidebarOpen: boolean;
  sidebarCollapsed: boolean;
  commandOpen: boolean;
  toggleSidebar: () => void;
  toggleCollapsed: () => void;
  toggleCommand: () => void;
  setSidebarOpen: (open: boolean) => void;
}

export const useUIStore = create<UIState>((set) => ({
  sidebarOpen: true,
  sidebarCollapsed: false,
  commandOpen: false,

  toggleSidebar: () => set((s) => ({ sidebarOpen: !s.sidebarOpen })),
  toggleCollapsed: () => {
    set((s) => {
      const next = !s.sidebarCollapsed;
      if (typeof window !== "undefined") {
        localStorage.setItem("sidebarCollapsed", String(next));
      }
      return { sidebarCollapsed: next };
    });
  },
  toggleCommand: () => set((s) => ({ commandOpen: !s.commandOpen })),
  setSidebarOpen: (open: boolean) => set({ sidebarOpen: open }),
}));
