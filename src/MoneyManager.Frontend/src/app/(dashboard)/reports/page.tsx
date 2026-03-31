"use client";

import { useState, useMemo } from "react";
import {
  DollarSign,
  TrendingDown,
  TrendingUp,
  PiggyBank,
} from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Progress } from "@/components/ui/progress";
import { PageHeader } from "@/components/shared/page-header";
import { StatCard } from "@/components/shared/stat-card";
import { CardGridSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { DonutChart } from "@/components/charts/pie-chart";
import { AreaChartComponent } from "@/components/charts/area-chart";
import { useReports, getPresetPeriod, getDefaultPeriod } from "@/hooks/use-reports";
import { PeriodSelector } from "@/components/shared/period-selector";

type Preset = "current" | "previous" | "3m" | "6m" | "year" | "custom";

function formatCurrency(value: number): string {
  return value.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

export default function ReportsPage() {
  const [preset, setPreset] = useState<Preset>("current");
  const [period, setPeriod] = useState(getDefaultPeriod);

  const report = useReports(period);

  const handlePreset = (p: string) => {
    const val = p as Preset;
    setPreset(val);
    if (val !== "custom") {
      setPeriod(getPresetPeriod(val as Exclude<Preset, "custom">));
    }
  };

  const handlePeriodChange = (start: string, end: string) => {
    setPeriod({ startDate: start, endDate: end });
    setPreset("custom");
  };

  const donutData = useMemo(
    () =>
      report.expensesByCategory.map((c) => ({
        name: c.categoryName,
        value: c.total,
        color: c.color,
      })),
    [report.expensesByCategory]
  );

  const trendSeries = useMemo(
    () => [
      { dataKey: "income", name: "Receitas", color: "hsl(var(--income))" },
      { dataKey: "expense", name: "Despesas", color: "hsl(var(--expense))" },
    ],
    []
  );

  if (report.isLoading) {
    return (
      <div className="space-y-6">
        <PageHeader title="Relatórios" description="Análise financeira detalhada" />
        <CardGridSkeleton count={4} />
      </div>
    );
  }

  const hasData =
    report.totalIncome > 0 ||
    report.totalExpense > 0;

  return (
    <div className="space-y-6">
      <PageHeader title="Relatórios" description="Análise financeira detalhada" />

      {/* Period selector */}
      <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
        <PeriodSelector
          startDate={period.startDate}
          endDate={period.endDate}
          onChange={handlePeriodChange}
          showPresets={false}
        />
        <Tabs value={preset} onValueChange={handlePreset}>
          <TabsList className="h-8">
            <TabsTrigger value="current" className="text-xs px-2 h-6">
              Atual
            </TabsTrigger>
            <TabsTrigger value="previous" className="text-xs px-2 h-6">
              Anterior
            </TabsTrigger>
            <TabsTrigger value="3m" className="text-xs px-2 h-6">
              3M
            </TabsTrigger>
            <TabsTrigger value="6m" className="text-xs px-2 h-6">
              6M
            </TabsTrigger>
            <TabsTrigger value="year" className="text-xs px-2 h-6">
              Ano
            </TabsTrigger>
          </TabsList>
        </Tabs>
      </div>

      {!hasData ? (
        <EmptyState
          icon={DollarSign}
          title="Sem dados para o período"
          description="Não há transações registradas neste período."
        />
      ) : (
        <>
          {/* Stat cards */}
          <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
            <StatCard
              title="Total Receitas"
              value={formatCurrency(report.totalIncome)}
              icon={TrendingUp}
              variant="income"
            />
            <StatCard
              title="Total Despesas"
              value={formatCurrency(report.totalExpense)}
              icon={TrendingDown}
              variant="expense"
            />
            <StatCard
              title="Saldo Líquido"
              value={formatCurrency(report.netBalance)}
              icon={DollarSign}
              variant={report.netBalance >= 0 ? "income" : "expense"}
            />
            <StatCard
              title="Taxa de Poupança"
              value={`${report.savingsRate.toFixed(1)}%`}
              icon={PiggyBank}
              variant={report.savingsRate >= 0 ? "income" : "warning"}
            />
          </div>

          {/* Charts */}
          <div className="grid gap-6 lg:grid-cols-3">
            {/* Area chart - trends */}
            <Card className="lg:col-span-2">
              <CardHeader>
                <CardTitle className="text-base">Tendências Mensais</CardTitle>
              </CardHeader>
              <CardContent>
                {report.monthlyTrends.length > 0 ? (
                  <AreaChartComponent
                    data={report.monthlyTrends}
                    series={trendSeries}
                    xAxisKey="month"
                    height={300}
                    formatter={formatCurrency}
                  />
                ) : (
                  <p className="text-sm text-muted-foreground text-center py-10">
                    Dados insuficientes para o gráfico.
                  </p>
                )}
              </CardContent>
            </Card>

            {/* Donut chart - expenses by category */}
            <Card>
              <CardHeader>
                <CardTitle className="text-base">
                  Despesas por Categoria
                </CardTitle>
              </CardHeader>
              <CardContent>
                {donutData.length > 0 ? (
                  <DonutChart
                    data={donutData}
                    centerValue={formatCurrency(report.totalExpense)}
                    centerLabel="Total"
                    height={300}
                    formatter={formatCurrency}
                  />
                ) : (
                  <p className="text-sm text-muted-foreground text-center py-10">
                    Nenhuma despesa no período.
                  </p>
                )}
              </CardContent>
            </Card>
          </div>

          {/* Category breakdown table */}
          {report.expensesByCategory.length > 0 && (
            <Card>
              <CardHeader>
                <CardTitle className="text-base">
                  Detalhamento por Categoria
                </CardTitle>
              </CardHeader>
              <CardContent>
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b text-muted-foreground">
                        <th className="pb-2 text-left font-medium">
                          Categoria
                        </th>
                        <th className="pb-2 text-right font-medium">Total</th>
                        <th className="pb-2 text-right font-medium">
                          % do Total
                        </th>
                        <th className="pb-2 text-left font-medium pl-4 hidden sm:table-cell">
                          Progresso
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      {report.expensesByCategory.map((cat) => (
                        <tr
                          key={cat.categoryId}
                          className="border-b last:border-0"
                        >
                          <td className="py-3">
                            <div className="flex items-center gap-2">
                              <span
                                className="h-3 w-3 rounded-full shrink-0"
                                style={{ backgroundColor: cat.color }}
                              />
                              <span className="font-medium">
                                {cat.categoryName}
                              </span>
                            </div>
                          </td>
                          <td className="py-3 text-right font-medium">
                            {formatCurrency(cat.total)}
                          </td>
                          <td className="py-3 text-right text-muted-foreground">
                            {cat.percentage.toFixed(1)}%
                          </td>
                          <td className="py-3 pl-4 hidden sm:table-cell">
                            <Progress
                              value={cat.percentage}
                              className="h-2 w-24"
                            />
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </CardContent>
            </Card>
          )}
        </>
      )}
    </div>
  );
}
