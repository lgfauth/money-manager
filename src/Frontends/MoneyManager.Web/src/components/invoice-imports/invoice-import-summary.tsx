"use client";

import { AlertTriangle, Info } from "lucide-react";
import { cn } from "@/lib/utils";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";
import { formatDateOnly, formatReferenceMonth } from "@/lib/invoice-import";
import type {
  CreditCardInvoiceImportResponseDto,
  InvoiceImportWarningResponseDto,
} from "@/types/invoice-import";

/** Dados da fatura lidos do documento: emissor, final, mês, fechamento, vencimento e total. */
export function InvoiceImportSummary({ job }: { job: CreditCardInvoiceImportResponseDto }) {
  const { formatMonetaryValue } = useMoneyPrivacy();
  const invoice = job.invoice;
  const referenceMonth = invoice?.referenceMonth ?? job.referenceMonth;

  const fields: { label: string; value: string; hint?: string }[] = [
    { label: "Mês de referência", value: formatReferenceMonth(referenceMonth) },
    {
      label: "Fechamento",
      value: formatDateOnly(invoice?.closingDate),
      hint: invoice?.closingDateCalculated
        ? "Calculado pelo cadastro do cartão (não aparece no documento)"
        : undefined,
    },
    { label: "Vencimento", value: formatDateOnly(invoice?.dueDate) },
    {
      label: "Total da fatura",
      value:
        invoice?.totalAmount !== null && invoice?.totalAmount !== undefined
          ? formatMonetaryValue(invoice.totalAmount)
          : "—",
    },
  ];

  return (
    <section className="space-y-3" aria-label="Resumo da fatura">
      {(invoice?.issuer || invoice?.cardLastDigits) && (
        <p className="text-sm text-muted-foreground">
          {invoice.issuer ?? "Fatura"}
          {invoice.cardLastDigits && ` · cartão final ${invoice.cardLastDigits}`}
        </p>
      )}
      <div className="grid gap-3 grid-cols-2 lg:grid-cols-4">
        {fields.map((field) => (
          <div key={field.label} className="min-w-0 rounded-xl border bg-card p-4">
            <p className="text-xs text-muted-foreground">{field.label}</p>
            <p className="mt-1 break-words text-base font-semibold first-letter:uppercase sm:text-lg">
              {field.value}
            </p>
            {field.hint && (
              <p className="mt-1 text-xs text-muted-foreground">{field.hint}</p>
            )}
          </div>
        ))}
      </div>
      {invoice?.isOpenOrPartial && (
        <WarningBox
          blocking={false}
          message="O documento parece ser de uma fatura ainda aberta ou incompleta. A importação é feita para faturas fechadas: confira se todos os lançamentos estão presentes."
        />
      )}
    </section>
  );
}

/** Alertas gerais da importação; os bloqueantes aparecem primeiro. */
export function InvoiceImportWarnings({
  warnings,
}: {
  warnings: InvoiceImportWarningResponseDto[];
}) {
  if (warnings.length === 0) return null;
  const sorted = [...warnings].sort((a, b) => Number(b.isBlocking) - Number(a.isBlocking));

  return (
    <section className="space-y-2" aria-label="Alertas da importação">
      {sorted.map((warning, index) => (
        <WarningBox
          key={`${warning.code}-${index}`}
          blocking={warning.isBlocking}
          message={warning.message}
        />
      ))}
    </section>
  );
}

function WarningBox({ blocking, message }: { blocking: boolean; message: string }) {
  const Icon = blocking ? AlertTriangle : Info;
  return (
    <div
      role={blocking ? "alert" : "status"}
      className={cn(
        "flex gap-2 rounded-lg border p-3 text-sm",
        blocking
          ? "border-expense/30 bg-expense/10 text-expense"
          : "border-amber-500/30 bg-amber-500/10 text-amber-800 dark:text-amber-300"
      )}
    >
      <Icon className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
      <p>
        {blocking && <span className="font-semibold">Impede a importação: </span>}
        {message}
      </p>
    </div>
  );
}
