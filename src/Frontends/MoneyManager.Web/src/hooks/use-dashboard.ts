"use client";

import { useQuery } from "@tanstack/react-query";
import { format } from "date-fns";
import { apiClient } from "@/lib/api-client";
import { queryKeys } from "@/lib/query-client";
import { getDefaultPeriod } from "@/hooks/use-reports";
import type { DashboardSummary } from "@/types/dashboard";
import type { ReportOverview } from "@/types/report";
import type { TransactionResponseDto, PaginatedResponse } from "@/types/transaction";

const RECENT_TRANSACTIONS_LIMIT = 10;

// Relatório do mês atual em regime de competência — mesmos números da página de Relatórios.
export function useDashboardOverview() {
  const period = getDefaultPeriod();

  return useQuery({
    queryKey: queryKeys.dashboardOverview(period.startDate.slice(0, 7)),
    queryFn: () =>
      apiClient.get<ReportOverview>(
        `/api/reports/overview?startDate=${period.startDate}&endDate=${period.endDate}&basis=Accrual`
      ),
  });
}

export function useDashboardSummary() {
  return useQuery({
    queryKey: queryKeys.dashboardSummary,
    queryFn: () => apiClient.get<DashboardSummary>("/api/dashboard/summary"),
  });
}

// Últimos lançamentos até hoje, sem restringir ao mês (no início do mês a lista não fica vazia)
// e sem lançamentos agendados para o futuro.
export function useRecentTransactions() {
  const today = format(new Date(), "yyyy-MM-dd");

  return useQuery({
    queryKey: queryKeys.dashboardRecentTransactions(today),
    queryFn: () =>
      apiClient.get<PaginatedResponse<TransactionResponseDto>>(
        `/api/transactions?page=1&pageSize=${RECENT_TRANSACTIONS_LIMIT}&sortBy=date&endDate=${today}`
      ),
  });
}
