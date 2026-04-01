"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { DonutChart } from "@/components/charts/pie-chart";
import { COLOR_PRESETS } from "@/config/constants";

interface CategoryData {
  id: string;
  name: string;
  amount: number;
  color: string;
}

interface BudgetUsageChartProps {
  data: CategoryData[];
}

const fmt = (v: number) =>
  new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: "BRL",
  }).format(v);

export function BudgetUsageChart({ data }: BudgetUsageChartProps) {
  const total = data.reduce((s, d) => s + d.amount, 0);
  const chartData = data.map((d, i) => ({
    name: d.name,
    value: d.amount,
    color: d.color || COLOR_PRESETS[i % COLOR_PRESETS.length],
  }));

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm font-medium">
          Despesas por Categoria
        </CardTitle>
      </CardHeader>
      <CardContent>
        {chartData.length === 0 ? (
          <p className="text-sm text-muted-foreground text-center py-8">
            Sem despesas no periodo.
          </p>
        ) : (
          <DonutChart
            data={chartData}
            centerLabel="Total"
            centerValue={fmt(total)}
            height={250}
            formatter={fmt}
          />
        )}
      </CardContent>
    </Card>
  );
}
