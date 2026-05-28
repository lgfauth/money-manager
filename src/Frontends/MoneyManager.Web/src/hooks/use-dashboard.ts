"use client";

import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { queryKeys } from "@/lib/query-client";
import { useAccounts } from "@/hooks/use-accounts";
import { useBudget } from "@/hooks/use-budgets";
import { useCreditCards } from "@/hooks/use-credit-cards";
import type { TransactionResponseDto, PaginatedResponse } from "@/types/transaction";
import type { AccountResponseDto } from "@/types/account";
import { format } from "date-fns";

export interface DashboardData {
  accounts: AccountResponseDto[];
  recentTransactions: TransactionResponseDto[];
  monthlyIncome: number;
  monthlyExpense: number;
  netBalance: number;
  totalAssets: number;
}

export function useDashboard() {
  const now = new Date();
  const month = format(now, "yyyy-MM");
  const startDate = format(new Date(now.getFullYear(), now.getMonth(), 1), "yyyy-MM-dd");
  const endDate = format(new Date(now.getFullYear(), now.getMonth() + 1, 0), "yyyy-MM-dd");

  const accounts = useAccounts();
  const budget = useBudget(month);
  const creditCards = useCreditCards();

  const transactions = useQuery({
    queryKey: queryKeys.transactions({
      page: 1,
      pageSize: 10,
      sortBy: "date",
      startDate,
      endDate,
    }),
    queryFn: () =>
      apiClient.get<PaginatedResponse<TransactionResponseDto>>(
        `/api/transactions?page=1&pageSize=10&sortBy=date&startDate=${startDate}&endDate=${endDate}`
      ),
  });

  const allMonthTx = useQuery({
    queryKey: ["dashboard", "month-totals", month],
    queryFn: () =>
      apiClient.get<PaginatedResponse<TransactionResponseDto>>(
        `/api/transactions?page=1&pageSize=9999&sortBy=date&startDate=${startDate}&endDate=${endDate}`
      ),
  });

  const isLoading =
    accounts.isLoading ||
    transactions.isLoading ||
    allMonthTx.isLoading ||
    budget.isLoading ||
    creditCards.isLoading;

  const accountList = accounts.data ?? [];
  const creditCardList = creditCards.data ?? [];
  const monthTx = allMonthTx.data?.items ?? [];

  const monthlyIncome = monthTx
    .filter((t) => t.type === "Income")
    .reduce((s, t) => s + t.amount, 0);

  const monthlyExpense = monthTx
    .filter((t) => t.type === "Expense")
    .reduce((s, t) => s + t.amount, 0);

  const netBalance = accountList
    .reduce((s, a) => s + a.balance, 0);

  const totalAssets = accountList
    .reduce((s, a) => s + a.balance, 0);

  const totalAvailableLimit = creditCardList.reduce(
    (sum, card) => sum + card.availableLimit,
    0
  );

  const totalCreditCardExpense = creditCardList.reduce(
    (sum, card) => sum + card.currentBalance,
    0
  );

  // Group expenses by category for donut chart
  const expensesByCategory = monthTx
    .filter((t) => t.type === "Expense")
    .reduce<Record<string, { name: string; amount: number; color: string }>>((acc, t) => {
      if (!acc[t.categoryId]) {
        acc[t.categoryId] = {
          name: t.categoryName || t.categoryId,
          amount: 0,
          color: t.categoryColor || "#64748b",
        };
      }
      acc[t.categoryId].amount += t.amount;
      return acc;
    }, {});

  const expenseByDay = monthTx
    .filter((transaction) => transaction.type === "Expense")
    .reduce<Record<string, number>>((acc, transaction) => {
      const day = transaction.date.split("T")[0];
      acc[day] = (acc[day] ?? 0) + transaction.amount;
      return acc;
    }, {});

  const accountExpenseTrend: Array<{
    date: string;
    accountTotal: number;
    accumulatedExpense: number;
  }> = [];

  const cursor = new Date(now.getFullYear(), now.getMonth(), 1);
  const today = new Date(now.getFullYear(), now.getMonth(), now.getDate());
  let accumulatedExpense = 0;

  while (cursor <= today) {
    const dayKey = format(cursor, "yyyy-MM-dd");
    accumulatedExpense += expenseByDay[dayKey] ?? 0;

    accountExpenseTrend.push({
      date: dayKey,
      accountTotal: totalAssets,
      accumulatedExpense,
    });

    cursor.setDate(cursor.getDate() + 1);
  }

  const creditLimitExpenseData = creditCardList.map((card) => ({
    cardName: card.name,
    availableLimit: card.availableLimit,
    expense: card.currentBalance,
  }));

  return {
    isLoading,
    accounts: accountList,
    recentTransactions: transactions.data?.items ?? [],
    monthlyIncome,
    monthlyExpense,
    netBalance,
    totalAssets,
    budget: budget.data,
    expensesByCategory: Object.entries(expensesByCategory).map(
      ([id, { name, amount, color }]) => ({ id, name, amount, color })
    ),
    accountExpenseTrend,
    creditLimitExpenseData,
    totalAvailableLimit,
    totalCreditCardExpense,
    month,
  };
}
