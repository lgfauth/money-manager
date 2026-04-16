"use client";

import { useMemo } from "react";
import { format, startOfMonth, subMonths } from "date-fns";
import { useDashboard } from "@/hooks/use-dashboard";
import { useTransactions } from "@/hooks/use-transactions";
import { DashboardSkeleton } from "@/components/shared/loading-skeleton";
import { OverviewCards } from "@/components/dashboard/overview-cards";
import { IncomeExpenseChart } from "@/components/dashboard/income-expense-chart";
import { BudgetUsageChart } from "@/components/dashboard/budget-usage-chart";
import { AccountsChart } from "@/components/dashboard/accounts-chart";
import { RecentTransactions } from "@/components/dashboard/recent-transactions";
import {
  BalanceAreaChart,
  RevenueExpenseLineChart,
  SpendingBaselineChart,
} from "@/components/charts";

export default function DashboardPage() {
  const startDate = format(startOfMonth(subMonths(new Date(), 5)), "yyyy-MM-dd");
  const endDate = format(new Date(), "yyyy-MM-dd");

  const {
    isLoading,
    netBalance,
    totalAssets,
    monthlyIncome,
    monthlyExpense,
    budget,
    accounts,
    recentTransactions,
    expensesByCategory,
    dailyData,
  } = useDashboard();

  const monthlyTransactions = useTransactions({
    page: 1,
    pageSize: 10000,
    sortBy: "date",
    startDate,
    endDate,
  });

  const histogramData = useMemo(() => {
    const items = monthlyTransactions.data?.items ?? [];
    const monthlyMap = new Map<string, { income: number; expense: number }>();

    for (const transaction of items) {
      const monthKey = `${transaction.date.slice(0, 7)}-01`;
      const current = monthlyMap.get(monthKey) ?? { income: 0, expense: 0 };

      if (transaction.type === "Income") {
        current.income += transaction.amount;
      } else if (transaction.type === "Expense") {
        current.expense += transaction.amount;
      }

      monthlyMap.set(monthKey, current);
    }

    return Array.from(monthlyMap.entries())
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([time, totals]) => ({
        time,
        income: totals.income,
        expense: totals.expense,
      }));
  }, [monthlyTransactions.data?.items]);

  const balanceAreaData = useMemo(() => {
    let runningBalance = 0;
    return histogramData.map((point) => {
      runningBalance += point.income - point.expense;
      return { time: point.time, value: runningBalance };
    });
  }, [histogramData]);

  const spendingBaselineData = useMemo(
    () => histogramData.map((point) => ({ time: point.time, value: point.expense })),
    [histogramData]
  );

  const monthlyBudget =
    budget?.items?.reduce((sum, item) => sum + item.limitAmount, 0) ?? 0;

  if (isLoading) return <DashboardSkeleton />;

  return (
    <div className="space-y-6">
      <OverviewCards
        netBalance={netBalance}
        totalAssets={totalAssets}
        monthlyIncome={monthlyIncome}
        monthlyExpense={monthlyExpense}
      />

      <div className="grid gap-4 lg:grid-cols-3">
        <IncomeExpenseChart data={dailyData} />
        <BudgetUsageChart data={expensesByCategory} />
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="rounded-xl border border-border bg-card p-4 lg:col-span-2">
          <h3 className="mb-3 text-sm font-medium text-foreground">Saldo Acumulado (6 meses)</h3>
          <BalanceAreaChart data={balanceAreaData} height={240} />
        </div>
        <div className="rounded-xl border border-border bg-card p-4">
          <h3 className="mb-3 text-sm font-medium text-foreground">Gasto vs Orçamento</h3>
          <SpendingBaselineChart data={spendingBaselineData} budget={monthlyBudget} height={240} />
        </div>
      </div>

      <div className="rounded-xl border border-border bg-card p-4">
        <h3 className="mb-3 text-sm font-medium text-foreground">Receitas e Despesas (6 meses)</h3>
        <RevenueExpenseLineChart data={histogramData} height={200} />
      </div>

      <AccountsChart accounts={accounts} />

      <RecentTransactions transactions={recentTransactions} />
    </div>
  );
}
