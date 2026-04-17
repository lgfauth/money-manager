"use client";

import { useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { ArrowLeft, FileText, Trash2 } from "lucide-react";

import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState } from "@/components/shared/empty-state";
import { TableSkeleton } from "@/components/shared/loading-skeleton";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { InvoiceStatusBadge } from "@/components/credit-cards/invoice-status-badge";
import { InvoicePaymentModal } from "@/components/credit-cards/invoice-payment-modal";
import {
  useCreditCardInvoice,
  useDeleteCreditCardTransaction,
} from "@/hooks/use-credit-cards";
import type { CreditCardTransactionResponseDto } from "@/types/credit-card";

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

export default function InvoiceDetailPage() {
  const params = useParams<{ cardId: string; invoiceId: string }>();
  const router = useRouter();
  const cardId = params?.cardId;
  const invoiceId = params?.invoiceId;

  const { data, isLoading } = useCreditCardInvoice(cardId, invoiceId);
  const deleteTx = useDeleteCreditCardTransaction();

  const [paymentOpen, setPaymentOpen] = useState(false);
  const [deletingTx, setDeletingTx] =
    useState<CreditCardTransactionResponseDto | null>(null);

  if (isLoading) {
    return <TableSkeleton rows={5} />;
  }

  if (!data) {
    return (
      <EmptyState
        icon={FileText}
        title="Fatura não encontrada"
        description="A fatura solicitada não existe ou foi removida."
        actionLabel="Voltar para Cartões"
        onAction={() => router.push("/credit-cards")}
      />
    );
  }

  const { invoice, transactions } = data;
  const canPay = invoice.status === "closed" || invoice.status === "overdue";
  const canRemoveTx = invoice.status === "open" || invoice.status === "pending";

  const handleDeleteTx = () => {
    if (!deletingTx) return;
    deleteTx.mutate(deletingTx.id, {
      onSuccess: () => setDeletingTx(null),
    });
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title={`Fatura · ${fmtRefMonth(invoice.referenceMonth)}`}
        description={`Cartão ${invoice.creditCardName}`}
      >
        <Button
          variant="outline"
          size="sm"
          onClick={() => router.push(`/credit-cards/${cardId}`)}
        >
          <ArrowLeft className="mr-2 h-4 w-4" />
          Voltar
        </Button>
        {canPay && (
          <Button onClick={() => setPaymentOpen(true)}>Registrar pagamento</Button>
        )}
      </PageHeader>

      <div className="grid gap-3 sm:grid-cols-4">
        <div className="rounded-xl border bg-card p-4">
          <p className="text-xs text-muted-foreground">Status</p>
          <div className="mt-1">
            <InvoiceStatusBadge status={invoice.status} />
          </div>
        </div>
        <div className="rounded-xl border bg-card p-4">
          <p className="text-xs text-muted-foreground">Total</p>
          <p className="text-lg font-semibold">
            {fmt(invoice.totalAmount, invoice.currency)}
          </p>
        </div>
        <div className="rounded-xl border bg-card p-4">
          <p className="text-xs text-muted-foreground">Fechamento</p>
          <p className="text-lg font-semibold">{fmtDate(invoice.closingDate)}</p>
        </div>
        <div className="rounded-xl border bg-card p-4">
          <p className="text-xs text-muted-foreground">Vencimento</p>
          <p className="text-lg font-semibold">{fmtDate(invoice.dueDate)}</p>
        </div>
      </div>

      {invoice.status === "paid" && invoice.paidAt && (
        <div className="rounded-lg border border-income/30 bg-income/10 p-3 text-sm text-income">
          Fatura paga em {fmtDate(invoice.paidAt)}
          {invoice.paidAmount !== null &&
            ` — ${fmt(invoice.paidAmount, invoice.currency)}`}
          .
        </div>
      )}

      <section className="space-y-3">
        <h2 className="text-sm font-semibold text-muted-foreground">
          Transações ({transactions.length})
        </h2>
        {transactions.length === 0 ? (
          <p className="rounded-lg border border-dashed bg-card px-4 py-6 text-center text-sm text-muted-foreground">
            Nenhuma transação nesta fatura.
          </p>
        ) : (
          <ul className="space-y-2">
            {transactions.map((tx) => (
              <li
                key={tx.id}
                className="flex items-center justify-between gap-3 rounded-lg border bg-card px-4 py-3"
              >
                <div className="min-w-0 flex-1 space-y-1">
                  <div className="flex items-center gap-2">
                    <span
                      className="h-2 w-2 shrink-0 rounded-full"
                      style={{ backgroundColor: tx.categoryColor }}
                    />
                    <p className="truncate text-sm font-medium">
                      {tx.description}
                    </p>
                    {tx.totalInstallments > 1 && (
                      <span className="rounded bg-muted px-1.5 py-0.5 text-[10px] font-medium text-muted-foreground">
                        {tx.installmentNumber}/{tx.totalInstallments}
                      </span>
                    )}
                  </div>
                  <p className="text-[11px] text-muted-foreground">
                    {tx.categoryName} · {fmtDate(tx.purchaseDate)}
                  </p>
                </div>
                <div className="flex items-center gap-2">
                  <p className="text-sm font-semibold">
                    {fmt(tx.installmentAmount, tx.currency)}
                  </p>
                  {canRemoveTx && (
                    <button
                      type="button"
                      onClick={() => setDeletingTx(tx)}
                      className="text-muted-foreground hover:text-destructive"
                      aria-label="Excluir transação"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>

      <InvoicePaymentModal
        open={paymentOpen}
        onOpenChange={setPaymentOpen}
        cardId={invoice.creditCardId}
        invoice={invoice}
      />

      <ConfirmDialog
        open={!!deletingTx}
        onOpenChange={(open) => {
          if (!open) setDeletingTx(null);
        }}
        title="Excluir transação"
        description={
          deletingTx?.totalInstallments && deletingTx.totalInstallments > 1
            ? `Tem certeza? Todas as ${deletingTx.totalInstallments} parcelas desta compra serão removidas.`
            : `Tem certeza que deseja excluir "${deletingTx?.description}"?`
        }
        onConfirm={handleDeleteTx}
        confirmLabel="Excluir"
        variant="destructive"
      />
    </div>
  );
}
