"use client";

import { Info } from "lucide-react";
import { Alert, AlertDescription, AlertTitle } from "@/components/ui/alert";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";
import type { ReportCurrencyTotals, ReportDataQuality } from "@/types/report";

interface ReportDataQualityAlertProps {
  currency: string;
  dataQuality: ReportDataQuality;
  otherCurrencies: ReportCurrencyTotals[];
}

export function ReportDataQualityAlert({ currency, dataQuality, otherCurrencies }: ReportDataQualityAlertProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();
  const notes: string[] = [];

  if (dataQuality.uncategorizedCount > 0) {
    notes.push(
      `${dataQuality.uncategorizedCount} lançamento(s) sem categoria somando ${formatMonetaryValue(dataQuality.uncategorizedTotal, currency)}.`
    );
  }
  if (dataQuality.pendingCardTransactionsCount > 0) {
    notes.push(
      `${dataQuality.pendingCardTransactionsCount} compra(s) no cartão ainda pendente(s) de confirmação pelo banco.`
    );
  }
  if (dataQuality.excludedTransfersCount > 0) {
    notes.push(
      `${dataQuality.excludedTransfersCount} transferência(s) entre suas contas não foram contadas como receita ou despesa.`
    );
  }
  if (dataQuality.excludedInvoicePaymentsCount > 0) {
    notes.push(
      `${dataQuality.excludedInvoicePaymentsCount} pagamento(s) de fatura foram substituídos pelas compras do cartão, para não contar em dobro.`
    );
  }
  for (const other of otherCurrencies) {
    notes.push(
      `Lançamentos em ${other.currency} ficaram fora dos totais: receitas ${formatMonetaryValue(other.income, other.currency)}, despesas ${formatMonetaryValue(other.expense, other.currency)}.`
    );
  }

  if (notes.length === 0) return null;

  return (
    <Alert>
      <Info />
      <AlertTitle>Como os números foram apurados</AlertTitle>
      <AlertDescription>
        <ul className="list-disc space-y-0.5 pl-4">
          {notes.map((note) => (
            <li key={note}>{note}</li>
          ))}
        </ul>
      </AlertDescription>
    </Alert>
  );
}
