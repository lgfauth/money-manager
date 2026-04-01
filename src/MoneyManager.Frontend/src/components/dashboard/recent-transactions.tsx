"use client";

import { format } from "date-fns";
import { ptBR } from "date-fns/locale";
import { cn } from "@/lib/utils";
import { TransactionType, type TransactionResponseDto } from "@/types/transaction";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import Link from "next/link";

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
            Nenhuma transação este mês.
          </p>
        ) : (
          <div className="space-y-3">
            {transactions.map((tx) => (
              <div
                key={tx.id}
                className="flex items-center justify-between text-sm"
              >
                <div className="flex items-center gap-3 min-w-0">
                  <span className="text-xs text-muted-foreground w-12 shrink-0">
                    {format(new Date(tx.date), "dd/MM", { locale: ptBR })}
                  </span>
                  <span className="truncate">{tx.description}</span>
                  <span className="hidden items-center gap-1.5 text-xs text-muted-foreground sm:inline-flex">
                    <ColorDot color={tx.categoryColor || "#64748b"} />
                    {tx.categoryName}
                  </span>
                </div>
                <span
                  className={cn(
                    "font-medium shrink-0 ml-2",
                    tx.type === TransactionType.Income
                      ? "text-income"
                      : "text-expense"
                  )}
                >
                  {tx.type === TransactionType.Income ? "+" : "-"}
                  {new Intl.NumberFormat("pt-BR", {
                    style: "currency",
                    currency: "BRL",
                  }).format(tx.amount)}
                </span>
              </div>
            ))}
          </div>
        )}
      </CardContent>
    </Card>
  );
}
