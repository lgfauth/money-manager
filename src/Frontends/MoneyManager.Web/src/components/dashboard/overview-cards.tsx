"use client";

import {
  Wallet,
  TrendingUp,
  ArrowDownLeft,
  ArrowUpRight,
} from "lucide-react";
import { StatCard } from "@/components/shared/stat-card";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";

interface OverviewCardsProps {
  netBalance: number;
  totalAssets: number;
  monthlyIncome: number;
  monthlyExpense: number;
}

export function OverviewCards({
  netBalance,
  totalAssets,
  monthlyIncome,
  monthlyExpense,
}: OverviewCardsProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();

  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <StatCard title="Saldo Liquido" value={formatMonetaryValue(netBalance)} icon={Wallet} />
      <StatCard
        title="Total em Ativos"
        value={formatMonetaryValue(totalAssets)}
        icon={TrendingUp}
        variant="default"
      />
      <StatCard
        title="Receitas do Mes"
        value={formatMonetaryValue(monthlyIncome)}
        icon={ArrowDownLeft}
        variant="income"
      />
      <StatCard
        title="Despesas do Mes"
        value={formatMonetaryValue(monthlyExpense)}
        icon={ArrowUpRight}
        variant="expense"
      />
    </div>
  );
}
