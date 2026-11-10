"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { FileUp, Inbox, XCircle } from "lucide-react";

import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState } from "@/components/shared/empty-state";
import { TableSkeleton } from "@/components/shared/loading-skeleton";
import { InvoiceImportStatusBadge } from "@/components/invoice-imports/invoice-import-status-badge";
import { useCreditCards } from "@/hooks/use-credit-cards";
import { useRecentInvoiceImports } from "@/hooks/use-invoice-imports";
import {
  describeInvoiceImportFiles,
  formatDateTime,
  formatReferenceMonth,
  invoiceImportHref,
  newInvoiceImportHref,
} from "@/lib/invoice-import";
import type { CreditCardInvoiceImportResponseDto } from "@/types/invoice-import";

/** Ação principal de cada importação, conforme o estado. */
function primaryAction(job: CreditCardInvoiceImportResponseDto): { label: string; href: string } {
  switch (job.status) {
    case "AwaitingReview":
      return { label: "Revisar", href: invoiceImportHref(job.id) };
    case "Confirmed":
      return { label: "Ver resultado", href: invoiceImportHref(job.id) };
    case "Failed":
      return { label: "Enviar novamente", href: newInvoiceImportHref(job.creditCardId) };
    case "Discarded":
      return { label: "Ver detalhes", href: invoiceImportHref(job.id) };
    default:
      return { label: "Acompanhar", href: invoiceImportHref(job.id) };
  }
}

export default function InvoiceImportsPage() {
  const router = useRouter();
  const { data: imports, isLoading, refetch, isFetching } = useRecentInvoiceImports();
  const { data: cards } = useCreditCards();

  const header = (
    <PageHeader
      title="Importações de fatura"
      description="As 20 importações mais recentes. Acompanhe a leitura, revise e veja o resultado."
    >
      <Button size="sm" onClick={() => router.push(newInvoiceImportHref())}>
        <FileUp className="mr-2 h-4 w-4" />
        Nova importação
      </Button>
    </PageHeader>
  );

  if (isLoading) {
    return (
      <div className="space-y-6">
        {header}
        <TableSkeleton rows={4} />
      </div>
    );
  }

  if (!imports) {
    return (
      <div className="space-y-6">
        {header}
        <EmptyState
          icon={XCircle}
          title="Não foi possível carregar as importações"
          description="Verifique sua conexão e tente de novo."
          actionLabel={isFetching ? "Carregando…" : "Tentar de novo"}
          onAction={() => void refetch()}
        />
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {header}

      {imports.length === 0 ? (
        <EmptyState
          icon={Inbox}
          title="Nenhuma importação ainda"
          description="Envie a fatura fechada de um cartão em PDF, fotos ou planilha .xlsx. A IA lê os lançamentos e você revisa antes de cadastrar."
          actionLabel="Importar fatura"
          onAction={() => router.push(newInvoiceImportHref())}
        />
      ) : (
        <ul className="divide-y rounded-xl border bg-card">
          {imports.map((job) => {
            const card = cards?.find((c) => c.id === job.creditCardId);
            const action = primaryAction(job);
            const referenceMonth = job.invoice?.referenceMonth ?? job.referenceMonth;
            const created = job.status === "Confirmed" ? (job.result?.createdCount ?? 0) : null;

            return (
              <li
                key={job.id}
                className="flex flex-col gap-3 p-3 sm:flex-row sm:items-center sm:justify-between sm:p-4"
              >
                <Link href={invoiceImportHref(job.id)} className="min-w-0 flex-1 space-y-1 hover:opacity-80">
                  <span className="flex min-w-0 items-center gap-2">
                    <span
                      className="h-3 w-3 shrink-0 rounded-full"
                      style={{ backgroundColor: card?.color ?? "var(--muted-foreground)" }}
                      aria-hidden
                    />
                    <span className="truncate font-medium">{card?.name ?? "Cartão removido"}</span>
                    <InvoiceImportStatusBadge status={job.status} />
                  </span>
                  <span className="block text-xs text-muted-foreground">
                    Enviada em {formatDateTime(job.createdAt)} ·{" "}
                    {describeInvoiceImportFiles(job.sourceType, job.fileCount)}
                    {referenceMonth && <> · Fatura de {formatReferenceMonth(referenceMonth)}</>}
                    {created !== null && (
                      <> · {created === 1 ? "1 lançamento criado" : `${created} lançamentos criados`}</>
                    )}
                  </span>
                </Link>
                <Button
                  variant={job.status === "AwaitingReview" ? "default" : "outline"}
                  size="sm"
                  className="w-full shrink-0 sm:w-auto"
                  onClick={() => router.push(action.href)}
                >
                  {action.label}
                </Button>
              </li>
            );
          })}
        </ul>
      )}
    </div>
  );
}
