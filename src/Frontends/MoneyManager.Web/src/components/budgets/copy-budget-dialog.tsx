"use client";

import { useEffect, useState } from "react";
import { format, isValid, parseISO } from "date-fns";
import { useAllBudgets } from "@/hooks/use-budgets";
import type { BudgetResponseDto } from "@/types/budget";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

interface CopyBudgetDialogProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  targetMonth: string;
  // Chamado quando o usuário confirma a seleção; a página abre o wizard para edição
  onSelectBudget: (sourceBudget: BudgetResponseDto) => void;
}

export function CopyBudgetDialog({
  open,
  onOpenChange,
  targetMonth,
  onSelectBudget,
}: CopyBudgetDialogProps) {
  // enabled=false para não buscar automaticamente; refetch() é disparado pelo useEffect
  const { data: allBudgets, isLoading, isError, refetch } = useAllBudgets(false);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  // Busca os orçamentos toda vez que o modal é aberto
  useEffect(() => {
    if (open) {
      refetch();
    }
  }, [open, refetch]);

  // Exibe apenas os 3 orçamentos mais recentes com mês válido (YYYY-MM) e diferentes do mês destino
  const MONTH_REGEX = /^\d{4}-\d{2}$/;
  const availableBudgets = (allBudgets ?? [])
    .filter((b) => MONTH_REGEX.test(b.month) && b.month !== targetMonth)
    .sort((a, b) => b.month.localeCompare(a.month))
    .slice(0, 3);

  const formatMonth = (month: string): string => {
    if (!month) return month;
    const d = parseISO(`${month}-01`);
    return isValid(d) ? format(d, "MM/yyyy") : month;
  };

  const handleConfirm = () => {
    const source = availableBudgets.find((b) => b.id === selectedId);
    if (!source) return;
    setSelectedId(null);
    onOpenChange(false);
    onSelectBudget(source);
  };

  const handleOpenChange = (value: boolean) => {
    if (!value) setSelectedId(null);
    onOpenChange(value);
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle>Copiar orçamento</DialogTitle>
          <DialogDescription>
            Selecione o mês de origem para copiar para{" "}
            <span className="font-medium">{formatMonth(targetMonth)}</span>.
            Os valores poderão ser ajustados antes de salvar.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-2 max-h-60 overflow-y-auto">
          {!open ? null : isLoading ? (
            <div className="flex items-center justify-center py-6">
              <div className="h-6 w-6 animate-spin rounded-full border-4 border-primary border-t-transparent" />
            </div>
          ) : isError ? (
            <p className="text-sm text-destructive text-center py-4">
              Erro ao carregar orçamentos. Tente novamente.
            </p>
          ) : availableBudgets.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-4">
              Nenhum orçamento anterior encontrado.
            </p>
          ) : (
            availableBudgets.map((budget) => (
              <button
                key={budget.id}
                type="button"
                onClick={() => setSelectedId(budget.id)}
                className={`w-full flex items-center justify-between rounded-lg border p-3 text-left text-sm transition-colors ${
                  selectedId === budget.id
                    ? "border-primary bg-primary/5"
                    : "border-border hover:bg-muted"
                }`}
              >
                <span className="font-medium">{formatMonth(budget.month)}</span>
                <span className="text-muted-foreground">
                  {budget.items.length}{" "}
                  {budget.items.length === 1 ? "categoria" : "categorias"}
                </span>
              </button>
            ))
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => handleOpenChange(false)}>
            Cancelar
          </Button>
          <Button onClick={handleConfirm} disabled={!selectedId || isLoading}>
            Próximo
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
