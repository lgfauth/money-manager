"use client";

import { useQuery } from "@tanstack/react-query";
import { format, subMonths, startOfMonth, endOfMonth, parseISO } from "date-fns";
import { ptBR } from "date-fns/locale";
import { apiClient } from "@/lib/api-client";
import { queryKeys } from "@/lib/query-client";
import type { ReportBasis, ReportOverview, ReportPeriod } from "@/types/report";

export type ReportPreset = "current" | "previous" | "3m" | "6m" | "year";

export function useReportOverview(period: ReportPeriod, basis: ReportBasis) {
  return useQuery({
    queryKey: queryKeys.reports({ ...period, basis }),
    queryFn: () =>
      apiClient.get<ReportOverview>(
        `/api/reports/overview?startDate=${period.startDate}&endDate=${period.endDate}&basis=${basis}`
      ),
  });
}

// Helper for initial period (current month)
export function getDefaultPeriod(): ReportPeriod {
  const now = new Date();
  return {
    startDate: format(startOfMonth(now), "yyyy-MM-dd"),
    endDate: format(endOfMonth(now), "yyyy-MM-dd"),
  };
}

// Preset periods
export function getPresetPeriod(preset: ReportPreset): ReportPeriod {
  const now = new Date();
  let start: Date;
  let end: Date;

  switch (preset) {
    case "current":
      start = startOfMonth(now);
      end = endOfMonth(now);
      break;
    case "previous":
      start = startOfMonth(subMonths(now, 1));
      end = endOfMonth(subMonths(now, 1));
      break;
    case "3m":
      start = startOfMonth(subMonths(now, 2));
      end = endOfMonth(now);
      break;
    case "6m":
      start = startOfMonth(subMonths(now, 5));
      end = endOfMonth(now);
      break;
    case "year":
      start = new Date(now.getFullYear(), 0, 1);
      end = endOfMonth(now);
      break;
  }

  return {
    startDate: format(start, "yyyy-MM-dd"),
    endDate: format(end, "yyyy-MM-dd"),
  };
}

// A API devolve datas com fuso (UTC); usar só a parte da data evita que o dia "volte" no fuso local.
export function parseReportDate(value: string): Date {
  return parseISO(value.slice(0, 10));
}

export function formatReportDate(value: string, pattern = "dd/MM"): string {
  return format(parseReportDate(value), pattern, { locale: ptBR });
}

export function formatReportMonth(month: string): string {
  return format(parseISO(`${month}-01`), "MMM/yy", { locale: ptBR });
}

// Variação percentual entre dois valores; null quando não há base de comparação.
export function getPercentChange(current: number, previous: number): number | null {
  if (previous === 0) return null;
  return ((current - previous) / Math.abs(previous)) * 100;
}
