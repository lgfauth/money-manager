"use client";

import { useMemo, useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { AreaChartComponent } from "@/components/charts/area-chart";
import { LineChartComponent } from "@/components/charts/line-chart";
import { formatReportDate } from "@/hooks/use-reports";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";
import type { ReportDailyPoint } from "@/types/report";

type DailyView = "balance" | "accumulated";

interface DailyFlowCardProps {
  currency: string;
  points: ReportDailyPoint[];
}

export function DailyFlowCard({ currency, points }: DailyFlowCardProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();
  const [view, setView] = useState<DailyView>("accumulated");
  const formatMoney = (value: number) => formatMonetaryValue(value, currency);

  const data = useMemo(() => {
    let accumulatedIncome = 0;
    let accumulatedExpense = 0;

    return points.map((point) => {
      accumulatedIncome += point.income;
      accumulatedExpense += point.expense;
      return {
        date: formatReportDate(point.date),
        balance: point.balance,
        accumulatedIncome,
        accumulatedExpense,
      };
    });
  }, [points]);

  return (
    <Card>
      <CardHeader className="flex flex-row items-start justify-between gap-2 space-y-0">
        <div>
          <CardTitle className="text-base">Fluxo Diário</CardTitle>
          <p className="text-sm text-muted-foreground">
            {view === "accumulated"
              ? "Receitas e despesas acumuladas no período"
              : "Saldo das contas ao fim de cada dia"}
          </p>
        </div>
        <Tabs value={view} onValueChange={(value) => setView(value as DailyView)}>
          <TabsList className="h-8">
            <TabsTrigger value="accumulated" className="text-xs px-2 h-6">
              Acumulado
            </TabsTrigger>
            <TabsTrigger value="balance" className="text-xs px-2 h-6">
              Saldo
            </TabsTrigger>
          </TabsList>
        </Tabs>
      </CardHeader>
      <CardContent>
        {view === "accumulated" ? (
          <LineChartComponent
            data={data}
            xAxisKey="date"
            series={[
              { dataKey: "accumulatedIncome", name: "Receitas acumuladas", color: "var(--color-income)" },
              { dataKey: "accumulatedExpense", name: "Despesas acumuladas", color: "var(--color-expense)" },
            ]}
            height={260}
            formatter={formatMoney}
          />
        ) : (
          <AreaChartComponent
            data={data}
            xAxisKey="date"
            series={[{ dataKey: "balance", name: "Saldo em contas", color: "var(--color-primary)" }]}
            height={260}
            formatter={formatMoney}
          />
        )}
      </CardContent>
    </Card>
  );
}
