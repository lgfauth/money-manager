"use client";

import { useMemo, useState } from "react";
import { AlertTriangle, Pencil, Search } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { cn } from "@/lib/utils";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";
import { formatDateOnly } from "@/lib/invoice-import";
import {
  isBlockingItemWarning,
  itemWarningTexts,
  type ReviewItemState,
} from "@/lib/invoice-import-review";
import type {
  InvoiceImportItemResponseDto,
  InvoiceImportWarningCode,
} from "@/types/invoice-import";

/** Lançamento pronto para exibir: dados extraídos, valores atuais e alertas recalculados. */
export interface ReviewRow {
  item: InvoiceImportItemResponseDto;
  state: ReviewItemState;
  warnings: InvoiceImportWarningCode[];
  hasBlocking: boolean;
  /** Já importado antes: não pode ser marcado. */
  locked: boolean;
  source: string | null;
  categoryName: string | null;
  /** Problema apontado pelo servidor na última tentativa de confirmação. */
  serverError: string | null;
}

type Filter = "all" | "warnings" | "selected";

interface InvoiceImportItemsProps {
  rows: ReviewRow[];
  hiddenByHolderCount: number;
  onToggle: (itemId: string) => void;
  onSetSelected: (itemIds: string[], selected: boolean) => void;
  onEdit: (itemId: string) => void;
}

const normalize = (text: string) =>
  text.normalize("NFD").replace(/[̀-ͯ]/g, "").toLowerCase();

/** Lista dos lançamentos extraídos: filtros, busca, seleção e acesso à edição. */
export function InvoiceImportItems({
  rows,
  hiddenByHolderCount,
  onToggle,
  onSetSelected,
  onEdit,
}: InvoiceImportItemsProps) {
  const [filter, setFilter] = useState<Filter>("all");
  const [search, setSearch] = useState("");

  const counts = {
    all: rows.length,
    warnings: rows.filter((r) => r.warnings.length > 0 || r.serverError).length,
    selected: rows.filter((r) => r.state.selected).length,
  };

  const visible = useMemo(() => {
    const term = normalize(search.trim());
    return rows.filter((row) => {
      if (filter === "warnings" && row.warnings.length === 0 && !row.serverError) return false;
      if (filter === "selected" && !row.state.selected) return false;
      return !term || normalize(row.state.description).includes(term);
    });
  }, [rows, filter, search]);

  const selectableVisible = visible.filter((r) => !r.locked).map((r) => r.item.id);
  const allVisibleSelected =
    selectableVisible.length > 0 &&
    visible.filter((r) => !r.locked).every((r) => r.state.selected);

  const filters: { key: Filter; label: string }[] = [
    { key: "all", label: `Todos (${counts.all})` },
    { key: "warnings", label: `Com alertas (${counts.warnings})` },
    { key: "selected", label: `Selecionados (${counts.selected})` },
  ];

  return (
    <section className="space-y-3" aria-labelledby="invoice-import-items-title">
      <div className="flex flex-col gap-3 sm:flex-row sm:items-end sm:justify-between">
        <div>
          <h2 id="invoice-import-items-title" className="text-base font-semibold">
            Lançamentos
          </h2>
          <p className="text-sm text-muted-foreground">
            Marque o que deve entrar no cartão. Toque em um lançamento para corrigir.
          </p>
        </div>
        <div className="relative sm:w-64">
          <Search
            className="pointer-events-none absolute top-1/2 left-2.5 h-4 w-4 -translate-y-1/2 text-muted-foreground"
            aria-hidden
          />
          <Input
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder="Buscar descrição"
            aria-label="Buscar lançamento pela descrição"
            className="pl-8"
          />
        </div>
      </div>

      <div className="flex flex-wrap items-center gap-2">
        {filters.map((f) => (
          <Button
            key={f.key}
            type="button"
            size="sm"
            variant={filter === f.key ? "default" : "outline"}
            onClick={() => setFilter(f.key)}
            aria-pressed={filter === f.key}
          >
            {f.label}
          </Button>
        ))}
        {selectableVisible.length > 0 && (
          <Button
            type="button"
            size="sm"
            variant="ghost"
            onClick={() => onSetSelected(selectableVisible, !allVisibleSelected)}
          >
            {allVisibleSelected ? "Desmarcar exibidos" : "Marcar exibidos"}
          </Button>
        )}
      </div>

      {hiddenByHolderCount > 0 && (
        <p className="text-sm text-muted-foreground">
          {hiddenByHolderCount === 1
            ? "1 lançamento de titular não escolhido está oculto."
            : `${hiddenByHolderCount} lançamentos de titulares não escolhidos estão ocultos.`}
        </p>
      )}

      {visible.length === 0 ? (
        <p className="rounded-xl border bg-card p-6 text-center text-sm text-muted-foreground">
          Nenhum lançamento neste filtro.
        </p>
      ) : (
        <>
          {/* Desktop: tabela */}
          <div className="hidden rounded-xl border bg-card md:block">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead className="w-10">
                    <span className="sr-only">Importar</span>
                  </TableHead>
                  <TableHead className="w-28">Data</TableHead>
                  <TableHead>Descrição</TableHead>
                  <TableHead className="w-24">Parcela</TableHead>
                  <TableHead className="w-40">Categoria</TableHead>
                  <TableHead className="w-32 text-right">Valor</TableHead>
                  <TableHead className="w-12">
                    <span className="sr-only">Editar</span>
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {visible.map((row) => (
                  <TableRow
                    key={row.item.id}
                    className={cn(!row.state.selected && "text-muted-foreground")}
                  >
                    <TableCell>
                      <RowCheckbox row={row} onToggle={onToggle} />
                    </TableCell>
                    <TableCell className="whitespace-nowrap">
                      {formatDateOnly(row.state.date || null)}
                    </TableCell>
                    <TableCell className="whitespace-normal">
                      <RowDetails row={row} />
                    </TableCell>
                    <TableCell>{describeInstallment(row.state)}</TableCell>
                    <TableCell className="truncate">{row.categoryName ?? "—"}</TableCell>
                    <TableCell className="text-right">
                      <RowAmount row={row} />
                    </TableCell>
                    <TableCell>
                      <Button
                        type="button"
                        size="icon-sm"
                        variant="ghost"
                        aria-label={`Editar ${row.state.description || "lançamento"}`}
                        onClick={() => onEdit(row.item.id)}
                      >
                        <Pencil className="h-4 w-4" />
                      </Button>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>

          {/* Celular: lista de cartões */}
          <ul className="space-y-2 md:hidden">
            {visible.map((row) => (
              <li
                key={row.item.id}
                className={cn(
                  "flex gap-3 rounded-xl border bg-card p-3",
                  !row.state.selected && "text-muted-foreground"
                )}
              >
                <RowCheckbox row={row} onToggle={onToggle} />
                <button
                  type="button"
                  className="min-w-0 flex-1 text-left"
                  onClick={() => onEdit(row.item.id)}
                >
                  <div className="flex items-start justify-between gap-2">
                    <RowDetails row={row} />
                    <RowAmount row={row} />
                  </div>
                  <p className="mt-1 text-xs text-muted-foreground">
                    {formatDateOnly(row.state.date || null)}
                    {row.state.totalInstallments && ` · parcela ${describeInstallment(row.state)}`}
                    {row.categoryName && ` · ${row.categoryName}`}
                  </p>
                </button>
              </li>
            ))}
          </ul>
        </>
      )}
    </section>
  );
}

function describeInstallment(state: ReviewItemState) {
  return state.totalInstallments && state.installmentNumber
    ? `${state.installmentNumber}/${state.totalInstallments}`
    : "À vista";
}

function RowCheckbox({ row, onToggle }: { row: ReviewRow; onToggle: (id: string) => void }) {
  return (
    <input
      type="checkbox"
      className="mt-1 h-4 w-4 shrink-0 accent-primary disabled:cursor-not-allowed"
      checked={row.state.selected}
      disabled={row.locked}
      onChange={() => onToggle(row.item.id)}
      aria-label={`Importar ${row.state.description || "lançamento"}`}
      title={row.locked ? "Já importado: não será cadastrado de novo" : undefined}
    />
  );
}

function RowAmount({ row }: { row: ReviewRow }) {
  const { formatMonetaryValue } = useMoneyPrivacy();
  const isRefund = row.item.type === "Refund";
  return (
    <span
      className={cn("shrink-0 font-medium whitespace-nowrap", isRefund && "text-income")}
    >
      {isRefund ? "− " : ""}
      {formatMonetaryValue(row.state.amount)}
    </span>
  );
}

function RowDetails({ row }: { row: ReviewRow }) {
  const isRefund = row.item.type === "Refund";
  const meta = [
    row.item.holderName,
    row.item.cardLastDigits ? `final ${row.item.cardLastDigits}` : null,
    row.source,
  ].filter(Boolean);

  return (
    <div className="min-w-0 space-y-1">
      <p className="font-medium break-words text-foreground">
        {row.state.description || <span className="italic">Sem descrição</span>}
        {isRefund && (
          <Badge variant="outline" className="ml-2 border-income/30 text-income">
            Estorno
          </Badge>
        )}
      </p>
      {meta.length > 0 && <p className="text-xs text-muted-foreground">{meta.join(" · ")}</p>}
      {row.warnings.length > 0 && (
        <div className="flex flex-wrap gap-1">
          {row.warnings.map((code) => {
            const blocking = isBlockingItemWarning(code);
            return (
              <Badge
                key={code}
                variant="outline"
                title={itemWarningTexts[code]?.help}
                className={cn(
                  blocking
                    ? "border-expense/30 bg-expense/10 text-expense"
                    : "border-amber-500/30 bg-amber-500/10 text-amber-700 dark:text-amber-400"
                )}
              >
                {blocking && <AlertTriangle aria-hidden />}
                {itemWarningTexts[code]?.label ?? code}
              </Badge>
            );
          })}
        </div>
      )}
      {row.serverError && (
        <p role="alert" className="text-xs text-expense">
          {row.serverError}
        </p>
      )}
    </div>
  );
}
