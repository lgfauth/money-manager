"use client";

import { Check } from "lucide-react";
import { CardBrandIcon } from "@/components/shared/card-brand-icon";
import { cn } from "@/lib/utils";
import type { InvoiceImportEligibleCardResponseDto } from "@/types/invoice-import";

interface InvoiceImportCardPickerProps {
  cards: InvoiceImportEligibleCardResponseDto[];
  value: string | null;
  onChange: (cardId: string) => void;
  disabled?: boolean;
}

/** Escolha do cartão da fatura entre os elegíveis (os não sincronizados pelo Open Finance). */
export function InvoiceImportCardPicker({
  cards,
  value,
  onChange,
  disabled,
}: InvoiceImportCardPickerProps) {
  return (
    <div role="radiogroup" aria-label="Cartão da fatura" className="grid gap-2 sm:grid-cols-2">
      {cards.map((card) => {
        const selected = card.id === value;
        return (
          <button
            key={card.id}
            type="button"
            role="radio"
            aria-checked={selected}
            disabled={disabled}
            onClick={() => onChange(card.id)}
            className={cn(
              "flex min-w-0 items-center gap-3 rounded-xl border bg-card p-3 text-left transition-colors",
              "focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring",
              "disabled:cursor-not-allowed disabled:opacity-60",
              selected ? "border-primary ring-1 ring-primary" : "hover:bg-muted/50"
            )}
          >
            <span
              className="h-8 w-1.5 shrink-0 rounded-full"
              style={{ backgroundColor: card.color }}
              aria-hidden
            />
            <span className="min-w-0 flex-1">
              <span className="flex items-center gap-2">
                <span className="truncate font-medium">{card.name}</span>
                <CardBrandIcon brand={card.brand} />
              </span>
              <span className="block text-xs text-muted-foreground">
                Fecha dia {card.closingDay} · vence dia {card.billingDueDay}
              </span>
            </span>
            <span
              className={cn(
                "flex h-5 w-5 shrink-0 items-center justify-center rounded-full border",
                selected ? "border-primary bg-primary text-primary-foreground" : "border-muted-foreground/40"
              )}
              aria-hidden
            >
              {selected && <Check className="h-3 w-3" />}
            </span>
          </button>
        );
      })}
    </div>
  );
}
