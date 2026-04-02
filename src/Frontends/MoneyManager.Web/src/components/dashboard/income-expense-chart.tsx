"use client";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { AreaChartComponent } from "@/components/charts/area-chart";

interface DailyData {
  date: string;
  income: number;
  expense: number;
}

interface IncomeExpenseChartProps {
  data: DailyData[];
}

export function IncomeExpenseChart({ data }: IncomeExpenseChartProps) {
  const chartData = data.map((d) => ({
    ...d,
    date: d.date.slice(5).replace("-", "/"),
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
          <AreaChartComponent
            data={chartData}
            xAxisKey="date"
            series={[
              { dataKey: "income", name: "Receita", color: "oklch(0.72 0.19 142)" },
              {
                dataKey: "expense",
                name: "Despesa",
                color: "oklch(0.63 0.24 25)",
              },
            ]}
            height={250}
          />
        )}
      </CardContent>
    </Card>
  );
}
