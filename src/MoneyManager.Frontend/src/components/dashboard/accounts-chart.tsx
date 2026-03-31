"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
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
    color: a.color,
  }));

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm font-medium">Saldos por Conta</CardTitle>
      </CardHeader>
      <CardContent>
        {chartData.length === 0 ? (
          <p className="text-sm text-muted-foreground text-center py-8">
            Nenhuma conta cadastrada.
          </p>
        ) : (
          <BarChartComponent
            data={chartData}
            layout="horizontal"
            height={250}
            formatter={fmt}
          />
        )}
      </CardContent>
    </Card>
  );
}
