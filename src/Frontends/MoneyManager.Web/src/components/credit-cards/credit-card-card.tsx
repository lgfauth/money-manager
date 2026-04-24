"use client";

import Link from "next/link";
import {
  ChevronRight,
  CreditCard as CreditCardIcon,
  MoreHorizontal,
  Pencil,
  Trash2,
} from "lucide-react";
import { Card, CardContent } from "@/components/ui/card";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { InvoiceStatusBadge } from "@/components/credit-cards/invoice-status-badge";
import { cn } from "@/lib/utils";
import type { CreditCardResponseDto } from "@/types/credit-card";
import {
  CREDIT_LIMIT_THRESHOLD_DANGER,
  CREDIT_LIMIT_THRESHOLD_WARNING,
} from "@/config/constants";

interface CreditCardCardProps {
  card: CreditCardResponseDto;
  onEdit: () => void;
  onDelete: () => void;
}

const fmt = (value: number, currency: string) =>
  new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency,
  }).format(value);

const fmtDate = (iso: string) => {
  const d = new Date(iso);
  return new Intl.DateTimeFormat("pt-BR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  }).format(d);
};

export function CreditCardCard({ card, onEdit, onDelete }: CreditCardCardProps) {
  const used = card.currentBalance;
  const limit = card.limit;
  const usedPercent = limit > 0 ? Math.min(100, (used / limit) * 100) : 0;

  let usageColor = "bg-income";
  if (usedPercent >= CREDIT_LIMIT_THRESHOLD_DANGER) usageColor = "bg-expense";
  else if (usedPercent >= CREDIT_LIMIT_THRESHOLD_WARNING) usageColor = "bg-amber-500";

  return (
    <Card className="rounded-xl overflow-hidden hover:shadow-md transition-shadow">
      <div
        className="h-2 w-full"
        style={{ backgroundColor: card.color }}
        aria-hidden
      />
      <CardContent className="space-y-4 pt-4">
        <div className="flex items-start justify-between gap-2">
          <Link
            href={`/credit-cards/${card.id}`}
            className="flex items-center gap-2 group"
          >
            <div
              className="flex h-9 w-9 items-center justify-center rounded-lg text-white"
              style={{ backgroundColor: card.color }}
            >
              <CreditCardIcon className="h-4 w-4" />
            </div>
            <div>
              <p className="text-sm font-semibold leading-tight group-hover:underline">
                {card.name}
              </p>
              <p className="text-[11px] text-muted-foreground">
                Fech. dia {card.closingDay} · Venc. dia {card.billingDueDay}
              </p>
            </div>
          </Link>
          <DropdownMenu>
            <DropdownMenuTrigger className="outline-none">
              <MoreHorizontal className="h-4 w-4 text-muted-foreground" />
            </DropdownMenuTrigger>
            <DropdownMenuContent align="end">
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
        </div>

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
              {fmt(used, card.currency)}
            </span>
            <span className="font-medium">{fmt(limit, card.currency)}</span>
          </div>
        </div>

        <div className="flex items-center justify-between rounded-lg bg-muted/40 px-3 py-2 text-xs">
          <div>
            <p className="text-muted-foreground">Disponível</p>
            <p className="font-semibold text-income">
              {fmt(card.availableLimit, card.currency)}
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
            </div>
          ) : (
            <p className="text-muted-foreground">Sem fatura corrente</p>
          )}
        </div>

        <Link
          href={`/credit-cards/${card.id}`}
          className="flex items-center justify-between rounded-lg border px-3 py-2 text-xs font-medium hover:bg-muted/50 transition-colors"
        >
          <span>Ver faturas</span>
          <ChevronRight className="h-4 w-4" />
        </Link>
      </CardContent>
    </Card>
  );
}
