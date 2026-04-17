"use client";

import { useMemo } from "react";
import { BarChartComponent } from "@/components/charts/bar-chart";
import type { AccountResponseDto } from "@/types/account";

const fmt = (v: number) =>
  new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(v);

interface AccountsChartProps {
  accounts: AccountResponseDto[];
}

export function AccountsChart({ accounts }: AccountsChartProps) {
  const chartData = accounts.map((a) => ({
    name: a.name,
    value: a.balance,
    color: a.color || "#00C896",
  }));

  const totalBalance = useMemo(
    () => accounts.reduce((sum, a) => sum + a.balance, 0),
    [accounts]
  );

  return (
    <div className="saldos-card">
      <p className="saldos-title">Saldos por Conta</p>

      {chartData.length === 0 ? (
        <p className="text-sm text-center py-8" style={{ color: "#8B9AB0" }}>
          Nenhuma conta cadastrada.
        </p>
      ) : (
        <div className="saldos-grid">
          <div className="saldos-chart-col">
            <BarChartComponent
              data={chartData}
              layout="horizontal"
              height={220}
              formatter={fmt}
            />
          </div>

          <div className="saldos-summary-col">
            <p className="saldos-summary-label">RESUMO</p>

            <div className="saldos-total-block">
              <p className="saldos-total-label">Saldo total</p>
              <p className="saldos-total-val">{fmt(totalBalance)}</p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
