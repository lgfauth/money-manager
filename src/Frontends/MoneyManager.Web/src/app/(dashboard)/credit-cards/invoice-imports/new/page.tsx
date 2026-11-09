"use client";

import { Suspense, useState } from "react";
import Link from "next/link";
import { useRouter, useSearchParams } from "next/navigation";
import { AlertCircle, ArrowLeft, CreditCard, Info, Loader2, Upload, XCircle } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Progress } from "@/components/ui/progress";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState } from "@/components/shared/empty-state";
import { TableSkeleton } from "@/components/shared/loading-skeleton";
import { InvoiceImportCardPicker } from "@/components/invoice-imports/invoice-import-card-picker";
import { InvoiceImportFilePicker } from "@/components/invoice-imports/invoice-import-file-picker";
import {
  useCreateInvoiceImport,
  useInvoiceImportEligibleCards,
  useRecentInvoiceImports,
} from "@/hooks/use-invoice-imports";
import { ApiClientError } from "@/lib/api-errors";
import {
  invoiceImportHref,
  readInvoiceImportApiError,
  validateInvoiceImportFiles,
} from "@/lib/invoice-import";
import { useBreadcrumbLabel } from "@/stores/breadcrumb-store";

interface SubmitError {
  statusCode: number | null;
  messages: string[];
}

export default function NewInvoiceImportPage() {
  // useSearchParams exige um limite de Suspense para a rota poder ser pré-renderizada
  return (
    <Suspense fallback={<TableSkeleton rows={4} />}>
      <NewInvoiceImportContent />
    </Suspense>
  );
}

function NewInvoiceImportContent() {
  const router = useRouter();
  const requestedCardId = useSearchParams().get("cardId");

  const { data: cards, isLoading, refetch, isFetching } = useInvoiceImportEligibleCards();
  const { data: recentImports } = useRecentInvoiceImports();
  const upload = useCreateInvoiceImport();

  const [chosenCardId, setChosenCardId] = useState<string | null>(null);
  const [files, setFiles] = useState<File[]>([]);
  const [submitError, setSubmitError] = useState<SubmitError | null>(null);
  const [showValidation, setShowValidation] = useState(false);

  useBreadcrumbLabel("new", "Nova importação");

  if (isLoading) return <TableSkeleton rows={4} />;

  if (!cards) {
    return (
      <EmptyState
        icon={XCircle}
        title="Não foi possível carregar seus cartões"
        description="Verifique sua conexão e tente de novo."
        actionLabel={isFetching ? "Carregando…" : "Tentar de novo"}
        onAction={() => void refetch()}
      />
    );
  }

  if (cards.length === 0) {
    return (
      <EmptyState
        icon={CreditCard}
        title="Nenhum cartão disponível para importação"
        description="Cadastre um cartão de crédito em Bancos e Contas para importar a fatura dele. Cartões sincronizados pelo Open Finance não aparecem aqui: as compras deles já chegam automaticamente."
        actionLabel="Ir para Bancos e Contas"
        onAction={() => router.push("/bancos-e-contas")}
      />
    );
  }

  const requestedIsEligible = !!requestedCardId && cards.some((c) => c.id === requestedCardId);
  // Cartão pedido na URL, ou o único elegível; depois, o escolhido pelo usuário
  const cardId =
    chosenCardId ??
    (requestedIsEligible ? requestedCardId : cards.length === 1 ? cards[0].id : null);
  const card = cards.find((c) => c.id === cardId);

  const fileError = files.length > 0 || showValidation ? validateInvoiceImportFiles(files) : null;
  const cardError = showValidation && !cardId ? "Escolha o cartão da fatura." : null;
  const uploading = upload.isPending;

  // 409 por importação em andamento no cartão: leva o usuário até ela
  const activeImport =
    submitError?.statusCode === 409
      ? recentImports?.find(
          (i) => i.creditCardId === cardId && (i.status === "Queued" || i.status === "Processing")
        )
      : undefined;

  const handleCardChange = (id: string) => {
    setChosenCardId(id);
    setSubmitError(null);
  };

  const handleFilesChange = (next: File[]) => {
    setFiles(next);
    setSubmitError(null);
  };

  const handleSubmit = () => {
    setShowValidation(true);
    if (!cardId || validateInvoiceImportFiles(files) || uploading) return;

    setSubmitError(null);
    upload.mutate(
      { creditCardId: cardId, files },
      {
        onSuccess: (created) => router.push(invoiceImportHref(created.id)),
        onError: (err) => {
          const { message, errors } = readInvoiceImportApiError(
            err,
            "Não foi possível enviar a fatura. Tente novamente em instantes."
          );
          setSubmitError({
            statusCode: err instanceof ApiClientError ? err.statusCode : null,
            // Validação (400): o resumo genérico só aparece se não houver a lista de problemas
            messages: errors.length > 0 ? [...new Set(errors)] : [message],
          });
        },
      }
    );
  };

  const backHref = card ? `/credit-cards/${card.id}` : "/bancos-e-contas";

  return (
    <div className="space-y-6">
      <PageHeader
        title="Importar fatura"
        description="Envie a fatura fechada do cartão e revise os lançamentos antes de cadastrar."
      >
        <Button variant="outline" size="sm" onClick={() => router.push(backHref)} disabled={uploading}>
          <ArrowLeft className="mr-2 h-4 w-4" />
          Voltar
        </Button>
      </PageHeader>

      <div className="flex gap-3 rounded-xl border bg-muted/40 p-4 text-sm">
        <Info className="mt-0.5 h-4 w-4 shrink-0 text-primary" aria-hidden />
        <ul className="list-disc space-y-1 pl-4 text-muted-foreground">
          <li>Use a fatura <strong className="text-foreground">fechada</strong>, em PDF, fotos ou prints, ou na planilha .xlsx do banco.</li>
          <li>A leitura é feita por IA e traz os lançamentos de até 60 dias atrás.</li>
          <li>Nada é cadastrado sem a sua revisão: você confere, corrige e escolhe o que entra.</li>
          <li>A leitura leva alguns minutos e você pode continuar usando o MoneyManager enquanto isso.</li>
        </ul>
      </div>

      <section className="space-y-3" aria-labelledby="invoice-import-card">
        <h2 id="invoice-import-card" className="text-sm font-semibold">
          1. Cartão da fatura
        </h2>
        {requestedCardId && !requestedIsEligible && !chosenCardId && (
          <p className="text-sm text-muted-foreground">
            O cartão indicado não está disponível para importação: ele foi removido ou é
            sincronizado pelo Open Finance. Escolha outro cartão.
          </p>
        )}
        <InvoiceImportCardPicker
          cards={cards}
          value={cardId}
          onChange={handleCardChange}
          disabled={uploading}
        />
        {cardError && <FieldError message={cardError} />}
      </section>

      <section className="space-y-3" aria-labelledby="invoice-import-files">
        <h2 id="invoice-import-files" className="text-sm font-semibold">
          2. Arquivos da fatura
        </h2>
        <InvoiceImportFilePicker files={files} onChange={handleFilesChange} disabled={uploading} />
        {fileError && <FieldError message={fileError} />}
      </section>

      {submitError && (
        <div
          role="alert"
          className="space-y-2 rounded-xl border border-expense/30 bg-expense/10 p-4 text-sm"
        >
          <p className="font-medium text-expense">Não foi possível enviar a fatura.</p>
          <ul className="space-y-1">
            {submitError.messages.map((message) => (
              <li key={message}>{message}</li>
            ))}
          </ul>
          {activeImport && (
            <Link
              href={invoiceImportHref(activeImport.id)}
              className="inline-block font-medium text-primary underline-offset-4 hover:underline"
            >
              Acompanhar a importação em andamento
            </Link>
          )}
          <p className="text-muted-foreground">Os arquivos continuam selecionados.</p>
        </div>
      )}

      <div className="space-y-3">
        {uploading && (
          <div className="space-y-1.5" role="status">
            <Progress value={upload.progress} aria-label="Progresso do envio" />
            <p className="text-xs text-muted-foreground">
              {upload.progress < 100
                ? `Enviando os arquivos… ${upload.progress}%`
                : "Concluindo o envio…"}
            </p>
          </div>
        )}
        <Button className="w-full sm:w-auto" onClick={handleSubmit} disabled={uploading}>
          {uploading ? (
            <Loader2 className="mr-2 h-4 w-4 animate-spin" />
          ) : (
            <Upload className="mr-2 h-4 w-4" />
          )}
          {uploading ? "Enviando…" : "Enviar fatura"}
        </Button>
      </div>
    </div>
  );
}

function FieldError({ message }: { message: string }) {
  return (
    <p className="flex items-start gap-2 text-sm text-expense" role="alert">
      <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
      {message}
    </p>
  );
}
