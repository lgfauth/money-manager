"use client";

import { useState } from "react";
import { format, parseISO } from "date-fns";
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
  const { data: allBudgets, isLoading } = useAllBudgets(open);
  const [selectedId, setSelectedId] = useState<string | null>(null);

  // Exibe apenas orçamentos de meses diferentes do mês destino, ordenados do mais recente
  const availableBudgets = (allBudgets ?? [])
    .filter((b) => b.month !== targetMonth)
    .sort((a, b) => b.month.localeCompare(a.month));

  const formatMonth = (month: string) =>
    format(parseISO(`${month}-01`), "MM/yyyy");

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
          {isLoading ? (
            <div className="flex items-center justify-center py-6">
              <div className="h-6 w-6 animate-spin rounded-full border-4 border-primary border-t-transparent" />
            </div>
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
