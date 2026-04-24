import { useEffect } from "react";
import { create } from "zustand";

interface BreadcrumbState {
  labels: Record<string, string>;
  setLabel: (segment: string, label: string) => void;
  clearLabel: (segment: string) => void;
}

export const useBreadcrumbStore = create<BreadcrumbState>((set) => ({
  labels: {},
  setLabel: (segment, label) =>
    set((s) => {
      if (s.labels[segment] === label) return s;
      return { labels: { ...s.labels, [segment]: label } };
    }),
  clearLabel: (segment) =>
    set((s) => {
      if (!(segment in s.labels)) return s;
      const next = { ...s.labels };
      delete next[segment];
      return { labels: next };
    }),
}));

export function useBreadcrumbLabel(
  segment: string | undefined,
  label: string | undefined | null
) {
  const setLabel = useBreadcrumbStore((s) => s.setLabel);
  const clearLabel = useBreadcrumbStore((s) => s.clearLabel);

  useEffect(() => {
    if (!segment || !label) return;
    setLabel(segment, label);
    return () => clearLabel(segment);
  }, [segment, label, setLabel, clearLabel]);
}
