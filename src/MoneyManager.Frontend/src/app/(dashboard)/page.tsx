"use client";

import { useDashboard } from "@/hooks/use-dashboard";
import { DashboardSkeleton } from "@/components/shared/loading-skeleton";
import { OverviewCards } from "@/components/dashboard/overview-cards";
import { IncomeExpenseChart } from "@/components/dashboard/income-expense-chart";
import { BudgetUsageChart } from "@/components/dashboard/budget-usage-chart";
import { AccountsChart } from "@/components/dashboard/accounts-chart";
import { RecentTransactions } from "@/components/dashboard/recent-transactions";
import { CreditCardSummary } from "@/components/dashboard/credit-card-summary";

export default function DashboardPage() {
  const {
    isLoading,
    netBalance,
    totalAssets,
    monthlyIncome,
    monthlyExpense,
    accounts,
    recentTransactions,
    creditCards,
    expensesByCategory,
    dailyData,
  } = useDashboard();

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

      <div className="grid gap-4 lg:grid-cols-2">
        <AccountsChart accounts={accounts} />
        <CreditCardSummary cards={creditCards} />
      </div>

      <RecentTransactions transactions={recentTransactions} />
    </div>
  );
}
