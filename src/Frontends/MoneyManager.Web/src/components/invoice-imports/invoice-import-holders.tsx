"use client";

import { Users } from "lucide-react";
import { cn } from "@/lib/utils";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";
import type {
  InvoiceImportHolderResponseDto,
  InvoiceImportHolderType,
} from "@/types/invoice-import";

const holderTypeLabels: Record<InvoiceImportHolderType, string> = {
  Primary: "Titular",
  Additional: "Adicional",
  Unknown: "Não identificado",
};

interface InvoiceImportHoldersProps {
  holders: InvoiceImportHolderResponseDto[];
  selected: string[];
  onToggle: (name: string) => void;
}

/** Escolha de quais titulares da fatura terão os lançamentos importados. */
export function InvoiceImportHolders({ holders, selected, onToggle }: InvoiceImportHoldersProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();
  const noneSelected = selected.length === 0;

  return (
    <section className="space-y-3" aria-labelledby="invoice-import-holders-title">
      <div className="flex items-start gap-2">
        <Users className="mt-0.5 h-4 w-4 shrink-0 text-muted-foreground" aria-hidden />
        <div>
          <h2 id="invoice-import-holders-title" className="text-base font-semibold">
            De quem são os lançamentos?
          </h2>
          <p className="text-sm text-muted-foreground">
            A fatura tem mais de um titular. Escolha de quais deles os lançamentos entram neste
            cartão; os demais ficam de fora.
          </p>
        </div>
      </div>

      <div className="grid gap-2 sm:grid-cols-2">
        {holders.map((holder) => {
          const checked = selected.includes(holder.name);
          return (
            <label
              key={holder.name}
              className={cn(
                "flex cursor-pointer items-start gap-3 rounded-xl border bg-card p-3 transition-colors",
                checked ? "border-primary/50 bg-primary/5" : "hover:bg-muted/40"
              )}
            >
              <input
                type="checkbox"
                className="mt-1 h-4 w-4 accent-primary"
                checked={checked}
                onChange={() => onToggle(holder.name)}
              />
              <span className="min-w-0 flex-1">
                <span className="block font-medium break-words">{holder.name}</span>
                <span className="block text-xs text-muted-foreground">
                  {holderTypeLabels[holder.type] ?? holder.type}
                  {holder.cardLastDigits.length > 0 &&
                    ` · final ${holder.cardLastDigits.join(", ")}`}
                </span>
                <span className="block text-sm">
                  {holder.itemCount === 1 ? "1 lançamento" : `${holder.itemCount} lançamentos`} ·{" "}
                  {formatMonetaryValue(holder.totalAmount)}
                </span>
              </span>
            </label>
          );
        })}
      </div>

      {noneSelected && (
        <p role="alert" className="text-sm text-expense">
          Escolha ao menos um titular para continuar.
        </p>
      )}
    </section>
  );
}
