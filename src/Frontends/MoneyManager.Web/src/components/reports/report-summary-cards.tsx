"use client";

import { DollarSign, PiggyBank, TrendingDown, TrendingUp } from "lucide-react";
import { StatCard } from "@/components/shared/stat-card";
import { getPercentChange } from "@/hooks/use-reports";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";
import type { ReportTotals } from "@/types/report";

const comparisonLabel = "vs período anterior";

interface ReportSummaryCardsProps {
  currency: string;
  totals: ReportTotals;
  previousTotals: ReportTotals;
}

function buildTrend(current: number, previous: number, isIncreaseBad = false) {
  const change = getPercentChange(current, previous);
  return change === null ? undefined : { value: change, label: comparisonLabel, isIncreaseBad };
}

export function ReportSummaryCards({ currency, totals, previousTotals }: ReportSummaryCardsProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();
  const formatMoney = (value: number) => formatMonetaryValue(value, currency);

  const savingsRateTrend =
    totals.savingsRate !== null && previousTotals.savingsRate !== null
      ? {
          value: totals.savingsRate - previousTotals.savingsRate,
          label: comparisonLabel,
          unit: " p.p.",
        }
      : undefined;

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <StatCard
        title="Total Receitas"
        value={formatMoney(totals.income)}
        icon={TrendingUp}
        variant="income"
        trend={buildTrend(totals.income, previousTotals.income)}
      />
      <StatCard
        title="Total Despesas"
        value={formatMoney(totals.expense)}
        icon={TrendingDown}
        variant="expense"
        trend={buildTrend(totals.expense, previousTotals.expense, true)}
      />
      <StatCard
        title="Saldo Líquido"
        value={formatMoney(totals.netBalance)}
        icon={DollarSign}
        variant={totals.netBalance >= 0 ? "income" : "expense"}
        trend={buildTrend(totals.netBalance, previousTotals.netBalance)}
      />
      <StatCard
        title="Taxa de Poupança"
        value={totals.savingsRate === null ? "—" : `${totals.savingsRate.toFixed(1)}%`}
        icon={PiggyBank}
        variant={totals.savingsRate !== null && totals.savingsRate >= 0 ? "income" : "warning"}
        trend={savingsRateTrend}
      />
    </div>
  );
}
