"use client";

import Link from "next/link";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { DonutChart } from "@/components/charts/pie-chart";
import { BUDGET_THRESHOLD_DANGER, BUDGET_THRESHOLD_WARNING } from "@/config/constants";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";
import { cn } from "@/lib/utils";
import type { ReportCategoryBreakdown } from "@/types/report";

const TOP_CATEGORIES_LIMIT = 6;

interface BudgetUsageChartProps {
  currency: string;
  categories: ReportCategoryBreakdown[];
}

function getUsageColor(usage: number) {
  if (usage >= BUDGET_THRESHOLD_DANGER) return "bg-expense";
  if (usage >= BUDGET_THRESHOLD_WARNING) return "bg-warning";
  return "bg-income";
}

export function BudgetUsageChart({ currency, categories }: BudgetUsageChartProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();
  const formatMoney = (value: number) => formatMonetaryValue(value, currency);
  const positiveCategories = categories.filter((category) => category.total > 0);
  const total = positiveCategories.reduce((sum, category) => sum + category.total, 0);

  return (
    <Card>
      <CardHeader className="flex flex-row items-start justify-between gap-2 space-y-0">
        <div>
          <CardTitle className="text-sm font-medium">Despesas do Mês por Categoria</CardTitle>
          <p className="text-xs text-muted-foreground">Inclui compras no cartão, com orçamento quando definido</p>
        </div>
        <Link href="/reports" className="text-xs text-primary hover:underline whitespace-nowrap">
          Ver relatório →
        </Link>
      </CardHeader>
      <CardContent>
        {positiveCategories.length === 0 ? (
          <p className="text-sm text-muted-foreground text-center py-8">Sem despesas no mês.</p>
        ) : (
          <div className="grid gap-6 md:grid-cols-[220px_1fr] md:items-center">
            <DonutChart
              data={positiveCategories.map((category) => ({
                name: category.categoryName,
                value: category.total,
                color: category.color,
              }))}
              centerLabel="Total"
              centerValue={formatMoney(total)}
              height={220}
              formatter={formatMoney}
            />
            <ul className="space-y-3">
              {positiveCategories.slice(0, TOP_CATEGORIES_LIMIT).map((category) => {
                const usage =
                  category.budgetLimit && category.budgetLimit > 0
                    ? (category.total / category.budgetLimit) * 100
                    : null;

                return (
                  <li key={category.categoryId} className="space-y-1">
                    <div className="flex items-center justify-between gap-2 text-sm">
                      <span className="flex min-w-0 items-center gap-2">
                        <span
                          className="h-2.5 w-2.5 rounded-full shrink-0"
                          style={{ backgroundColor: category.color }}
                        />
                        <span className="truncate font-medium">{category.categoryName}</span>
                      </span>
                      <span className="whitespace-nowrap font-medium">{formatMoney(category.total)}</span>
                    </div>
                    {usage !== null && category.budgetLimit !== null && (
                      <>
                        <div className="h-1.5 w-full overflow-hidden rounded-full bg-muted">
                          <div
                            className={cn("h-full rounded-full", getUsageColor(usage))}
                            style={{ width: `${Math.min(usage, 100)}%` }}
                          />
                        </div>
                        <p
                          className={cn(
                            "text-xs",
                            usage > 100 ? "text-expense" : "text-muted-foreground"
                          )}
                        >
                          {usage.toFixed(0)}% do orçamento de {formatMoney(category.budgetLimit)}
                        </p>
                      </>
                    )}
                  </li>
                );
              })}
            </ul>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
