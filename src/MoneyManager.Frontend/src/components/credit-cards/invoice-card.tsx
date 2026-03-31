"use client";

import Link from "next/link";
import { format } from "date-fns";
import { ptBR } from "date-fns/locale";
import { Card, CardContent } from "@/components/ui/card";
import { Button } from "@/components/ui/button";
import { InvoiceStatusBadge } from "./invoice-status-badge";
import type { CreditCardInvoiceResponseDto } from "@/types/invoice";
import { InvoiceStatus } from "@/types/invoice";

interface InvoiceCardProps {
  invoice: CreditCardInvoiceResponseDto;
  currency?: string;
  label: string;
  onPay?: () => void;
}

function fmt(value: number, currency = "BRL"): string {
  return value.toLocaleString("pt-BR", { style: "currency", currency });
}

export function InvoiceCard({ invoice, currency = "BRL", label, onPay }: InvoiceCardProps) {
  const isOverdue =
    invoice.status !== InvoiceStatus.Paid &&
    new Date(invoice.dueDate) < new Date();

  const canPay =
    invoice.status === InvoiceStatus.Closed ||
    invoice.status === InvoiceStatus.Overdue ||
    invoice.status === InvoiceStatus.PartiallyPaid;

  return (
    <Card className={isOverdue ? "border-expense/50" : undefined}>
      <CardContent className="p-4 space-y-3">
        <div className="flex items-center justify-between">
          <span className="text-sm font-medium text-muted-foreground">
            {label}
          </span>
          <InvoiceStatusBadge status={invoice.status} isOverdue={isOverdue} />
        </div>

        <div className="space-y-1 text-sm">
          <div className="flex justify-between">
            <span className="text-muted-foreground">Período</span>
            <span>
              {format(new Date(invoice.periodStart), "dd/MM", { locale: ptBR })} —{" "}
              {format(new Date(invoice.periodEnd), "dd/MM/yyyy", { locale: ptBR })}
            </span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Total</span>
            <span className="font-semibold">{fmt(invoice.totalAmount, currency)}</span>
          </div>
          {invoice.paidAmount > 0 && (
            <div className="flex justify-between">
              <span className="text-muted-foreground">Pago</span>
              <span className="text-income">{fmt(invoice.paidAmount, currency)}</span>
            </div>
          )}
          {invoice.remainingAmount > 0 && invoice.remainingAmount !== invoice.totalAmount && (
            <div className="flex justify-between">
              <span className="text-muted-foreground">Restante</span>
              <span className="text-expense">{fmt(invoice.remainingAmount, currency)}</span>
            </div>
          )}
          <div className="flex justify-between">
            <span className="text-muted-foreground">Vencimento</span>
            <span>{format(new Date(invoice.dueDate), "dd/MM/yyyy", { locale: ptBR })}</span>
          </div>
          {invoice.daysUntilDue > 0 && !isOverdue && (
            <p className="text-xs text-muted-foreground">
              {invoice.daysUntilDue} dias até o vencimento
            </p>
          )}
        </div>

        <div className="flex gap-2 pt-1">
          <Link
            href={`/invoices/${invoice.id}`}
            className="text-xs text-primary hover:underline"
          >
            Ver detalhes
          </Link>
          {canPay && onPay && (
            <Button size="sm" variant="outline" className="ml-auto h-7 text-xs" onClick={onPay}>
              Pagar Fatura
            </Button>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
