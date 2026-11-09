"use client";

import Link from "next/link";
import { CheckCircle2, ChevronRight } from "lucide-react";
import { useCreditCardInvoices } from "@/hooks/use-credit-cards";
import { formatDateTime, formatReferenceMonth } from "@/lib/invoice-import";
import type { CreditCardInvoiceImportResponseDto } from "@/types/invoice-import";

/** Resultado de uma importação confirmada: quantos lançamentos entraram e em quais faturas. */
export function InvoiceImportResult({ job }: { job: CreditCardInvoiceImportResponseDto }) {
  const { data: invoices } = useCreditCardInvoices(job.creditCardId);
  const result = job.result;
  const created = result?.createdCount ?? 0;
  const skipped = result?.skippedCount ?? 0;

  // Mais antigas primeiro (parcelamentos seguem para as faturas seguintes)
  const affected = (result?.affectedInvoiceIds ?? [])
    .map((id) => ({ id, invoice: invoices?.find((i) => i.id === id) }))
    .sort((a, b) =>
      (a.invoice?.referenceMonth ?? "").localeCompare(b.invoice?.referenceMonth ?? "")
    );

  return (
    <div className="space-y-4 rounded-xl border bg-card p-4 sm:p-6">
      <div className="flex gap-3">
        <CheckCircle2 className="h-6 w-6 shrink-0 text-income" aria-hidden />
        <div>
          <p className="font-medium">
            {created === 1
              ? "1 lançamento cadastrado no cartão."
              : `${created} lançamentos cadastrados no cartão.`}
          </p>
          {skipped > 0 && (
            <p className="text-sm text-muted-foreground">
              {skipped === 1
                ? "1 lançamento já tinha sido importado antes e foi ignorado."
                : `${skipped} lançamentos já tinham sido importados antes e foram ignorados.`}
            </p>
          )}
          {job.confirmedAt && (
            <p className="text-sm text-muted-foreground">
              Importação confirmada em {formatDateTime(job.confirmedAt)}.
            </p>
          )}
        </div>
      </div>

      {affected.length > 0 && (
        <div>
          <p className="mb-2 text-sm font-medium">Faturas atualizadas</p>
          <ul className="divide-y rounded-lg border">
            {affected.map(({ id, invoice }) => (
              <li key={id}>
                <Link
                  href={`/credit-cards/${job.creditCardId}/invoices/${id}`}
                  className="flex items-center justify-between gap-2 px-3 py-2.5 text-sm hover:bg-muted/50"
                >
                  <span className="first-letter:uppercase">
                    {invoice ? formatReferenceMonth(invoice.referenceMonth) : "Fatura"}
                  </span>
                  <ChevronRight className="h-4 w-4 text-muted-foreground" aria-hidden />
                </Link>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
