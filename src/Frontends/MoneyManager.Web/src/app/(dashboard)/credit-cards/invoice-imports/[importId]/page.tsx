"use client";

import { useEffect, useState } from "react";
import { useParams, useRouter } from "next/navigation";
import { ArrowLeft, FileUp, FileX, Loader2, RotateCcw, Trash2, XCircle } from "lucide-react";

import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState } from "@/components/shared/empty-state";
import { TableSkeleton } from "@/components/shared/loading-skeleton";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { InvoiceImportStatusBadge } from "@/components/invoice-imports/invoice-import-status-badge";
import { InvoiceImportProgress } from "@/components/invoice-imports/invoice-import-progress";
import { InvoiceImportSummary } from "@/components/invoice-imports/invoice-import-summary";
import { InvoiceImportReview } from "@/components/invoice-imports/invoice-import-review";
import { InvoiceImportResult } from "@/components/invoice-imports/invoice-import-result";
import { useCreditCard } from "@/hooks/use-credit-cards";
import { useDiscardInvoiceImport, useInvoiceImport } from "@/hooks/use-invoice-imports";
import { ApiClientError } from "@/lib/api-errors";
import { INVOICE_IMPORTS_HREF, formatDateTime, newInvoiceImportHref } from "@/lib/invoice-import";
import { useBreadcrumbLabel } from "@/stores/breadcrumb-store";
import { useInvoiceImportDraftStore } from "@/stores/invoice-import-draft-store";
import type { CreditCardResponseDto } from "@/types/credit-card";
import type { CreditCardInvoiceImportResponseDto } from "@/types/invoice-import";

export default function InvoiceImportPage() {
  const params = useParams<{ importId: string }>();
  const router = useRouter();
  const importId = params?.importId;

  const { data: job, isLoading, error, refetch, isFetching } = useInvoiceImport(importId);
  const { data: card } = useCreditCard(job?.creditCardId);
  const discard = useDiscardInvoiceImport();
  const [discardOpen, setDiscardOpen] = useState(false);

  useBreadcrumbLabel(importId, "Importação");

  // Rascunho da revisão só serve enquanto ela aguarda revisão (ex.: confirmada em outra aba)
  const clearDraft = useInvoiceImportDraftStore((s) => s.clearDraft);
  const notFound = error instanceof ApiClientError && error.statusCode === 404;
  const leftReview = job !== undefined && job.status !== "AwaitingReview";
  useEffect(() => {
    if (importId && (leftReview || (!job && notFound))) clearDraft(importId);
  }, [importId, leftReview, job, notFound, clearDraft]);

  if (isLoading) {
    return <TableSkeleton rows={4} />;
  }

  if (!job) {
    return notFound ? (
      <EmptyState
        icon={FileX}
        title="Importação não encontrada"
        description="Esta importação de fatura não existe ou não pertence à sua conta."
        actionLabel="Ver importações de fatura"
        onAction={() => router.push(INVOICE_IMPORTS_HREF)}
      />
    ) : (
      <EmptyState
        icon={XCircle}
        title="Não foi possível carregar a importação"
        description="Verifique sua conexão e tente de novo."
        actionLabel={isFetching ? "Carregando…" : "Tentar de novo"}
        onAction={() => void refetch()}
      />
    );
  }

  const canDiscard = job.status === "AwaitingReview" || job.status === "Failed";
  const backHref = card ? `/credit-cards/${card.id}` : "/bancos-e-contas";

  const handleDiscard = () => {
    discard.mutate(job.id, { onSettled: () => setDiscardOpen(false) });
  };

  return (
    <div className="space-y-6">
      <PageHeader
        title="Importação de fatura"
        description={card ? `Cartão ${card.name}` : undefined}
      >
        <Button variant="outline" size="sm" onClick={() => router.push(backHref)}>
          <ArrowLeft className="mr-2 h-4 w-4" />
          Voltar
        </Button>
        {canDiscard && (
          <Button
            variant="outline"
            size="sm"
            onClick={() => setDiscardOpen(true)}
            disabled={discard.isPending}
          >
            <Trash2 className="mr-2 h-4 w-4" />
            Descartar
          </Button>
        )}
        {job.status === "Failed" && (
          <Button size="sm" onClick={() => router.push(newInvoiceImportHref(job.creditCardId))}>
            <RotateCcw className="mr-2 h-4 w-4" />
            Enviar novamente
          </Button>
        )}
        {job.status === "Confirmed" && (
          <Button size="sm" onClick={() => router.push(newInvoiceImportHref(job.creditCardId))}>
            <FileUp className="mr-2 h-4 w-4" />
            Importar outra fatura
          </Button>
        )}
      </PageHeader>

      <div className="flex flex-wrap items-center gap-2 text-sm text-muted-foreground">
        <InvoiceImportStatusBadge status={job.status} />
        <span>Enviada em {formatDateTime(job.createdAt)}</span>
      </div>

      <InvoiceImportStatusContent job={job} card={card} />

      <ConfirmDialog
        open={discardOpen}
        onOpenChange={setDiscardOpen}
        title="Descartar importação?"
        description="Nenhum lançamento desta fatura será cadastrado. Se quiser importá-la depois, será preciso enviar os arquivos de novo."
        confirmLabel="Descartar"
        variant="destructive"
        onConfirm={handleDiscard}
      />
    </div>
  );
}

function InvoiceImportStatusContent({
  job,
  card,
}: {
  job: CreditCardInvoiceImportResponseDto;
  card: CreditCardResponseDto | undefined;
}) {
  switch (job.status) {
    case "Queued":
    case "Processing":
      return <InvoiceImportProgress job={job} />;

    case "Confirming":
      return (
        <div
          role="status"
          className="flex items-center gap-3 rounded-xl border bg-card p-4 sm:p-6"
        >
          <Loader2 className="h-5 w-5 shrink-0 animate-spin text-primary" aria-hidden />
          <div>
            <p className="font-medium">Cadastrando os lançamentos…</p>
            <p className="text-sm text-muted-foreground">
              Isso leva poucos segundos. A página se atualiza sozinha ao terminar.
            </p>
          </div>
        </div>
      );

    case "Failed":
      return (
        <div
          role="alert"
          className="space-y-2 rounded-xl border border-expense/30 bg-expense/10 p-4 sm:p-6"
        >
          <p className="font-medium text-expense">Não foi possível importar a fatura.</p>
          <p className="text-sm">
            {job.errorMessage ?? "Ocorreu um erro ao ler os arquivos enviados."}
          </p>
          <p className="text-sm text-muted-foreground">
            Nenhum lançamento foi cadastrado. Você pode enviar a fatura novamente ou descartar
            esta importação.
          </p>
        </div>
      );

    case "AwaitingReview":
      // A chave com a versão descarta as edições locais se o servidor alterar a importação
      return <InvoiceImportReview key={`${job.id}-${job.version}`} job={job} card={card} />;

    case "Confirmed":
      return (
        <div className="space-y-6">
          <InvoiceImportResult job={job} />
          <InvoiceImportSummary job={job} />
        </div>
      );

    case "Discarded":
      return (
        <div className="rounded-xl border bg-card p-4 text-sm text-muted-foreground sm:p-6">
          Esta importação foi descartada e nenhum lançamento foi cadastrado.
        </div>
      );

    default:
      return null;
  }
}
