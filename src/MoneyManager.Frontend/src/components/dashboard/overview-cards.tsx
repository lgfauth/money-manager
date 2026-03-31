"use client";

import {
  Wallet,
  TrendingUp,
  ArrowDownLeft,
  ArrowUpRight,
} from "lucide-react";
import { StatCard } from "@/components/shared/stat-card";

interface OverviewCardsProps {
  netBalance: number;
  totalAssets: number;
  monthlyIncome: number;
  monthlyExpense: number;
}

const fmt = (v: number) =>
  new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(v);

export function OverviewCards({
  netBalance,
  totalAssets,
  monthlyIncome,
  monthlyExpense,
}: OverviewCardsProps) {
  return (
    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
      <StatCard title="Saldo Liquido" value={fmt(netBalance)} icon={Wallet} />
      <StatCard
        title="Total em Ativos"
        value={fmt(totalAssets)}
        icon={TrendingUp}
        variant="investment"
      />
      <StatCard
        title="Receitas do Mes"
        value={fmt(monthlyIncome)}
        icon={ArrowDownLeft}
        variant="income"
      />
      <StatCard
        title="Despesas do Mes"
        value={fmt(monthlyExpense)}
        icon={ArrowUpRight}
        variant="expense"
      />
    </div>
  );
}
