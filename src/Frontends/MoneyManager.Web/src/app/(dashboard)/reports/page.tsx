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
import { Button } from "@/components/ui/button";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { Progress } from "@/components/ui/progress";
import { PageHeader } from "@/components/shared/page-header";
import { StatCard } from "@/components/shared/stat-card";
import { CardGridSkeleton } from "@/components/shared/loading-skeleton";
import { EmptyState } from "@/components/shared/empty-state";
import { DonutChart } from "@/components/charts/pie-chart";
import { CategoryBarChart, RevenueExpenseLineChart } from "@/components/charts";
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
  const [selectedMonth, setSelectedMonth] = useState<string>(
    new Date().toISOString().slice(0, 7)
  );
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

  const availableMonths = useMemo(() => {
    const months = new Set<string>();
    const items = transactions6m.data?.items ?? [];

    for (const transaction of items) {
      months.add(transaction.date.slice(0, 7));
    }

    if (months.size === 0) {
      months.add(new Date().toISOString().slice(0, 7));
    }

    return Array.from(months).sort((a, b) => b.localeCompare(a));
  }, [transactions6m.data?.items]);

  const effectiveSelectedMonth =
    availableMonths.find((month) => month === selectedMonth) ?? availableMonths[0];

  const categoryBarData = useMemo(() => {
    const items = transactions6m.data?.items ?? [];
    const monthCategoryTotals = new Map<string, { name: string; color: string; value: number }>();

    for (const transaction of items) {
      if (
        transaction.type !== "Expense" ||
        transaction.date.slice(0, 7) !== effectiveSelectedMonth
      ) {
        continue;
      }

      const key = transaction.categoryId || transaction.categoryName || "outros";
      const current = monthCategoryTotals.get(key) ?? {
        name: transaction.categoryName || "Outros",
        color: transaction.categoryColor || "#22c55e",
        value: 0,
      };

      current.value += transaction.amount;
      monthCategoryTotals.set(key, current);
    }

    return Array.from(monthCategoryTotals.values())
      .filter((category) => category.value > 0)
      .sort((a, b) => b.value - a.value);
  }, [transactions6m.data?.items, effectiveSelectedMonth]);

  const revenueExpenseLineData = useMemo(
    () => histogramData.map((point) => ({ time: point.time, income: point.income, expense: point.expense })),
    [histogramData]
  );

  const averageIncome = useMemo(() => {
    if (histogramData.length === 0) {
      return 0;
    }

    const totalIncome = histogramData.reduce((sum, point) => sum + point.income, 0);
    return totalIncome / histogramData.length;
  }, [histogramData]);

  const averageExpense = useMemo(() => {
    if (histogramData.length === 0) {
      return 0;
    }

    const totalExpense = histogramData.reduce((sum, point) => sum + point.expense, 0);
    return totalExpense / histogramData.length;
  }, [histogramData]);

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
          <div className="grid gap-6 lg:grid-cols-2">
            <Card>
              <CardHeader>
                <CardTitle className="text-base">Gastos por Categoria</CardTitle>
                <p className="text-sm text-muted-foreground">Selecione o mês</p>
              </CardHeader>
              <CardContent>
                <div className="mb-4 flex flex-wrap gap-2">
                  {availableMonths.map((month) => (
                    <Button
                      key={month}
                      size="sm"
                      variant={month === effectiveSelectedMonth ? "default" : "outline"}
                      onClick={() => setSelectedMonth(month)}
                      className="rounded-full px-3 text-xs"
                    >
                      {month}
                    </Button>
                  ))}
                </div>

                {categoryBarData.length > 0 ? (
                  <CategoryBarChart data={categoryBarData} />
                ) : (
                  <p className="text-sm text-muted-foreground text-center py-10">
                    Sem despesas para o mês selecionado.
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
              <CardTitle className="text-base">Receitas e Despesas</CardTitle>
              <p className="text-sm text-muted-foreground">Últimos 6 meses</p>
            </CardHeader>
            <CardContent>
              <div className="mb-4 grid gap-2 sm:grid-cols-2">
                <div>
                  <p className="text-xs text-muted-foreground">Receita média do período</p>
                  <p className="text-sm font-semibold text-green-600">{formatCurrency(averageIncome)}</p>
                </div>
                <div>
                  <p className="text-xs text-muted-foreground">Despesa média do período</p>
                  <p className="text-sm font-semibold text-red-600">{formatCurrency(averageExpense)}</p>
                </div>
              </div>

              {revenueExpenseLineData.length > 0 ? (
                <RevenueExpenseLineChart data={revenueExpenseLineData} height={220} />
              ) : (
                <p className="text-sm text-muted-foreground text-center py-10">
                  Sem dados suficientes para o período.
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
