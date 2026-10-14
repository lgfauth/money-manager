"use client";

import { CreditCard, Landmark } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { formatReportDate } from "@/hooks/use-reports";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";
import type { ReportTopExpense } from "@/types/report";

interface TopExpensesCardProps {
  currency: string;
  expenses: ReportTopExpense[];
}

export function TopExpensesCard({ currency, expenses }: TopExpensesCardProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-base">Maiores Despesas</CardTitle>
        <p className="text-sm text-muted-foreground">Os lançamentos de maior valor no período</p>
      </CardHeader>
      <CardContent>
        {expenses.length === 0 ? (
          <p className="text-sm text-muted-foreground text-center py-10">Nenhuma despesa no período.</p>
        ) : (
          <ul className="divide-y">
            {expenses.map((expense, index) => {
              const SourceIcon = expense.sourceType === "credit_card" ? CreditCard : Landmark;
              return (
                <li key={`${expense.description}-${expense.date}-${index}`} className="flex items-center gap-3 py-2.5">
                  <span
                    className="h-8 w-1 rounded-full shrink-0"
                    style={{ backgroundColor: expense.categoryColor }}
                  />
                  <div className="min-w-0 flex-1">
                    <p className="truncate text-sm font-medium">
                      {expense.description}
                      {expense.installmentLabel && (
                        <span className="ml-1 text-xs text-muted-foreground">({expense.installmentLabel})</span>
                      )}
                    </p>
                    <p className="flex items-center gap-1 text-xs text-muted-foreground">
                      <span>{formatReportDate(expense.date)}</span>
                      <span>·</span>
                      <span>{expense.categoryName}</span>
                      <span>·</span>
                      <SourceIcon className="h-3 w-3" />
                      <span className="truncate">{expense.sourceName}</span>
                    </p>
                  </div>
                  <span className="text-sm font-semibold whitespace-nowrap">
                    {formatMonetaryValue(expense.amount, currency)}
                  </span>
                </li>
              );
            })}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
