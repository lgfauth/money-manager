"use client";

import Link from "next/link";
import { ChevronRight } from "lucide-react";
import { InvoiceStatusBadge } from "@/components/credit-cards/invoice-status-badge";
import type { CreditCardInvoiceResponseDto } from "@/types/credit-card";

interface InvoiceListItemProps {
  invoice: CreditCardInvoiceResponseDto;
  cardId: string;
  highlight?: boolean;
}

const fmt = (value: number, currency: string) =>
  new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency,
  }).format(value);

const fmtDate = (iso: string) => {
  const d = new Date(iso);
  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(d);
};

const fmtRefMonth = (ref: string) => {
  const [year, month] = ref.split("-").map(Number);
  if (!year || !month) return ref;
  const date = new Date(year, month - 1, 1);
  return new Intl.DateTimeFormat("pt-BR", {
    month: "long",
    year: "numeric",
  }).format(date);
};

export function InvoiceListItem({
  invoice,
  cardId,
  highlight = false,
}: InvoiceListItemProps) {
  const isPending = invoice.status === "pending";
  const className = `flex items-center justify-between rounded-lg border bg-card px-4 py-3 transition-colors ${
    isPending ? "cursor-default opacity-80" : "hover:bg-muted/40"
  } ${highlight ? "border-primary/40 ring-1 ring-primary/20" : ""}`;

  const body = (
    <>
      <div className="space-y-1">
        <div className="flex items-center gap-2">
          <p className="text-sm font-semibold capitalize">
            {fmtRefMonth(invoice.referenceMonth)}
          </p>
          <InvoiceStatusBadge status={invoice.status} />
        </div>
        <p className="text-[11px] text-muted-foreground">
          Fechamento {fmtDate(invoice.closingDate)} · Vencimento{" "}
          {fmtDate(invoice.dueDate)}
        </p>
      </div>
      <div className="flex items-center gap-3">
        <div className="text-right">
          <p className="text-sm font-semibold">
            {fmt(invoice.totalAmount, invoice.currency)}
          </p>
          {invoice.status === "paid" && invoice.paidAt && (
            <p className="text-[11px] text-muted-foreground">
              Pago em {fmtDate(invoice.paidAt)}
            </p>
          )}
        </div>
        {!isPending && (
          <ChevronRight className="h-4 w-4 text-muted-foreground" />
        )}
      </div>
    </>
  );

  if (isPending) {
    return <div className={className}>{body}</div>;
  }

  return (
    <Link
      href={`/credit-cards/${cardId}/invoices/${invoice.id}`}
      className={className}
    >
      {body}
    </Link>
  );
}
