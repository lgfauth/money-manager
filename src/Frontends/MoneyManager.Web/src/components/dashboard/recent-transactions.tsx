"use client";

import { cn } from "@/lib/utils";
import { formatReportDate } from "@/hooks/use-reports";
import { TransactionType, type TransactionResponseDto } from "@/types/transaction";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import Link from "next/link";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";

function ColorDot({ color }: { color: string }) {
  return (
    <span
      className="inline-block h-2.5 w-2.5 rounded-full shrink-0"
      style={{ backgroundColor: color }}
    />
  );
}

interface RecentTransactionsProps {
  transactions: TransactionResponseDto[];
}

export function RecentTransactions({ transactions }: RecentTransactionsProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();

  return (
    <Card className="col-span-full">
      <CardHeader className="flex flex-row items-center justify-between">
        <CardTitle className="text-sm font-medium">
          Transações Recentes
        </CardTitle>
        <Link
          href="/transactions"
          className="text-xs text-primary hover:underline"
        >
          Ver todas →
        </Link>
      </CardHeader>
      <CardContent>
        {transactions.length === 0 ? (
          <p className="text-sm text-muted-foreground text-center py-4">
            Nenhuma transação registrada.
          </p>
        ) : (
          <div className="space-y-3">
            {transactions.map((tx) => {
              const isIncome = tx.type === TransactionType.Income;
              const isExpense = tx.type === TransactionType.Expense;

              return (
                <div
                  key={tx.id}
                  className="flex items-center justify-between text-sm"
                >
                  <div className="flex items-center gap-3 min-w-0">
                    <span className="text-xs text-muted-foreground w-12 shrink-0">
                      {formatReportDate(tx.date)}
                    </span>
                    <span className="truncate">{tx.description}</span>
                    <span className="hidden items-center gap-1.5 text-xs text-muted-foreground sm:inline-flex">
                      <ColorDot color={tx.categoryColor || "#64748b"} />
                      {tx.categoryName}
                    </span>
                  </div>
                  {/* Transferências entre contas não são receita nem despesa: valor neutro, sem sinal */}
                  <span
                    className={cn(
                      "font-medium shrink-0 ml-2",
                      isIncome ? "text-income" : isExpense ? "text-expense" : "text-muted-foreground"
                    )}
                  >
                    {isIncome ? "+" : isExpense ? "-" : ""}
                    {formatMonetaryValue(tx.amount, tx.currency)}
                  </span>
                </div>
              );
            })}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
