"use client";

import { AlertTriangle } from "lucide-react";
import {
  useDashboardOverview,
  useDashboardSummary,
  useRecentTransactions,
} from "@/hooks/use-dashboard";
import { useAccounts } from "@/hooks/use-accounts";
import { useCreditCards } from "@/hooks/use-credit-cards";
import { useSnapshotStatus } from "@/hooks/use-financial-health";
import { formatReportDate } from "@/hooks/use-reports";
import { DashboardSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { OverviewCards } from "@/components/dashboard/overview-cards";
import { UpcomingBillsCard } from "@/components/dashboard/upcoming-bills-card";
import { BudgetUsageChart } from "@/components/dashboard/budget-usage-chart";
import { CreditCardUsageCard } from "@/components/dashboard/credit-card-usage-card";
import { AccountsChart } from "@/components/dashboard/accounts-chart";
import { RecentTransactions } from "@/components/dashboard/recent-transactions";
import { DashboardBlock } from "@/components/dashboard/dashboard-block";
import { DailyFlowCard } from "@/components/reports/daily-flow-card";
import { CheckinBanner } from "@/components/financial-health/checkin-banner";
import { BankConnectionBanner } from "@/components/bank-connections/bank-connection-banner";

export default function DashboardPage() {
  const overview = useDashboardOverview();
  const summary = useDashboardSummary();
  const accounts = useAccounts();
  const creditCards = useCreditCards();
  const recentTransactions = useRecentTransactions();
  const { data: snapshotStatus } = useSnapshotStatus();

  if (overview.isLoading || summary.isLoading) return <DashboardSkeleton />;

  const report = overview.data;
  const position = summary.data;

  return (
    <div className="space-y-6">
      <BankConnectionBanner />
      {snapshotStatus?.showBanner && <CheckinBanner status={snapshotStatus} />}

      {!report || !position ? (
        <EmptyState
          icon={AlertTriangle}
          title="Não foi possível carregar o resumo"
          description="Tente novamente em alguns instantes."
        />
      ) : (
        <>
          <p className="text-xs text-muted-foreground">
            Mês atual até {formatReportDate(report.period.endDate)}, em regime de competência
            (compras no cartão contam na data de cada parcela). Patrimônio líquido = saldo das
            contas − faturas em aberto.
          </p>

          <OverviewCards
            summary={position}
            totals={report.totals}
            previousTotals={report.previousTotals}
          />

          <div className="grid gap-4 lg:grid-cols-3">
            <div className="lg:col-span-2">
              <DailyFlowCard currency={report.currency} points={report.dailyPoints} />
            </div>
            <UpcomingBillsCard
              currency={position.currency}
              invoices={position.upcomingInvoices}
              recurring={position.upcomingRecurring}
            />
          </div>

          <BudgetUsageChart currency={report.currency} categories={report.expensesByCategory} />
        </>
      )}

      <div className="grid gap-4 lg:grid-cols-2">
        <DashboardBlock title="Uso dos Cartões" isLoading={creditCards.isLoading} isError={creditCards.isError}>
          <CreditCardUsageCard cards={creditCards.data ?? []} />
        </DashboardBlock>
        <DashboardBlock title="Saldos por Conta" isLoading={accounts.isLoading} isError={accounts.isError}>
          <AccountsChart accounts={accounts.data ?? []} />
        </DashboardBlock>
      </div>

      <DashboardBlock
        title="Transações Recentes"
        isLoading={recentTransactions.isLoading}
        isError={recentTransactions.isError}
      >
        <RecentTransactions transactions={recentTransactions.data?.items ?? []} />
      </DashboardBlock>
    </div>
  );
}
