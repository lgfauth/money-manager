"use client";

import { useState, useMemo } from "react";
import { format, addMonths, subMonths } from "date-fns";
import { ptBR } from "date-fns/locale";
import { ChevronLeft, ChevronRight, PiggyBank, Plus, Settings } from "lucide-react";
import { useBudget } from "@/hooks/use-budgets";

import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState } from "@/components/shared/empty-state";
import { StatCard } from "@/components/shared/stat-card";
import { CardGridSkeleton } from "@/components/shared/loading-skeleton";
import { BudgetCard } from "@/components/budgets/budget-card";
import { BudgetWizard } from "@/components/budgets/budget-wizard";

export default function BudgetsPage() {
  const [currentDate, setCurrentDate] = useState(new Date());
  const month = format(currentDate, "yyyy-MM");
  const monthLabel = format(currentDate, "MMMM yyyy", { locale: ptBR });

  const { data: budget, isLoading } = useBudget(month);
  const [wizardOpen, setWizardOpen] = useState(false);

  const stats = useMemo(() => {
    if (!budget || !budget.items.length)
      return { totalLimit: 0, totalSpent: 0, overBudget: 0 };
    const totalLimit = budget.items.reduce((s, i) => s + i.limitAmount, 0);
    const totalSpent = budget.items.reduce((s, i) => s + i.spentAmount, 0);
    const overBudget = budget.items.filter(
      (i) => i.spentAmount > i.limitAmount
    ).length;
    return { totalLimit, totalSpent, overBudget };
  }, [budget]);

  const fmt = (v: number) =>
    new Intl.NumberFormat("pt-BR", {
      style: "currency",
      currency: "BRL",
    }).format(v);

  const percent =
    stats.totalLimit > 0
      ? ((stats.totalSpent / stats.totalLimit) * 100).toFixed(0)
      : "0";

  return (
    <div className="space-y-6">
      <PageHeader
        title="Orcamentos"
        description="Defina limites de gastos por categoria."
      >
        <Button onClick={() => setWizardOpen(true)}>
          {budget ? (
            <>
              <Settings className="mr-2 h-4 w-4" />
              Editar Orcamento
            </>
          ) : (
            <>
              <Plus className="mr-2 h-4 w-4" />
              Criar Orcamento
            </>
          )}
        </Button>
      </PageHeader>

      {/* Month navigator */}
      <div className="flex items-center gap-4">
        <Button
          variant="outline"
          size="icon"
          onClick={() => setCurrentDate((d) => subMonths(d, 1))}
        >
          <ChevronLeft className="h-4 w-4" />
        </Button>
        <span className="text-sm font-medium capitalize min-w-[120px] text-center">
          {monthLabel}
        </span>
        <Button
          variant="outline"
          size="icon"
          onClick={() => setCurrentDate((d) => addMonths(d, 1))}
        >
          <ChevronRight className="h-4 w-4" />
        </Button>
      </div>

      {/* Stats */}
      {budget && budget.items.length > 0 && (
        <div className="grid gap-4 sm:grid-cols-3">
          <StatCard
            title="Orcamento Total"
            value={fmt(stats.totalLimit)}
            icon={PiggyBank}
          />
          <StatCard
            title="Gasto no Mes"
            value={fmt(stats.totalSpent)}
            icon={PiggyBank}
            variant={
              Number(percent) >= 90
                ? "expense"
                : Number(percent) >= 75
                  ? "warning"
                  : "income"
            }
          />
          <StatCard
            title="Utilizado"
            value={`${percent}%`}
            icon={PiggyBank}
            trend={
              stats.overBudget > 0
                ? {
                    value: -stats.overBudget,
                    label: `${stats.overBudget} acima do limite`,
                  }
                : undefined
            }
          />
        </div>
      )}

      {/* Budget cards */}
      {isLoading ? (
        <CardGridSkeleton />
      ) : !budget || budget.items.length === 0 ? (
        <EmptyState
          icon={PiggyBank}
          title="Nenhum orcamento para este mes"
          description="Crie um orcamento para controlar seus gastos por categoria."
          actionLabel="Criar Orcamento"
          onAction={() => setWizardOpen(true)}
        />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {budget.items.map((item) => (
            <BudgetCard key={item.categoryId} item={item} />
          ))}
        </div>
      )}

      <BudgetWizard
        open={wizardOpen}
        onOpenChange={setWizardOpen}
        month={month}
        existingBudget={budget}
      />
    </div>
  );
}
