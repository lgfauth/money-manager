"use client";

import { useState, useMemo, useEffect } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { Plus, ArrowLeft, ArrowRight, Receipt } from "lucide-react";
import { useTransactions } from "@/hooks/use-transactions";
import { useDeleteTransaction } from "@/hooks/use-transactions";
import type { TransactionResponseDto, TransactionFilters } from "@/types/transaction";
import { DEFAULT_PAGE_SIZE } from "@/config/constants";

import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState } from "@/components/shared/empty-state";
import { TableSkeleton } from "@/components/shared/loading-skeleton";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { TransactionTable } from "@/components/transactions/transaction-table";
import { TransactionFilters as Filters } from "@/components/transactions/transaction-filters";
import { TransactionForm } from "@/components/transactions/transaction-form";

export default function TransactionsPage() {
  const router = useRouter();
  const searchParams = useSearchParams();

  // Read filters from URL
  const filters: TransactionFilters = useMemo(
    () => ({
      page: Number(searchParams.get("page")) || 1,
      pageSize: Number(searchParams.get("pageSize")) || DEFAULT_PAGE_SIZE,
      sortBy: searchParams.get("sortBy") || "date",
      startDate: searchParams.get("startDate") ?? undefined,
      endDate: searchParams.get("endDate") ?? undefined,
      type: searchParams.get("type") ?? undefined,
      accountId: searchParams.get("accountId") ?? undefined,
    }),
    [searchParams]
  );

  const { data, isLoading } = useTransactions(filters);
  const deleteTransaction = useDeleteTransaction();

  const [formOpen, setFormOpen] = useState(
    searchParams.get("new") === "true"
  );

  // Re-open form when navigating to ?new=true (e.g. from mobile FAB)
  useEffect(() => {
    if (searchParams.get("new") === "true") {
      setEditingTx(null);
      setFormOpen(true);
      // Clean the URL param so it doesn't persist
      router.replace("/transactions");
    }
  }, [searchParams, router]);

  const [editingTx, setEditingTx] =
    useState<TransactionResponseDto | null>(null);
  const [deletingTx, setDeletingTx] =
    useState<TransactionResponseDto | null>(null);

  // Update URL params
  const setFilters = (newFilters: Partial<TransactionFilters>) => {
    const merged = { ...filters, ...newFilters };
    const params = new URLSearchParams();
    if (merged.page > 1) params.set("page", String(merged.page));
    if (merged.sortBy !== "date") params.set("sortBy", merged.sortBy);
    if (merged.startDate) params.set("startDate", merged.startDate);
    if (merged.endDate) params.set("endDate", merged.endDate);
    if (merged.type) params.set("type", merged.type);
    if (merged.accountId) params.set("accountId", merged.accountId);
    router.replace(`/transactions?${params.toString()}`);
  };

  const handleEdit = (tx: TransactionResponseDto) => {
    setEditingTx(tx);
    setFormOpen(true);
  };

  const handleNew = () => {
    setEditingTx(null);
    setFormOpen(true);
  };

  const handleDelete = () => {
    if (deletingTx) {
      deleteTransaction.mutate(deletingTx.id, {
        onSuccess: () => setDeletingTx(null),
      });
    }
  };

  const totalPages = data?.totalPages ?? 1;
  const currentPage = data?.page ?? 1;

  return (
    <div className="space-y-6">
      <PageHeader
        title="Transacoes"
        description="Registre e acompanhe todas as suas movimentacoes."
      >
        <Button onClick={handleNew}>
          <Plus className="mr-2 h-4 w-4" />
          Nova Transacao
        </Button>
      </PageHeader>

      <Filters
        filters={{
          type: filters.type,
          accountId: filters.accountId,
          startDate: filters.startDate,
          endDate: filters.endDate,
        }}
        onFiltersChange={(f) =>
          setFilters({ ...f, page: 1 })
        }
      />

      {isLoading ? (
        <TableSkeleton />
      ) : !data || data.items.length === 0 ? (
        <EmptyState
          icon={Receipt}
          title="Nenhuma transacao encontrada"
          description="Registre sua primeira transacao ou ajuste os filtros."
          actionLabel="Nova Transacao"
          onAction={handleNew}
        />
      ) : (
        <>
          <TransactionTable
            transactions={data.items}
            onEdit={handleEdit}
            onDelete={(tx) => setDeletingTx(tx)}
          />

          {/* Pagination */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between">
              <p className="text-sm text-muted-foreground">
                {data.totalCount} transacoes · Pagina {currentPage} de{" "}
                {totalPages}
              </p>
              <div className="flex gap-2">
                <Button
                  variant="outline"
                  size="sm"
                  disabled={currentPage <= 1}
                  onClick={() => setFilters({ page: currentPage - 1 })}
                >
                  <ArrowLeft className="mr-1 h-4 w-4" />
                  Anterior
                </Button>
                <Button
                  variant="outline"
                  size="sm"
                  disabled={currentPage >= totalPages}
                  onClick={() => setFilters({ page: currentPage + 1 })}
                >
                  Proxima
                  <ArrowRight className="ml-1 h-4 w-4" />
                </Button>
              </div>
            </div>
          )}
        </>
      )}

      <TransactionForm
        open={formOpen}
        onOpenChange={(open) => {
          setFormOpen(open);
          if (!open) setEditingTx(null);
        }}
        editingTransaction={editingTx}
      />

      <ConfirmDialog
        open={!!deletingTx}
        onOpenChange={(open) => {
          if (!open) setDeletingTx(null);
        }}
        title="Excluir transacao"
        description={`Tem certeza que deseja excluir "${deletingTx?.description}"? Esta acao nao pode ser desfeita.`}
        onConfirm={handleDelete}
        confirmLabel="Excluir"
        variant="destructive"
      />
    </div>
  );
}
