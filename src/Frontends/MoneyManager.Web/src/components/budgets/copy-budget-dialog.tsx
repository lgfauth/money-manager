"use client";

import { useState } from "react";
import { format, parseISO } from "date-fns";
import { ptBR } from "date-fns/locale";
import { useAllBudgets, useCopyBudget } from "@/hooks/use-budgets";
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
}

export function CopyBudgetDialog({
  open,
  onOpenChange,
  targetMonth,
}: CopyBudgetDialogProps) {
  const { data: allBudgets, isLoading } = useAllBudgets(open);
  const copyBudget = useCopyBudget();
  const [selectedMonth, setSelectedMonth] = useState<string | null>(null);

  // Exibe apenas orçamentos de outros meses
  const availableBudgets = allBudgets?.filter((b) => b.month !== targetMonth) ?? [];

  const formatMonth = (month: string) =>
    format(parseISO(`${month}-01`), "MMMM yyyy", { locale: ptBR });

  const handleCopy = () => {
    if (!selectedMonth) return;
    copyBudget.mutate(
      { sourceMonth: selectedMonth, targetMonth },
      { onSuccess: () => { setSelectedMonth(null); onOpenChange(false); } }
    );
  };

  const handleOpenChange = (value: boolean) => {
    if (!value) setSelectedMonth(null);
    onOpenChange(value);
  };

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle>Copiar orçamento</DialogTitle>
          <DialogDescription>
            Selecione o mês de origem para copiar para{" "}
            <span className="font-medium capitalize">{formatMonth(targetMonth)}</span>.
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
                onClick={() => setSelectedMonth(budget.month)}
                className={`w-full flex items-center justify-between rounded-lg border p-3 text-left text-sm transition-colors ${
                  selectedMonth === budget.month
                    ? "border-primary bg-primary/5"
                    : "border-border hover:bg-muted"
                }`}
              >
                <span className="capitalize">{formatMonth(budget.month)}</span>
                <span className="text-muted-foreground">
                  {budget.items.length} {budget.items.length === 1 ? "categoria" : "categorias"}
                </span>
              </button>
            ))
          )}
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => handleOpenChange(false)}>
            Cancelar
          </Button>
          <Button
            onClick={handleCopy}
            disabled={!selectedMonth || copyBudget.isPending}
          >
            {copyBudget.isPending ? "Copiando..." : "Copiar"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
