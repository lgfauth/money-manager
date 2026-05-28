"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { LineChartComponent } from "@/components/charts/line-chart";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";

interface DailyData {
  date: string;
  accountTotal: number;
  accumulatedExpense: number;
}

interface IncomeExpenseChartProps {
  data: DailyData[];
}

export function IncomeExpenseChart({ data }: IncomeExpenseChartProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();

  const chartData = data.map((d) => ({
    date: d.date.slice(5).replace("-", "/"),
    accountTotal: d.accountTotal,
    accumulatedExpense: d.accumulatedExpense,
  }));

  return (
    <Card className="col-span-full lg:col-span-2">
      <CardHeader>
        <CardTitle className="text-sm font-medium">
          Receitas vs Despesas
        </CardTitle>
      </CardHeader>
      <CardContent>
        {chartData.length === 0 ? (
          <p className="text-sm text-muted-foreground text-center py-8">
            Sem dados para o período.
          </p>
        ) : (
          <LineChartComponent
            data={chartData}
            xAxisKey="date"
            series={[
              {
                dataKey: "accountTotal",
                name: "Saldo em contas",
                color: "oklch(0.72 0.19 142)",
              },
              {
                dataKey: "accumulatedExpense",
                name: "Despesa acumulada",
                color: "oklch(0.63 0.24 25)",
              },
            ]}
            height={250}
            formatter={formatMonetaryValue}
          />
        )}
      </CardContent>
    </Card>
  );
}
