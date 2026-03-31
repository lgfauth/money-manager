"use client";

import { useState, useMemo } from "react";
import { Plus, RefreshCw, Pause, Play } from "lucide-react";
import {
  useRecurring,
  useDeleteRecurring,
  useUpdateRecurring,
} from "@/hooks/use-recurring";
import type { RecurringTransactionResponseDto } from "@/types/recurring";

import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState } from "@/components/shared/empty-state";
import { StatCard } from "@/components/shared/stat-card";
import { CardGridSkeleton } from "@/components/shared/loading-skeleton";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { RecurringCard } from "@/components/recurring/recurring-card";
import { RecurringForm } from "@/components/recurring/recurring-form";

export default function RecurringPage() {
  const { data: recurring, isLoading } = useRecurring();
  const deleteRecurring = useDeleteRecurring();
  const updateRecurring = useUpdateRecurring();

  const [formOpen, setFormOpen] = useState(false);
  const [editingItem, setEditingItem] =
    useState<RecurringTransactionResponseDto | null>(null);
  const [deletingItem, setDeletingItem] =
    useState<RecurringTransactionResponseDto | null>(null);

  const stats = useMemo(() => {
    if (!recurring) return { total: 0, active: 0, monthlyIncome: 0, monthlyExpense: 0 };
    const active = recurring.filter((r) => r.isActive);
    const monthlyIncome = active
      .filter((r) => r.type === "Income" && r.frequency === "Monthly")
      .reduce((sum, r) => sum + r.amount, 0);
    const monthlyExpense = active
      .filter((r) => r.type === "Expense" && r.frequency === "Monthly")
      .reduce((sum, r) => sum + r.amount, 0);
    return {
      total: recurring.length,
      active: active.length,
      monthlyIncome,
      monthlyExpense,
    };
  }, [recurring]);

  const handleEdit = (item: RecurringTransactionResponseDto) => {
    setEditingItem(item);
    setFormOpen(true);
  };

  const handleNew = () => {
    setEditingItem(null);
    setFormOpen(true);
  };

  const handleDelete = () => {
    if (deletingItem) {
      deleteRecurring.mutate(deletingItem.id, {
        onSuccess: () => setDeletingItem(null),
      });
    }
  };

  const handleToggle = (item: RecurringTransactionResponseDto) => {
    updateRecurring.mutate({
      id: item.id,
      data: {
        description: item.description,
        amount: item.amount,
        type: item.type,
        accountId: item.accountId,
        categoryId: item.categoryId,
        frequency: item.frequency,
        startDate: item.startDate,
        endDate: item.endDate,
        isActive: !item.isActive,
        notes: item.notes,
      },
    });
  };

  const fmt = (v: number) =>
    new Intl.NumberFormat("pt-BR", {
      style: "currency",
      currency: "BRL",
    }).format(v);

  return (
    <div className="space-y-6">
      <PageHeader
        title="Transacoes Recorrentes"
        description="Gerencie receitas e despesas que se repetem automaticamente."
      >
        <Button onClick={handleNew}>
          <Plus className="mr-2 h-4 w-4" />
          Nova Recorrente
        </Button>
      </PageHeader>

      {/* Stats */}
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard
          title="Total"
          value={String(stats.total)}
          icon={RefreshCw}
        />
        <StatCard
          title="Ativas"
          value={String(stats.active)}
          icon={Play}
        />
        <StatCard
          title="Receita Mensal"
          value={fmt(stats.monthlyIncome)}
          icon={Play}
          variant="income"
        />
        <StatCard
          title="Despesa Mensal"
          value={fmt(stats.monthlyExpense)}
          icon={Pause}
          variant="expense"
        />
      </div>

      {isLoading ? (
        <CardGridSkeleton />
      ) : !recurring || recurring.length === 0 ? (
        <EmptyState
          icon={RefreshCw}
          title="Nenhuma recorrente cadastrada"
          description="Crie transacoes recorrentes para automatizar lancamentos."
          actionLabel="Nova Recorrente"
          onAction={handleNew}
        />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {recurring.map((item) => (
            <RecurringCard
              key={item.id}
              recurring={item}
              onEdit={() => handleEdit(item)}
              onDelete={() => setDeletingItem(item)}
              onToggle={() => handleToggle(item)}
            />
          ))}
        </div>
      )}

      <RecurringForm
        open={formOpen}
        onOpenChange={(open) => {
          setFormOpen(open);
          if (!open) setEditingItem(null);
        }}
        editingRecurring={editingItem}
      />

      <ConfirmDialog
        open={!!deletingItem}
        onOpenChange={(open) => {
          if (!open) setDeletingItem(null);
        }}
        title="Excluir recorrente"
        description={`Tem certeza que deseja excluir "${deletingItem?.description}"? Lancamentos ja realizados nao serao afetados.`}
        onConfirm={handleDelete}
        confirmLabel="Excluir"
        variant="destructive"
      />
    </div>
  );
}
