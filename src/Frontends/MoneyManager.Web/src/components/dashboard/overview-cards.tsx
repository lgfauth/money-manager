"use client";

import { ArrowDownLeft, ArrowUpRight, Landmark, Scale } from "lucide-react";
import { StatCard } from "@/components/shared/stat-card";
import { getPercentChange } from "@/hooks/use-reports";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";
import type { DashboardSummary } from "@/types/dashboard";
import type { ReportTotals } from "@/types/report";

const comparisonLabel = "vs mesmo período do mês anterior";

interface OverviewCardsProps {
  summary: DashboardSummary;
  totals: ReportTotals;
  previousTotals: ReportTotals;
}

function buildTrend(current: number, previous: number, isIncreaseBad = false) {
  const change = getPercentChange(current, previous);
  return change === null ? undefined : { value: change, label: comparisonLabel, isIncreaseBad };
}

export function OverviewCards({ summary, totals, previousTotals }: OverviewCardsProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();
  const formatMoney = (value: number) => formatMonetaryValue(value, summary.currency);

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <StatCard
        title="Patrimônio Líquido"
        value={formatMoney(summary.netWorth)}
        icon={Scale}
        variant={summary.netWorth >= 0 ? "default" : "expense"}
      />
      <StatCard
        title="Disponível em Contas"
        value={formatMoney(summary.accountsBalance)}
        icon={Landmark}
      />
      <StatCard
        title="Receitas do Mês"
        value={formatMoney(totals.income)}
        icon={ArrowDownLeft}
        variant="income"
        trend={buildTrend(totals.income, previousTotals.income)}
      />
      <StatCard
        title="Despesas do Mês"
        value={formatMoney(totals.expense)}
        icon={ArrowUpRight}
        variant="expense"
        trend={buildTrend(totals.expense, previousTotals.expense, true)}
      />
    </div>
  );
}
