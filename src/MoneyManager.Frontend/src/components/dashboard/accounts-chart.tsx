"use client";

import { useMemo } from "react";
import { BarChartComponent } from "@/components/charts/bar-chart";
import type { AccountResponseDto } from "@/types/account";
import { AccountType } from "@/types/account";

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
    color: a.type === AccountType.CreditCard ? "#4E9BFF" : "#00C896",
  }));

  const { checkingTotal, creditTotal, netBalance } = useMemo(() => {
    let checking = 0;
    let credit = 0;
    for (const a of accounts) {
      if (a.type === AccountType.CreditCard) {
        credit += Math.abs(a.balance);
      } else {
        checking += a.balance;
      }
    }
    return { checkingTotal: checking, creditTotal: credit, netBalance: checking - credit };
  }, [accounts]);

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

            <div className="saldos-summary-item">
              <div>
                <p className="saldos-summary-name">Contas</p>
                <p className="saldos-summary-type">Corrente</p>
              </div>
              <span className="saldos-summary-val green">{fmt(checkingTotal)}</span>
            </div>

            <div className="saldos-summary-item">
              <div>
                <p className="saldos-summary-name">Cartões</p>
                <p className="saldos-summary-type">Crédito</p>
              </div>
              <span className="saldos-summary-val blue">{fmt(creditTotal)}</span>
            </div>

            <div className="saldos-total-block">
              <p className="saldos-total-label">Saldo líquido</p>
              <p className="saldos-total-val">{fmt(netBalance)}</p>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
