"use client";

import { useMemo } from "react";
import { useParams, useRouter } from "next/navigation";
import { ArrowLeft, FileText } from "lucide-react";

import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState } from "@/components/shared/empty-state";
import {
  CardGridSkeleton,
  TableSkeleton,
} from "@/components/shared/loading-skeleton";
import { InvoiceListItem } from "@/components/credit-cards/invoice-list-item";
import {
  useCreditCard,
  useCreditCardInvoices,
} from "@/hooks/use-credit-cards";
import {
  CREDIT_LIMIT_THRESHOLD_DANGER,
  CREDIT_LIMIT_THRESHOLD_WARNING,
} from "@/config/constants";
import { cn } from "@/lib/utils";

const fmt = (value: number, currency: string) =>
  new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency,
  }).format(value);

export default function CreditCardDetailPage() {
  const params = useParams<{ cardId: string }>();
  const router = useRouter();
  const cardId = params?.cardId;

  const { data: card, isLoading: cardLoading } = useCreditCard(cardId);
  const { data: invoices, isLoading: invoicesLoading } =
    useCreditCardInvoices(cardId);

  const sortedInvoices = useMemo(() => {
    if (!invoices) return [];
    return [...invoices].sort((a, b) =>
      b.referenceMonth.localeCompare(a.referenceMonth)
    );
  }, [invoices]);

  const currentInvoice = useMemo(
    () => sortedInvoices.find((inv) => inv.status === "open"),
    [sortedInvoices]
  );

  const otherInvoices = useMemo(
    () => sortedInvoices.filter((inv) => inv.id !== currentInvoice?.id),
    [sortedInvoices, currentInvoice]
  );

  if (cardLoading) {
    return (
      <div className="space-y-6">
        <CardGridSkeleton />
        <TableSkeleton rows={3} />
      </div>
    );
  }

  if (!card) {
    return (
      <EmptyState
        icon={FileText}
        title="Cartão não encontrado"
        description="O cartão que você está tentando acessar não existe ou foi removido."
        actionLabel="Voltar para Cartões"
        onAction={() => router.push("/credit-cards")}
      />
    );
  }

  const used = card.currentBalance;
  const usedPercent = card.limit > 0 ? Math.min(100, (used / card.limit) * 100) : 0;
  let usageColor = "bg-income";
  if (usedPercent >= CREDIT_LIMIT_THRESHOLD_DANGER) usageColor = "bg-expense";
  else if (usedPercent >= CREDIT_LIMIT_THRESHOLD_WARNING)
    usageColor = "bg-amber-500";

  return (
    <div className="space-y-6">
      <PageHeader title={card.name} description="Faturas e detalhes do cartão.">
        <Button
          variant="outline"
          size="sm"
          onClick={() => router.push("/credit-cards")}
        >
          <ArrowLeft className="mr-2 h-4 w-4" />
          Voltar
        </Button>
      </PageHeader>

      <div className="grid gap-4 sm:grid-cols-3">
        <div className="rounded-xl border bg-card p-4">
          <p className="text-xs text-muted-foreground">Limite total</p>
          <p className="text-lg font-semibold">{fmt(card.limit, card.currency)}</p>
        </div>
        <div className="rounded-xl border bg-card p-4">
          <p className="text-xs text-muted-foreground">Utilizado</p>
          <p className="text-lg font-semibold text-expense">
            {fmt(used, card.currency)}
          </p>
          <div className="mt-2 h-1.5 w-full overflow-hidden rounded-full bg-muted">
            <div
              className={cn("h-full transition-all", usageColor)}
              style={{ width: `${usedPercent}%` }}
            />
          </div>
        </div>
        <div className="rounded-xl border bg-card p-4">
          <p className="text-xs text-muted-foreground">Disponível</p>
          <p className="text-lg font-semibold text-income">
            {fmt(card.availableLimit, card.currency)}
          </p>
        </div>
      </div>

      <section className="space-y-3">
        <h2 className="text-sm font-semibold text-muted-foreground">
          Fatura corrente
        </h2>
        {invoicesLoading ? (
          <TableSkeleton rows={3} />
        ) : currentInvoice ? (
          <InvoiceListItem
            invoice={currentInvoice}
            cardId={card.id}
            highlight
          />
        ) : (
          <p className="rounded-lg border border-dashed bg-card px-4 py-6 text-center text-sm text-muted-foreground">
            Nenhuma fatura em aberto no momento.
          </p>
        )}
      </section>

      <section className="space-y-3">
        <h2 className="text-sm font-semibold text-muted-foreground">
          Histórico de faturas
        </h2>
        {invoicesLoading ? (
          <TableSkeleton rows={3} />
        ) : otherInvoices.length === 0 ? (
          <p className="rounded-lg border border-dashed bg-card px-4 py-6 text-center text-sm text-muted-foreground">
            Sem faturas anteriores ou futuras.
          </p>
        ) : (
          <ul className="space-y-2">
            {otherInvoices.map((invoice) => (
              <li key={invoice.id}>
                <InvoiceListItem invoice={invoice} cardId={card.id} />
              </li>
            ))}
          </ul>
        )}
      </section>
    </div>
  );
}
