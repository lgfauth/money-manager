"use client";

import { useMemo } from "react";
import {
  Bar,
  CartesianGrid,
  ComposedChart,
  Legend,
  Line,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { ChartTooltip } from "@/components/charts/chart-tooltip";
import { formatReportMonth } from "@/hooks/use-reports";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";
import type { ReportMonthlyTrend } from "@/types/report";

interface MonthlyTrendCardProps {
  currency: string;
  trends: ReportMonthlyTrend[];
}

export function MonthlyTrendCard({ currency, trends }: MonthlyTrendCardProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();
  const data = useMemo(
    () => trends.map((trend) => ({ ...trend, label: formatReportMonth(trend.month) })),
    [trends]
  );

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Evolução Mensal</CardTitle>
        <p className="text-sm text-muted-foreground">Receitas, despesas e saldo de cada mês</p>
      </CardHeader>
      <CardContent>
        <ResponsiveContainer width="100%" height={280}>
          <ComposedChart data={data} margin={{ top: 5, right: 20, bottom: 5, left: 0 }} barGap={2}>
            <CartesianGrid strokeDasharray="3 3" className="stroke-border" vertical={false} />
            <XAxis dataKey="label" tick={{ fontSize: 12 }} className="text-muted-foreground" />
            <YAxis hide />
            <Tooltip
              content={({ active, payload, label }) => (
                <ChartTooltip
                  active={active}
                  label={label as string}
                  payload={payload?.map((p) => ({
                    name: String(p.name ?? ""),
                    value: p.value as number,
                    color: p.color ?? "",
                  }))}
                  formatter={(value) => formatMonetaryValue(value, currency)}
                />
              )}
            />
            <Legend />
            <Bar dataKey="income" name="Receitas" fill="var(--color-income)" radius={[4, 4, 0, 0]} maxBarSize={28} />
            <Bar dataKey="expense" name="Despesas" fill="var(--color-expense)" radius={[4, 4, 0, 0]} maxBarSize={28} />
            <Line
              type="monotone"
              dataKey="netBalance"
              name="Saldo do mês"
              stroke="var(--color-primary)"
              strokeWidth={2}
              dot={{ r: 4 }}
            />
          </ComposedChart>
        </ResponsiveContainer>
      </CardContent>
    </Card>
  );
}
