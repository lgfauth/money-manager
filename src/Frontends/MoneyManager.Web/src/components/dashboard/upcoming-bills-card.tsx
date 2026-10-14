"use client";

import Link from "next/link";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { formatReportDate, formatReportMonth } from "@/hooks/use-reports";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";
import { cn } from "@/lib/utils";
import type { DashboardUpcomingInvoice, DashboardUpcomingRecurring } from "@/types/dashboard";

const MAX_VISIBLE_ITEMS = 8;

const invoiceStatusLabels: Record<DashboardUpcomingInvoice["status"], string> = {
  Pending: "Futura",
  Open: "Aberta",
  Closed: "Fechada",
  Paid: "Paga",
  Overdue: "Vencida",
};

interface UpcomingBillsCardProps {
  currency: string;
  invoices: DashboardUpcomingInvoice[];
  recurring: DashboardUpcomingRecurring[];
}

export function UpcomingBillsCard({ currency, invoices, recurring }: UpcomingBillsCardProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();
  const isEmpty = invoices.length === 0 && recurring.length === 0;

  // Faturas têm prioridade (já vêm ordenadas por vencimento); as recorrências ocupam as vagas restantes.
  const visibleInvoices = invoices.slice(0, MAX_VISIBLE_ITEMS);
  const visibleRecurring = recurring.slice(0, MAX_VISIBLE_ITEMS - visibleInvoices.length);
  const hiddenInvoicesCount = invoices.length - visibleInvoices.length;
  const hiddenRecurringCount = recurring.length - visibleRecurring.length;

  return (
    <Card>
      <CardHeader>
        <CardTitle className="text-sm font-medium">Próximos Vencimentos</CardTitle>
        <p className="text-xs text-muted-foreground">Faturas a pagar e recorrências dos próximos 15 dias</p>
      </CardHeader>
      <CardContent className="space-y-5">
        {isEmpty && (
          <p className="text-sm text-muted-foreground text-center py-8">Nenhum vencimento próximo.</p>
        )}

        {visibleInvoices.length > 0 && (
          <div className="space-y-1">
            <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Faturas</p>
            <ul className="divide-y">
              {visibleInvoices.map((invoice) => (
                <li key={invoice.invoiceId}>
                  <Link
                    href={`/credit-cards/${invoice.creditCardId}/invoices/${invoice.invoiceId}`}
                    className="flex items-center gap-3 py-2.5 hover:opacity-80"
                  >
                    <span className="h-8 w-1 rounded-full shrink-0" style={{ backgroundColor: invoice.color }} />
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-medium">
                        {invoice.creditCardName}
                        <span className="ml-1 text-xs font-normal text-muted-foreground">
                          · {formatReportMonth(invoice.referenceMonth)}
                        </span>
                      </p>
                      <p className={cn("text-xs", invoice.isOverdue ? "text-expense" : "text-muted-foreground")}>
                        {invoice.isOverdue ? "Vencida em " : "Vence em "}
                        {formatReportDate(invoice.dueDate)} · {invoiceStatusLabels[invoice.status]}
                      </p>
                    </div>
                    <span className="text-sm font-semibold whitespace-nowrap">
                      {formatMonetaryValue(invoice.amount, invoice.currency)}
                    </span>
                  </Link>
                </li>
              ))}
            </ul>
          </div>
        )}

        {visibleRecurring.length > 0 && (
          <div className="space-y-1">
            <p className="text-xs font-medium uppercase tracking-wide text-muted-foreground">Recorrências</p>
            <ul className="divide-y">
              {visibleRecurring.map((item) => {
                const isIncome = item.type === "Income";
                return (
                  <li key={item.id} className="flex items-center gap-3 py-2.5">
                    <span className="w-10 shrink-0 text-xs text-muted-foreground">{formatReportDate(item.date)}</span>
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-medium">{item.description}</p>
                      <p className="flex items-center gap-1.5 text-xs text-muted-foreground">
                        <span
                          className="inline-block h-2 w-2 rounded-full shrink-0"
                          style={{ backgroundColor: item.categoryColor }}
                        />
                        <span className="truncate">
                          {item.categoryName} · {item.accountName}
                        </span>
                      </p>
                    </div>
                    <span
                      className={cn(
                        "text-sm font-semibold whitespace-nowrap",
                        isIncome ? "text-income" : item.type === "Expense" ? "text-expense" : "text-muted-foreground"
                      )}
                    >
                      {isIncome ? "+" : item.type === "Expense" ? "-" : ""}
                      {formatMonetaryValue(item.amount, currency)}
                    </span>
                  </li>
                );
              })}
            </ul>
          </div>
        )}

        {(hiddenInvoicesCount > 0 || hiddenRecurringCount > 0) && (
          <p className="flex flex-wrap gap-x-3 gap-y-1 text-xs text-muted-foreground">
            <span>Há mais vencimentos além destes {MAX_VISIBLE_ITEMS}.</span>
            {hiddenInvoicesCount > 0 && (
              <Link href="/credit-cards" className="text-primary hover:underline">
                Ver todas as faturas →
              </Link>
            )}
            {hiddenRecurringCount > 0 && (
              <Link href="/recurring" className="text-primary hover:underline">
                Ver todas as recorrências →
              </Link>
            )}
          </p>
        )}
      </CardContent>
    </Card>
  );
}
