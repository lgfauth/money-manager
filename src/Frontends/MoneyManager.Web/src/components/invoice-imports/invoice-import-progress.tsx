"use client";

import { useEffect, useState } from "react";
import { Check, Clock, Loader2 } from "lucide-react";
import { cn } from "@/lib/utils";
import {
  describeInvoiceImportFiles,
  formatDateTime,
  formatElapsed,
  INVOICE_IMPORT_LONG_QUEUE_MS,
} from "@/lib/invoice-import";
import type { CreditCardInvoiceImportResponseDto } from "@/types/invoice-import";

type StepState = "done" | "current" | "waiting";

interface Step {
  label: string;
  description: string;
  state: StepState;
}

function buildSteps(status: CreditCardInvoiceImportResponseDto["status"]): Step[] {
  const reading = status === "Processing";
  return [
    {
      label: "Fatura enviada",
      description: "Recebemos os arquivos.",
      state: "done",
    },
    {
      label: "Lendo a fatura",
      description: reading
        ? "A IA está extraindo os lançamentos. Isso pode levar alguns minutos."
        : "Aguardando a vez na fila de leitura.",
      state: "current",
    },
    {
      label: "Pronta para revisão",
      description: "Você confere os lançamentos antes de qualquer cadastro.",
      state: "waiting",
    },
  ];
}

/** Acompanhamento de uma importação na fila ou em leitura pela IA. */
export function InvoiceImportProgress({ job }: { job: CreditCardInvoiceImportResponseDto }) {
  // Relógio da tela para o "há X min" e para a orientação de fila longa
  const [now, setNow] = useState(() => Date.now());
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 30_000);
    return () => window.clearInterval(timer);
  }, []);

  const steps = buildSteps(job.status);
  const waitingTooLong =
    job.status === "Queued" &&
    now - new Date(job.createdAt).getTime() > INVOICE_IMPORT_LONG_QUEUE_MS;

  return (
    <div className="space-y-6 rounded-xl border bg-card p-4 sm:p-6">
      <ol className="space-y-4" aria-label="Etapas da importação">
        {steps.map((step) => (
          <li key={step.label} className="flex gap-3">
            <span
              className={cn(
                "flex h-8 w-8 shrink-0 items-center justify-center rounded-full border",
                step.state === "done" && "border-income/30 bg-income/15 text-income",
                step.state === "current" && "border-primary/30 bg-primary/15 text-primary",
                step.state === "waiting" && "bg-muted text-muted-foreground"
              )}
              aria-hidden
            >
              {step.state === "done" && <Check className="h-4 w-4" />}
              {step.state === "current" && <Loader2 className="h-4 w-4 animate-spin" />}
              {step.state === "waiting" && <Clock className="h-4 w-4" />}
            </span>
            <div className="min-w-0">
              <p
                className={cn(
                  "text-sm font-medium",
                  step.state === "waiting" && "text-muted-foreground"
                )}
              >
                {step.label}
              </p>
              <p className="text-sm text-muted-foreground">{step.description}</p>
            </div>
          </li>
        ))}
      </ol>

      <div className="rounded-lg bg-muted/60 p-3 text-sm">
        <p className="font-medium">Você pode continuar usando o MoneyManager.</p>
        <p className="mt-1 text-muted-foreground">
          A leitura continua mesmo se você sair desta tela. Volte por este mesmo endereço
          quando quiser: a página se atualiza sozinha quando a fatura ficar pronta.
        </p>
      </div>

      {waitingTooLong && (
        <div
          role="status"
          className="rounded-lg border border-amber-500/30 bg-amber-500/10 p-3 text-sm text-amber-800 dark:text-amber-300"
        >
          A fatura está na fila há mais tempo que o normal. Ela será lida assim que possível
          e não é preciso enviá-la de novo — você pode sair desta tela e voltar depois.
        </div>
      )}

      <dl className="grid gap-3 text-sm sm:grid-cols-2">
        <div>
          <dt className="text-muted-foreground">Arquivos</dt>
          <dd className="font-medium">
            {describeInvoiceImportFiles(job.sourceType, job.fileCount)}
          </dd>
        </div>
        <div>
          <dt className="text-muted-foreground">Enviada em</dt>
          <dd className="font-medium">
            {formatDateTime(job.createdAt)}{" "}
            <span className="font-normal text-muted-foreground">
              ({formatElapsed(job.createdAt, now)})
            </span>
          </dd>
        </div>
      </dl>
    </div>
  );
}
