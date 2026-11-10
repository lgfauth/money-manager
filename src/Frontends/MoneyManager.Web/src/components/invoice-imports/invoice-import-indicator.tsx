"use client";

import { useRouter } from "next/navigation";
import { FileClock, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuGroup,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { InvoiceImportStatusBadge } from "@/components/invoice-imports/invoice-import-status-badge";
import { useCreditCards } from "@/hooks/use-credit-cards";
import { useRecentInvoiceImports } from "@/hooks/use-invoice-imports";
import {
  INVOICE_IMPORTS_HREF,
  formatElapsed,
  invoiceImportHref,
  isActiveInvoiceImport,
  needsInvoiceImportAttention,
} from "@/lib/invoice-import";

/**
 * Indicador global das importações de fatura em andamento ou aguardando revisão. Só aparece
 * quando há alguma; a consulta periódica fica a cargo de `useRecentInvoiceImports`.
 */
export function InvoiceImportIndicator() {
  const router = useRouter();
  const { data: imports, dataUpdatedAt } = useRecentInvoiceImports();
  const { data: cards } = useCreditCards();

  const pending = (imports ?? []).filter((job) => needsInvoiceImportAttention(job.status));
  if (pending.length === 0) return null;

  const anyActive = pending.some((job) => isActiveInvoiceImport(job.status));
  const awaitingCount = pending.filter((job) => job.status === "AwaitingReview").length;
  const label =
    awaitingCount > 0
      ? `Importações de fatura: ${awaitingCount} aguardando revisão`
      : "Importações de fatura em andamento";
  // "Agora" do texto relativo: o horário da última consulta (render puro, atualiza a cada consulta)
  const now = dataUpdatedAt;

  return (
    <DropdownMenu>
      <DropdownMenuTrigger
        render={<Button variant="ghost" size="icon" className="relative" aria-label={label} title={label} />}
      >
        {anyActive ? (
          <Loader2 className="h-5 w-5 animate-spin text-primary" />
        ) : (
          <FileClock className="h-5 w-5" />
        )}
        <span
          className="absolute -right-0.5 -top-0.5 flex h-4 min-w-4 items-center justify-center rounded-full bg-primary px-1 text-[10px] font-semibold text-primary-foreground"
          aria-hidden
        >
          {pending.length}
        </span>
      </DropdownMenuTrigger>
      <DropdownMenuContent align="end" className="w-72 max-w-[calc(100vw-2rem)]">
        <DropdownMenuGroup>
          <DropdownMenuLabel>Importações de fatura</DropdownMenuLabel>
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <DropdownMenuGroup>
          {pending.map((job) => {
            const card = cards?.find((c) => c.id === job.creditCardId);
            return (
              <DropdownMenuItem
                key={job.id}
                onClick={() => router.push(invoiceImportHref(job.id))}
                className="flex items-center justify-between gap-2"
              >
                <span className="min-w-0">
                  <span className="block truncate text-sm">{card?.name ?? "Cartão"}</span>
                  <span className="block text-xs text-muted-foreground">
                    Enviada {formatElapsed(job.createdAt, now)}
                  </span>
                </span>
                <InvoiceImportStatusBadge status={job.status} />
              </DropdownMenuItem>
            );
          })}
        </DropdownMenuGroup>
        <DropdownMenuSeparator />
        <DropdownMenuGroup>
          <DropdownMenuItem onClick={() => router.push(INVOICE_IMPORTS_HREF)}>
            Ver todas as importações
          </DropdownMenuItem>
        </DropdownMenuGroup>
      </DropdownMenuContent>
    </DropdownMenu>
  );
}
