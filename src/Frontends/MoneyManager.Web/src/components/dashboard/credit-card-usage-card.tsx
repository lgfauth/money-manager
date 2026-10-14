"use client";

import Link from "next/link";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { CREDIT_LIMIT_THRESHOLD_DANGER, CREDIT_LIMIT_THRESHOLD_WARNING } from "@/config/constants";
import { formatReportDate } from "@/hooks/use-reports";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";
import { cn } from "@/lib/utils";
import type { CreditCardResponseDto } from "@/types/credit-card";

interface CreditCardUsageCardProps {
  cards: CreditCardResponseDto[];
}

function getUsageColor(usage: number) {
  if (usage >= CREDIT_LIMIT_THRESHOLD_DANGER) return "bg-expense";
  if (usage >= CREDIT_LIMIT_THRESHOLD_WARNING) return "bg-warning";
  return "bg-income";
}

export function CreditCardUsageCard({ cards }: CreditCardUsageCardProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm font-medium">Uso dos Cartões</CardTitle>
        <p className="text-xs text-muted-foreground">Limite comprometido e fatura atual de cada cartão</p>
      </CardHeader>
      <CardContent>
        {cards.length === 0 ? (
          <p className="text-sm text-muted-foreground text-center py-8">Nenhum cartão de crédito cadastrado.</p>
        ) : (
          <ul className="space-y-4">
            {cards.map((card) => {
              // O limite disponível informado pelo banco tem prioridade; sem ele, usa o saldo devedor.
              const used = card.availableLimit != null ? card.limit - card.availableLimit : card.currentBalance;
              const usage = card.limit > 0 ? (used / card.limit) * 100 : 0;
              const formatMoney = (value: number) => formatMonetaryValue(value, card.currency);

              return (
                <li key={card.id} className="space-y-1.5">
                  <div className="flex items-center justify-between gap-2 text-sm">
                    <Link href={`/credit-cards/${card.id}`} className="truncate font-medium hover:underline">
                      {card.name}
                    </Link>
                    <span className="whitespace-nowrap text-xs text-muted-foreground">
                      {formatMoney(used)} de {formatMoney(card.limit)}
                    </span>
                  </div>
                  <div className="h-2 w-full overflow-hidden rounded-full bg-muted">
                    <div
                      className={cn("h-full rounded-full", getUsageColor(usage))}
                      style={{ width: `${Math.min(Math.max(usage, 0), 100)}%` }}
                    />
                  </div>
                  <div className="flex items-center justify-between gap-2 text-xs text-muted-foreground">
                    <span>{usage.toFixed(0)}% do limite usado</span>
                    {card.currentInvoice && (
                      <span className="whitespace-nowrap">
                        Fatura atual {formatMoney(card.currentInvoice.totalAmount)} · vence{" "}
                        {formatReportDate(card.currentInvoice.dueDate)}
                      </span>
                    )}
                  </div>
                </li>
              );
            })}
          </ul>
        )}
      </CardContent>
    </Card>
  );
}
