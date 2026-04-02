"use client";

import { useState, useMemo } from "react";
import { format, startOfMonth, subMonths } from "date-fns";
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
import { CategoryLineChart, MonthlyHistogramChart } from "@/components/charts";
import { useReports, getPresetPeriod, getDefaultPeriod } from "@/hooks/use-reports";
import { useTransactions } from "@/hooks/use-transactions";
import { PeriodSelector } from "@/components/shared/period-selector";

type Preset = "current" | "previous" | "3m" | "6m" | "year" | "custom";

function formatCurrency(value: number): string {
  return value.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
}

export default function ReportsPage() {
  const [preset, setPreset] = useState<Preset>("current");
  const [period, setPeriod] = useState(getDefaultPeriod);
  const sixMonthsStart = format(startOfMonth(subMonths(new Date(), 5)), "yyyy-MM-dd");
  const sixMonthsEnd = format(new Date(), "yyyy-MM-dd");

  const report = useReports(period);
  const transactions6m = useTransactions({
    page: 1,
    pageSize: 10000,
    sortBy: "date",
    startDate: sixMonthsStart,
    endDate: sixMonthsEnd,
  });

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

  const accountDonutData = useMemo(
    () =>
      report.movementByAccount.map((account) => ({
        name: account.accountName,
        value: account.total,
        color: account.color,
      })),
    [report.movementByAccount]
  );

  const histogramData = useMemo(() => {
    const items = transactions6m.data?.items ?? [];
    const monthTotals = new Map<string, { income: number; expense: number }>();

    for (const transaction of items) {
      const monthKey = `${transaction.date.slice(0, 7)}-01`;
      const current = monthTotals.get(monthKey) ?? { income: 0, expense: 0 };

      if (transaction.type === "Income") {
        current.income += transaction.amount;
      } else if (transaction.type === "Expense") {
        current.expense += transaction.amount;
      }

      monthTotals.set(monthKey, current);
    }

    return Array.from(monthTotals.entries())
      .sort(([a], [b]) => a.localeCompare(b))
      .map(([time, values]) => ({
        time,
        income: values.income,
        expense: values.expense,
      }));
  }, [transactions6m.data?.items]);

  const categoryPalette = useMemo(
    () => ({
      "Alimentação": "#3b82f6",
      "Transporte": "#a855f7",
      "Lazer": "#f59e0b",
      Outros: "#22c55e",
    }),
    []
  );

  const categoryLineSeries = useMemo(() => {
    const items = transactions6m.data?.items ?? [];
    const monthKeys = new Set<string>();
    const categoryMonthTotals = new Map<string, Map<string, number>>();

    for (const transaction of items) {
      if (transaction.type !== "Expense") {
        continue;
      }

      const monthKey = `${transaction.date.slice(0, 7)}-01`;
      const categoryName = transaction.categoryName || "Outros";
      monthKeys.add(monthKey);

      const perCategory = categoryMonthTotals.get(categoryName) ?? new Map<string, number>();
      perCategory.set(monthKey, (perCategory.get(monthKey) ?? 0) + transaction.amount);
      categoryMonthTotals.set(categoryName, perCategory);
    }

    const orderedMonths = Array.from(monthKeys).sort((a, b) => a.localeCompare(b));
    const fallbackColors = ["#3b82f6", "#a855f7", "#f59e0b", "#22c55e", "#ef4444", "#06b6d4"];
    let colorIndex = 0;

    return Array.from(categoryMonthTotals.entries()).map(([name, monthMap]) => {
      const color =
        categoryPalette[name as keyof typeof categoryPalette] ?? fallbackColors[colorIndex++ % fallbackColors.length];

      return {
        name,
        color,
        data: orderedMonths.map((month) => ({
          time: month,
          value: monthMap.get(month) ?? 0,
        })),
      };
    });
  }, [transactions6m.data?.items, categoryPalette]);

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
            {/* Lightweight charts - trends */}
            <Card className="lg:col-span-2">
              <CardHeader>
                <CardTitle className="text-base">Receitas e Despesas (6 meses)</CardTitle>
              </CardHeader>
              <CardContent>
                {histogramData.length > 0 ? (
                  <MonthlyHistogramChart data={histogramData} height={300} />
                ) : (
                  <p className="text-sm text-muted-foreground text-center py-10">
                    Dados insuficientes para o gráfico.
                  </p>
                )}
              </CardContent>
            </Card>

            <div className="grid gap-6">
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

              <Card>
                <CardHeader>
                  <CardTitle className="text-base">
                    Movimentação por Conta
                  </CardTitle>
                </CardHeader>
                <CardContent>
                  {accountDonutData.length > 0 ? (
                    <DonutChart
                      data={accountDonutData}
                      centerValue={formatCurrency(
                        report.movementByAccount.reduce(
                          (sum, account) => sum + account.total,
                          0
                        )
                      )}
                      centerLabel="Movimento"
                      height={300}
                      formatter={formatCurrency}
                    />
                  ) : (
                    <p className="text-sm text-muted-foreground text-center py-10">
                      Nenhuma movimentação por conta no período.
                    </p>
                  )}
                </CardContent>
              </Card>
            </div>
          </div>

          <Card>
            <CardHeader>
              <CardTitle className="text-base">Tendência por Categoria (6 meses)</CardTitle>
            </CardHeader>
            <CardContent>
              {categoryLineSeries.length > 0 ? (
                <CategoryLineChart series={categoryLineSeries} height={300} />
              ) : (
                <p className="text-sm text-muted-foreground text-center py-10">
                  Sem despesas categorizadas para o período.
                </p>
              )}
            </CardContent>
          </Card>

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
