"use client";

import { useDashboard } from "@/hooks/use-dashboard";
import { DashboardSkeleton } from "@/components/shared/loading-skeleton";
import { OverviewCards } from "@/components/dashboard/overview-cards";
import { IncomeExpenseChart } from "@/components/dashboard/income-expense-chart";
import { BudgetUsageChart } from "@/components/dashboard/budget-usage-chart";
import { CreditLimitExpenseChart } from "@/components/dashboard/credit-limit-expense-chart";
import { AccountsChart } from "@/components/dashboard/accounts-chart";
import { RecentTransactions } from "@/components/dashboard/recent-transactions";

export default function DashboardPage() {
  const {
    isLoading,
    netBalance,
    totalAssets,
    monthlyIncome,
    monthlyExpense,
    accounts,
    recentTransactions,
    expensesByCategory,
    accountExpenseTrend,
    creditLimitExpenseData,
    totalAvailableLimit,
    totalCreditCardExpense,
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
        <IncomeExpenseChart data={accountExpenseTrend} />
        <BudgetUsageChart data={expensesByCategory} />
      </div>

      <div className="grid gap-4 lg:grid-cols-3">
        <div className="lg:col-span-2">
          <CreditLimitExpenseChart
            data={creditLimitExpenseData}
            totalAvailableLimit={totalAvailableLimit}
            totalExpense={totalCreditCardExpense}
          />
        </div>
      </div>

      <AccountsChart accounts={accounts} />

      <RecentTransactions transactions={recentTransactions} />
    </div>
  );
}
