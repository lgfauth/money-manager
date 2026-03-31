"use client";

import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { queryKeys } from "@/lib/query-client";
import { useAccounts } from "@/hooks/use-accounts";
import { useBudget } from "@/hooks/use-budgets";
import { useCategories } from "@/hooks/use-categories";
import type { TransactionResponseDto, PaginatedResponse } from "@/types/transaction";
import type { AccountResponseDto } from "@/types/account";
import { AccountType } from "@/types/account";
import { format } from "date-fns";

export interface DashboardData {
  accounts: AccountResponseDto[];
  recentTransactions: TransactionResponseDto[];
  monthlyIncome: number;
  monthlyExpense: number;
  netBalance: number;
  totalAssets: number;
  creditCards: AccountResponseDto[];
}

export function useDashboard() {
  const now = new Date();
  const month = format(now, "yyyy-MM");
  const startDate = format(new Date(now.getFullYear(), now.getMonth(), 1), "yyyy-MM-dd");
  const endDate = format(new Date(now.getFullYear(), now.getMonth() + 1, 0), "yyyy-MM-dd");

  const accounts = useAccounts();
  const budget = useBudget(month);
  const { data: categories } = useCategories();

  // Build category name map for resolving names (API doesn't return categoryName)
  const categoryNameMap: Record<string, string> = {};
  categories?.forEach((c) => {
    categoryNameMap[c.id] = c.name;
  });

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
    budget.isLoading;

  const accountList = accounts.data ?? [];
  const monthTx = allMonthTx.data?.items ?? [];

  const monthlyIncome = monthTx
    .filter((t) => t.type === "Income")
    .reduce((s, t) => s + t.amount, 0);

  const monthlyExpense = monthTx
    .filter((t) => t.type === "Expense")
    .reduce((s, t) => s + t.amount, 0);

  const netBalance = accountList
    .filter(
      (a) =>
        a.type !== AccountType.CreditCard &&
        a.type !== AccountType.Investment
    )
    .reduce((s, a) => s + a.balance, 0);

  const totalAssets = accountList
    .filter((a) => a.type !== AccountType.CreditCard)
    .reduce((s, a) => s + a.balance, 0);

  const creditCards = accountList.filter(
    (a) => a.type === AccountType.CreditCard
  );

  // Group expenses by category for donut chart
  const expensesByCategory = monthTx
    .filter((t) => t.type === "Expense")
    .reduce<Record<string, { name: string; amount: number }>>((acc, t) => {
      if (!acc[t.categoryId]) {
        acc[t.categoryId] = { name: categoryNameMap[t.categoryId] ?? t.categoryName ?? t.categoryId, amount: 0 };
      }
      acc[t.categoryId].amount += t.amount;
      return acc;
    }, {});

  // Group daily income/expense for area chart
  const dailyData = monthTx.reduce<
    Record<string, { date: string; income: number; expense: number }>
  >((acc, t) => {
    const day = t.date.split("T")[0];
    if (!acc[day]) acc[day] = { date: day, income: 0, expense: 0 };
    if (t.type === "Income") acc[day].income += t.amount;
    else if (t.type === "Expense") acc[day].expense += t.amount;
    return acc;
  }, {});

  return {
    isLoading,
    accounts: accountList,
    recentTransactions: transactions.data?.items ?? [],
    monthlyIncome,
    monthlyExpense,
    netBalance,
    totalAssets,
    creditCards,
    budget: budget.data,
    expensesByCategory: Object.entries(expensesByCategory).map(
      ([id, { name, amount }]) => ({ id, name, amount })
    ),
    dailyData: Object.values(dailyData).sort((a, b) =>
      a.date.localeCompare(b.date)
    ),
    month,
  };
}
