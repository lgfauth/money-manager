"use client";

import Link from "next/link";
import {
  ChevronRight,
  CreditCard as CreditCardIcon,
  MoreHorizontal,
  Pencil,
  Trash2,
  Unplug,
} from "lucide-react";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { FinancialEntityCard } from "@/components/shared/financial-entity-card";
import { CardBrandIcon } from "@/components/shared/card-brand-icon";
import { BankSyncStatus } from "@/components/shared/bank-sync-status";
import { InvoiceStatusBadge } from "@/components/credit-cards/invoice-status-badge";
import { cn } from "@/lib/utils";
import type { BankSyncInfo } from "@/hooks/use-bank-sync-status";
import type { CreditCardResponseDto } from "@/types/credit-card";
import {
  CREDIT_LIMIT_THRESHOLD_DANGER,
  CREDIT_LIMIT_THRESHOLD_WARNING,
} from "@/config/constants";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";

interface CreditCardCardProps {
  card: CreditCardResponseDto;
  syncInfo?: BankSyncInfo;
  onEdit: () => void;
  onDelete: () => void;
  onUnlink?: () => void;
}

const fmtDate = (iso: string) => {
  const d = new Date(iso);
  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(d);
};

export function CreditCardCard({
  card,
  syncInfo,
  onEdit,
  onDelete,
  onUnlink,
}: CreditCardCardProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();
  const used = card.limit - (card.availableLimit ?? 0);
  const limit = card.limit;
  const canShowUsage = limit > 0 && card.availableLimit != null;
  const usedPercent = canShowUsage ? Math.min(100, (used / limit) * 100) : 0;

  let usageColor = "bg-income";
  if (usedPercent >= CREDIT_LIMIT_THRESHOLD_DANGER) usageColor = "bg-expense";
  else if (usedPercent >= CREDIT_LIMIT_THRESHOLD_WARNING) usageColor = "bg-amber-500";

  return (
    <FinancialEntityCard
      indicator={
        <Link href={`/credit-cards/${card.id}`} className="shrink-0">
          <div
            className="flex h-9 w-9 items-center justify-center rounded-lg text-white"
            style={{ backgroundColor: card.color }}
          >
            <CreditCardIcon className="h-4 w-4" />
          </div>
        </Link>
      }
      name={card.name}
      nameSuffix={<CardBrandIcon brand={card.brand} />}
      menuTrigger={
        <DropdownMenu>
          <DropdownMenuTrigger className="outline-none">
            <MoreHorizontal className="h-4 w-4 text-muted-foreground" />
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            {syncInfo && onUnlink && (
              <DropdownMenuItem onClick={onUnlink}>
                <Unplug className="mr-2 h-4 w-4 text-amber-500" />
                Desvincular
              </DropdownMenuItem>
            )}
            <DropdownMenuItem onClick={onEdit}>
              <Pencil className="mr-2 h-4 w-4" />
              Editar
            </DropdownMenuItem>
            <DropdownMenuItem variant="destructive" onClick={onDelete}>
              <Trash2 className="mr-2 h-4 w-4" />
              Excluir
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      }
      accentColor={card.color}
      action={
        <Link
          href={`/credit-cards/${card.id}`}
          className="inline-flex items-center gap-1 rounded-lg px-2 py-1 text-xs font-medium hover:bg-muted/50 transition-colors"
        >
          <span>Ver faturas</span>
          <ChevronRight className="h-4 w-4" />
        </Link>
      }
    >
      <div className="space-y-4">
        {syncInfo && <BankSyncStatus syncInfo={syncInfo} />}

        <p className="text-[11px] text-muted-foreground">
          Fech. dia {card.closingDay} · Venc. dia {card.billingDueDay}
        </p>

        {canShowUsage && (
          <div className="space-y-2">
            <div className="flex items-baseline justify-between text-xs text-muted-foreground">
              <span>Utilizado</span>
              <span>{usedPercent.toFixed(0)}%</span>
            </div>
            <div className="h-2 w-full rounded-full bg-muted overflow-hidden">
              <div
                className={cn("h-full transition-all", usageColor)}
                style={{ width: `${usedPercent}%` }}
              />
            </div>
            <div className="flex items-baseline justify-between text-xs">
              <span className="text-muted-foreground">
                {formatMonetaryValue(used, card.currency)} usado
              </span>
              <span className="text-muted-foreground">
                limite {formatMonetaryValue(limit, card.currency)}
              </span>
            </div>
          </div>
        )}

        <div className="flex items-center justify-between rounded-lg bg-muted/40 px-3 py-2 text-xs">
          <div>
            <p className="text-muted-foreground">Disponível</p>
            <p className="font-semibold text-income">
              {formatMonetaryValue(card.availableLimit ?? 0, card.currency)}
            </p>
          </div>
          {card.currentInvoice ? (
            <div className="text-right">
              <div className="flex justify-end">
                <InvoiceStatusBadge status={card.currentInvoice.status} />
              </div>
              <p className="mt-1 text-muted-foreground">
                Vence em {fmtDate(card.currentInvoice.dueDate)}
              </p>
              {syncInfo && (
                <p className="mt-1 text-[10px] text-muted-foreground/70">
                  * Inclui compras parceladas de meses futuros. Valor do mês atual
                  calculado a partir das transações do período.
                </p>
              )}
            </div>
          ) : (
            <p className="text-muted-foreground">Sem fatura corrente</p>
          )}
        </div>
      </div>
    </FinancialEntityCard>
  );
}
