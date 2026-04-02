"use client";

import { useMemo } from "react";
import { useQuery } from "@tanstack/react-query";
import { format, subMonths, startOfMonth, endOfMonth } from "date-fns";
import { ptBR } from "date-fns/locale";
import { apiClient } from "@/lib/api-client";
import { queryKeys } from "@/lib/query-client";
import { useAccounts } from "@/hooks/use-accounts";
import type {
  TransactionResponseDto,
  PaginatedResponse,
} from "@/types/transaction";

interface ReportPeriod {
  startDate: string;
  endDate: string;
}

interface CategoryBreakdown {
  categoryId: string;
  categoryName: string;
  color: string;
  total: number;
  percentage: number;
}

interface AccountBreakdown {
  accountId: string;
  accountName: string;
  color: string;
  total: number;
  percentage: number;
}

interface MonthlyTrend {
  [key: string]: unknown;
  month: string;
  income: number;
  expense: number;
}

export interface ReportData {
  isLoading: boolean;
  totalIncome: number;
  totalExpense: number;
  netBalance: number;
  savingsRate: number;
  expensesByCategory: CategoryBreakdown[];
  movementByAccount: AccountBreakdown[];
  monthlyTrends: MonthlyTrend[];
}

export function useReports(period: ReportPeriod): ReportData {
  const { data: accounts } = useAccounts();

  const transactions = useQuery({
    queryKey: queryKeys.reports(period),
    queryFn: () =>
      apiClient.get<PaginatedResponse<TransactionResponseDto>>(
        `/api/transactions?page=1&pageSize=10000&sortBy=date&startDate=${period.startDate}&endDate=${period.endDate}`
      ),
  });

  const { totalIncome, totalExpense, expensesByCategory, movementByAccount, monthlyTrends } =
    useMemo(() => {
      const items = transactions.data?.items ?? [];
      const activeAccountIds = new Set((accounts ?? []).map((account) => account.id));
      const scopedItems =
        activeAccountIds.size > 0
          ? items.filter((transaction) => activeAccountIds.has(transaction.accountId))
          : items;

      let income = 0;
      let expense = 0;

      const categoryAcc: Record<
        string,
        { name: string; color: string; total: number }
      > = {};

      const accountAcc: Record<
        string,
        { name: string; color: string; total: number }
      > = {};

      const monthAcc: Record<
        string,
        { month: string; income: number; expense: number }
      > = {};

      for (const t of scopedItems) {
        if (t.type === "Income") {
          income += t.amount;
        } else if (t.type === "Expense") {
          expense += t.amount;

          if (!categoryAcc[t.categoryId]) {
            categoryAcc[t.categoryId] = {
              name: t.categoryName || t.categoryId,
              color: t.categoryColor || "#64748b",
              total: 0,
            };
          }
          categoryAcc[t.categoryId].total += t.amount;
        }

        if (t.accountId) {
          if (!accountAcc[t.accountId]) {
            accountAcc[t.accountId] = {
              name: t.accountName || t.accountId,
              color: t.accountColor || "#00C896",
              total: 0,
            };
          }
          accountAcc[t.accountId].total += Math.abs(t.amount);
        }

        const monthKey = t.date.substring(0, 7);
        if (!monthAcc[monthKey]) {
          const d = new Date(t.date);
          monthAcc[monthKey] = {
            month: format(d, "MMM yy", { locale: ptBR }),
            income: 0,
            expense: 0,
          };
        }
        if (t.type === "Income") monthAcc[monthKey].income += t.amount;
        else if (t.type === "Expense") monthAcc[monthKey].expense += t.amount;
      }

      const expByCategory = Object.entries(categoryAcc)
        .map(([categoryId, { name, color, total }]) => ({
          categoryId,
          categoryName: name,
          color,
          total,
          percentage: expense > 0 ? (total / expense) * 100 : 0,
        }))
        .sort((a, b) => b.total - a.total);

      const totalMovement = Object.values(accountAcc).reduce(
        (sum, account) => sum + account.total,
        0
      );

      const movementBreakdown = Object.entries(accountAcc)
        .map(([accountId, { name, color, total }]) => ({
          accountId,
          accountName: name,
          color,
          total,
          percentage: totalMovement > 0 ? (total / totalMovement) * 100 : 0,
        }))
        .sort((a, b) => b.total - a.total);

      const trends = Object.values(monthAcc).sort((a, b) =>
        a.month.localeCompare(b.month)
      );

      return {
        totalIncome: income,
        totalExpense: expense,
        expensesByCategory: expByCategory,
        movementByAccount: movementBreakdown,
        monthlyTrends: trends,
      };
    }, [transactions.data, accounts]);

  const netBalance = totalIncome - totalExpense;
  const savingsRate =
    totalIncome > 0 ? ((totalIncome - totalExpense) / totalIncome) * 100 : 0;

  return {
    isLoading: transactions.isLoading,
    totalIncome,
    totalExpense,
    netBalance,
    savingsRate,
    expensesByCategory,
    movementByAccount,
    monthlyTrends,
  };
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
export function getPresetPeriod(
  preset: "current" | "previous" | "3m" | "6m" | "year"
): ReportPeriod {
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
