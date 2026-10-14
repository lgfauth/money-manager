"use client";

import { useState } from "react";
import { cn } from "@/lib/utils";
import { AlertTriangle, DollarSign } from "lucide-react";
import { PageHeader } from "@/components/shared/page-header";
import { CardGridSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { ReportFilters } from "@/components/reports/report-filters";
import { ReportSummaryCards } from "@/components/reports/report-summary-cards";
import { ReportDataQualityAlert } from "@/components/reports/report-data-quality-alert";
import { MonthlyTrendCard } from "@/components/reports/monthly-trend-card";
import { DailyFlowCard } from "@/components/reports/daily-flow-card";
import { CategoryBreakdownCard } from "@/components/reports/category-breakdown-card";
import { SourcesBreakdownCard } from "@/components/reports/sources-breakdown-card";
import { UpcomingInstallmentsCard } from "@/components/reports/upcoming-installments-card";
import { TopExpensesCard } from "@/components/reports/top-expenses-card";
import {
  formatReportDate,
  getDefaultPeriod,
  getPresetPeriod,
  useReportOverview,
  type ReportPreset,
} from "@/hooks/use-reports";
import type { ReportBasis, ReportPeriod } from "@/types/report";

const basisLabels: Record<ReportBasis, string> = {
  Accrual: "competência",
  Cash: "caixa",
};

function formatPeriodRange(period: ReportPeriod) {
  return `${formatReportDate(period.startDate, "dd/MM/yy")} a ${formatReportDate(period.endDate, "dd/MM/yy")}`;
}

export default function ReportsPage() {
  const [preset, setPreset] = useState<ReportPreset | "custom">("current");
  const [period, setPeriod] = useState(getDefaultPeriod);
  const [basis, setBasis] = useState<ReportBasis>("Accrual");

  const overview = useReportOverview(period, basis);
  const report = overview.data;
  const isRefreshing = overview.isPlaceholderData && overview.isFetching;

  const handlePresetChange = (value: ReportPreset) => {
    setPreset(value);
    setPeriod(getPresetPeriod(value));
  };

  const handlePeriodChange = (start: string, end: string) => {
    setPeriod({ startDate: start, endDate: end });
    setPreset("custom");
  };

  const hasData =
    !!report &&
    (report.totals.income !== 0 ||
      report.totals.expense !== 0 ||
      report.upcomingInstallments.length > 0);

  return (
    <div className="space-y-6">
      <PageHeader title="Relatórios" description="Análise financeira detalhada" />

      <ReportFilters
        period={period}
        preset={preset}
        basis={basis}
        onPeriodChange={handlePeriodChange}
        onPresetChange={handlePresetChange}
        onBasisChange={setBasis}
      />

      {overview.isLoading ? (
        <CardGridSkeleton count={4} />
      ) : overview.isError || !report ? (
        <EmptyState
          icon={AlertTriangle}
          title="Não foi possível carregar o relatório"
          description="Tente novamente em alguns instantes."
        />
      ) : !hasData ? (
        <EmptyState
          icon={DollarSign}
          title="Sem dados para o período"
          description="Não há transações registradas neste período."
        />
      ) : (
        <div
          className={cn("space-y-6 transition-opacity", isRefreshing && "pointer-events-none opacity-60")}
          aria-busy={isRefreshing}
        >
          <p className="text-xs text-muted-foreground">
            {isRefreshing && <span className="font-medium text-foreground">Atualizando… </span>}
            Apurado de {formatPeriodRange(report.period)} em regime de {basisLabels[report.basis]}, comparado
            com {formatPeriodRange(report.previousPeriod)}.
          </p>

          <ReportSummaryCards
            currency={report.currency}
            totals={report.totals}
            previousTotals={report.previousTotals}
          />

          <ReportDataQualityAlert
            currency={report.currency}
            dataQuality={report.dataQuality}
            otherCurrencies={report.otherCurrencies}
          />

          {report.monthlyTrends.length > 1 && (
            <MonthlyTrendCard currency={report.currency} trends={report.monthlyTrends} />
          )}

          <div className="grid gap-6 lg:grid-cols-2">
            <DailyFlowCard currency={report.currency} points={report.dailyPoints} />
            <TopExpensesCard currency={report.currency} expenses={report.topExpenses} />
          </div>

          <CategoryBreakdownCard
            currency={report.currency}
            period={report.period}
            expenses={report.expensesByCategory}
            incomes={report.incomesByCategory}
            showMonthlyAverage={report.monthlyTrends.length > 1}
          />

          <div className="grid gap-6 lg:grid-cols-2">
            <SourcesBreakdownCard
              currency={report.currency}
              accounts={report.byAccount}
              creditCards={report.byCreditCard}
            />
            <UpcomingInstallmentsCard
              currency={report.currency}
              installments={report.upcomingInstallments}
            />
          </div>
        </div>
      )}
    </div>
  );
}
