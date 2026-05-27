"use client";

import { useMemo } from "react";
import { BarChartComponent } from "@/components/charts/bar-chart";
import type { AccountResponseDto } from "@/types/account";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";

interface AccountsChartProps {
  accounts: AccountResponseDto[];
}

export function AccountsChart({ accounts }: AccountsChartProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();

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
              formatter={(value) => formatMonetaryValue(value)}
            />
          </div>

          <div className="saldos-summary-col">
            <p className="saldos-summary-label">RESUMO</p>

            <div className="saldos-total-block">
              <p className="saldos-total-label">Saldo total</p>
              <p className="saldos-total-val">{formatMonetaryValue(totalBalance)}</p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
