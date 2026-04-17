"use client";

import Link from "next/link";
import { format } from "date-fns";
import { ptBR } from "date-fns/locale";
import { CreditCard as CreditCardIcon, Trash2 } from "lucide-react";

import { cn } from "@/lib/utils";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Card, CardContent } from "@/components/ui/card";
import type { CreditCardTransactionResponseDto } from "@/types/credit-card";
import type { CreditCardResponseDto } from "@/types/credit-card";

interface CreditCardTransactionTableProps {
  transactions: CreditCardTransactionResponseDto[];
  cards: CreditCardResponseDto[] | undefined;
  onDelete: (tx: CreditCardTransactionResponseDto) => void;
}

function fmt(value: number, currency: string) {
  return new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency,
  }).format(value);
}

function CategoryBadge({ name, color }: { name: string; color: string }) {
  return (
    <div className="inline-flex items-center gap-2 rounded-full bg-muted/70 px-2.5 py-1 text-xs font-medium text-foreground">
      <span
        className="h-2.5 w-2.5 rounded-full shrink-0"
        style={{ backgroundColor: color || "#64748b" }}
      />
      <span className="truncate">{name || "Sem categoria"}</span>
    </div>
  );
}

function CardBadge({
  card,
}: {
  card: CreditCardResponseDto | undefined;
}) {
  if (!card) return <span className="text-xs text-muted-foreground">—</span>;
  return (
    <div className="inline-flex items-center gap-2 rounded-full bg-muted/70 px-2.5 py-1 text-xs font-medium text-foreground">
      <span
        className="h-2.5 w-2.5 rounded-full shrink-0"
        style={{ backgroundColor: card.color }}
      />
      <span className="truncate">{card.name}</span>
    </div>
  );
}

export function CreditCardTransactionTable({
  transactions,
  cards,
  onDelete,
}: CreditCardTransactionTableProps) {
  const cardById = new Map((cards ?? []).map((c) => [c.id, c]));

  return (
    <>
      {/* Desktop table */}
      <div className="hidden md:block rounded-[10px] border overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Compra</TableHead>
              <TableHead>Descrição</TableHead>
              <TableHead>Cartão</TableHead>
              <TableHead>Categoria</TableHead>
              <TableHead>Parcela</TableHead>
              <TableHead className="text-right">Valor</TableHead>
              <TableHead className="w-10" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {transactions.map((tx) => {
              const card = cardById.get(tx.creditCardId);
              return (
                <TableRow key={tx.id}>
                  <TableCell className="whitespace-nowrap">
                    {format(new Date(tx.purchaseDate), "dd/MM/yyyy", {
                      locale: ptBR,
                    })}
                  </TableCell>
                  <TableCell className="max-w-[220px] truncate">
                    <Link
                      href={`/credit-cards/${tx.creditCardId}/invoices/${tx.invoiceId}`}
                      className="hover:underline"
                    >
                      {tx.description}
                    </Link>
                  </TableCell>
                  <TableCell>
                    <CardBadge card={card} />
                  </TableCell>
                  <TableCell>
                    <CategoryBadge
                      name={tx.categoryName}
                      color={tx.categoryColor}
                    />
                  </TableCell>
                  <TableCell className="whitespace-nowrap text-xs text-muted-foreground">
                    {tx.installmentNumber}/{tx.totalInstallments}
                  </TableCell>
                  <TableCell className="text-right font-semibold font-heading text-expense">
                    -{fmt(tx.installmentAmount, tx.currency)}
                  </TableCell>
                  <TableCell>
                    <button
                      type="button"
                      onClick={() => onDelete(tx)}
                      className="text-muted-foreground hover:text-destructive"
                      aria-label="Excluir compra"
                    >
                      <Trash2 className="h-4 w-4" />
                    </button>
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </div>

      {/* Mobile cards */}
      <div className="md:hidden space-y-2">
        {transactions.map((tx) => {
          const card = cardById.get(tx.creditCardId);
          return (
            <Card
              key={tx.id}
              className="rounded-[10px] hover:shadow-md transition-shadow"
            >
              <CardContent className="flex items-center gap-3 p-3">
                <div
                  className={cn(
                    "rounded-full p-2 bg-muted text-expense"
                  )}
                >
                  <CreditCardIcon className="h-4 w-4" />
                </div>
                <div className="flex-1 min-w-0">
                  <Link
                    href={`/credit-cards/${tx.creditCardId}/invoices/${tx.invoiceId}`}
                    className="text-sm font-medium truncate block hover:underline"
                  >
                    {tx.description}
                  </Link>
                  <div className="mt-1 flex flex-wrap items-center gap-1.5">
                    <CategoryBadge
                      name={tx.categoryName}
                      color={tx.categoryColor}
                    />
                    <CardBadge card={card} />
                  </div>
                </div>
                <div className="text-right">
                  <p className="text-sm font-semibold font-heading text-expense">
                    -{fmt(tx.installmentAmount, tx.currency)}
                  </p>
                  <p className="text-[10px] text-muted-foreground">
                    {tx.installmentNumber}/{tx.totalInstallments}
                  </p>
                </div>
                <button
                  type="button"
                  onClick={() => onDelete(tx)}
                  className="text-muted-foreground hover:text-destructive"
                  aria-label="Excluir compra"
                >
                  <Trash2 className="h-4 w-4" />
                </button>
              </CardContent>
            </Card>
          );
        })}
      </div>
    </>
  );
}
