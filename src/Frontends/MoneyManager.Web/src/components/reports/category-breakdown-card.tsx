"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { ArrowDown, ArrowUp } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Tabs, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { DonutChart } from "@/components/charts/pie-chart";
import { getPercentChange } from "@/hooks/use-reports";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";
import { cn } from "@/lib/utils";
import type { ReportCategoryBreakdown, ReportPeriod } from "@/types/report";

type CategoryView = "expense" | "income";

const UNCATEGORIZED_ID = "uncategorized";

interface CategoryBreakdownCardProps {
  currency: string;
  period: ReportPeriod;
  expenses: ReportCategoryBreakdown[];
  incomes: ReportCategoryBreakdown[];
  showMonthlyAverage: boolean;
}

export function CategoryBreakdownCard({
  currency,
  period,
  expenses,
  incomes,
  showMonthlyAverage,
}: CategoryBreakdownCardProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();
  const [view, setView] = useState<CategoryView>("expense");
  const formatMoney = (value: number) => formatMonetaryValue(value, currency);

  const isExpenseView = view === "expense";
  const categories = isExpenseView ? expenses : incomes;
  const total = categories.reduce((sum, category) => sum + category.total, 0);
  const hasBudget = isExpenseView && categories.some((category) => category.budgetLimit !== null);

  const donutData = useMemo(
    () =>
      categories
        .filter((category) => category.total > 0)
        .map((category) => ({ name: category.categoryName, value: category.total, color: category.color })),
    [categories]
  );

  const buildTransactionsHref = (categoryId: string) =>
    `/transactions?categoryId=${categoryId}&startDate=${period.startDate.slice(0, 10)}&endDate=${period.endDate.slice(0, 10)}`;

  return (
    <Card>
      <CardHeader className="flex flex-row items-start justify-between gap-2 space-y-0">
        <div>
          <CardTitle className="text-base">Categorias</CardTitle>
          <p className="text-sm text-muted-foreground">
            Distribuição, comparação com o período anterior
            {hasBudget ? " e orçamento" : ""}
          </p>
        </div>
        <Tabs value={view} onValueChange={(value) => setView(value as CategoryView)}>
          <TabsList className="h-8">
            <TabsTrigger value="expense" className="text-xs px-2 h-6">
              Despesas
            </TabsTrigger>
            <TabsTrigger value="income" className="text-xs px-2 h-6">
              Receitas
            </TabsTrigger>
          </TabsList>
        </Tabs>
      </CardHeader>
      <CardContent>
        {categories.length === 0 ? (
          <p className="text-sm text-muted-foreground text-center py-10">
            {isExpenseView ? "Nenhuma despesa no período." : "Nenhuma receita no período."}
          </p>
        ) : (
          <div className="grid gap-6 lg:grid-cols-[280px_1fr] lg:items-start">
            <DonutChart
              data={donutData}
              centerValue={formatMoney(total)}
              centerLabel="Total"
              height={260}
              formatter={formatMoney}
            />
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b text-muted-foreground">
                    <th className="pb-2 text-left font-medium">Categoria</th>
                    <th className="pb-2 text-right font-medium">Total</th>
                    <th className="pb-2 text-right font-medium">%</th>
                    <th className="pb-2 text-right font-medium">vs anterior</th>
                    {showMonthlyAverage && (
                      <th className="pb-2 text-right font-medium hidden md:table-cell">Média/mês</th>
                    )}
                    {hasBudget && (
                      <th className="pb-2 pl-4 text-left font-medium hidden md:table-cell">Orçamento</th>
                    )}
                  </tr>
                </thead>
                <tbody>
                  {categories.map((category) => (
                    <tr key={category.categoryId} className="border-b last:border-0">
                      <td className="py-3">
                        <div className="flex items-center gap-2">
                          <span
                            className="h-3 w-3 rounded-full shrink-0"
                            style={{ backgroundColor: category.color }}
                          />
                          <div className="min-w-0">
                            {category.categoryId === UNCATEGORIZED_ID ? (
                              <span className="font-medium">{category.categoryName}</span>
                            ) : (
                              <Link
                                href={buildTransactionsHref(category.categoryId)}
                                className="font-medium hover:underline"
                                title="Ver transações das contas nesta categoria"
                              >
                                {category.categoryName}
                              </Link>
                            )}
                            <p className="text-xs text-muted-foreground">
                              {category.transactionCount} lançamento(s)
                            </p>
                          </div>
                        </div>
                      </td>
                      <td className="py-3 text-right font-medium whitespace-nowrap">
                        {formatMoney(category.total)}
                      </td>
                      <td className="py-3 text-right text-muted-foreground">
                        {category.percentage.toFixed(1)}%
                      </td>
                      <td className="py-3 text-right">
                        <ChangeIndicator
                          current={category.total}
                          previous={category.previousTotal}
                          isIncreaseBad={isExpenseView}
                        />
                      </td>
                      {showMonthlyAverage && (
                        <td className="py-3 text-right text-muted-foreground whitespace-nowrap hidden md:table-cell">
                          {formatMoney(category.monthlyAverage)}
                        </td>
                      )}
                      {hasBudget && (
                        <td className="py-3 pl-4 hidden md:table-cell">
                          <BudgetUsage
                            spent={category.total}
                            limit={category.budgetLimit}
                            formatMoney={formatMoney}
                          />
                        </td>
                      )}
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}
      </CardContent>
    </Card>
  );
}

interface ChangeIndicatorProps {
  current: number;
  previous: number;
  isIncreaseBad: boolean;
}

function ChangeIndicator({ current, previous, isIncreaseBad }: ChangeIndicatorProps) {
  const change = getPercentChange(current, previous);

  if (change === null) {
    return <span className="text-xs text-muted-foreground">novo</span>;
  }

  const isGood = isIncreaseBad ? change <= 0 : change >= 0;
  const Icon = change >= 0 ? ArrowUp : ArrowDown;

  return (
    <span
      className={cn(
        "inline-flex items-center gap-0.5 text-xs font-medium",
        isGood ? "text-income" : "text-expense"
      )}
    >
      <Icon className="h-3 w-3" />
      {Math.abs(change).toFixed(0)}%
    </span>
  );
}

interface BudgetUsageProps {
  spent: number;
  limit: number | null;
  formatMoney: (value: number) => string;
}

function BudgetUsage({ spent, limit, formatMoney }: BudgetUsageProps) {
  if (limit === null || limit <= 0) {
    return <span className="text-xs text-muted-foreground">—</span>;
  }

  const usage = (spent / limit) * 100;
  const isOverBudget = usage > 100;

  return (
    <div className="w-32 space-y-1">
      <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
        <div
          className={cn("h-full rounded-full", isOverBudget ? "bg-expense" : "bg-income")}
          style={{ width: `${Math.min(usage, 100)}%` }}
        />
      </div>
      <p className={cn("text-xs", isOverBudget ? "text-expense" : "text-muted-foreground")}>
        {usage.toFixed(0)}% de {formatMoney(limit)}
      </p>
    </div>
  );
}
