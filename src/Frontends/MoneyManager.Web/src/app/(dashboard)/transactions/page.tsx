"use client";

import { useState, useMemo, useEffect } from "react";
import { useSearchParams, useRouter } from "next/navigation";
import { Plus, ArrowLeft, ArrowRight, Receipt, CreditCard } from "lucide-react";
import { useTransactions, useDeleteTransaction } from "@/hooks/use-transactions";
import {
  useCreditCardTransactions,
  useCreditCards,
  useDeleteCreditCardTransaction,
} from "@/hooks/use-credit-cards";
import type {
  TransactionResponseDto,
  TransactionFilters,
} from "@/types/transaction";
import type { CreditCardTransactionResponseDto } from "@/types/credit-card";
import { DEFAULT_PAGE_SIZE } from "@/config/constants";

import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState } from "@/components/shared/empty-state";
import { TableSkeleton } from "@/components/shared/loading-skeleton";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { Tabs, TabsList, TabsTrigger, TabsContent } from "@/components/ui/tabs";
import { TransactionTable } from "@/components/transactions/transaction-table";
import { TransactionFilters as Filters } from "@/components/transactions/transaction-filters";
import { TransactionForm } from "@/components/transactions/transaction-form";
import { CreditCardTransactionForm } from "@/components/transactions/credit-card-transaction-form";
import { CreditCardTransactionTable } from "@/components/transactions/credit-card-transaction-table";

type TabKind = "bank" | "card";

export default function TransactionsPage() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const initialTab: TabKind =
    searchParams.get("tab") === "card" ? "card" : "bank";
  const [tab, setTab] = useState<TabKind>(initialTab);

  // Bank transactions
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

  const { data: bankData, isLoading: bankLoading } = useTransactions(filters);
  const deleteBankTx = useDeleteTransaction();

  // Card transactions
  const { data: cardTxs, isLoading: cardTxLoading } =
    useCreditCardTransactions();
  const { data: cards } = useCreditCards();
  const deleteCardTx = useDeleteCreditCardTransaction();

  const [bankFormOpen, setBankFormOpen] = useState(
    searchParams.get("new") === "true"
  );
  const [cardFormOpen, setCardFormOpen] = useState(false);

  useEffect(() => {
    if (searchParams.get("new") === "true") {
      setEditingTx(null);
      setBankFormOpen(true);
      router.replace("/transactions");
    }
  }, [searchParams, router]);

  const [editingTx, setEditingTx] = useState<TransactionResponseDto | null>(
    null
  );
  const [deletingBankTx, setDeletingBankTx] =
    useState<TransactionResponseDto | null>(null);
  const [deletingCardTx, setDeletingCardTx] =
    useState<CreditCardTransactionResponseDto | null>(null);

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
    setBankFormOpen(true);
  };

  const handleNew = () => {
    if (tab === "card") {
      setCardFormOpen(true);
    } else {
      setEditingTx(null);
      setBankFormOpen(true);
    }
  };

  const handleDeleteBank = () => {
    if (deletingBankTx) {
      deleteBankTx.mutate(deletingBankTx.id, {
        onSuccess: () => setDeletingBankTx(null),
      });
    }
  };

  const handleDeleteCard = () => {
    if (deletingCardTx) {
      deleteCardTx.mutate(deletingCardTx.id, {
        onSuccess: () => setDeletingCardTx(null),
      });
    }
  };

  const totalPages = bankData?.totalPages ?? 1;
  const currentPage = bankData?.page ?? 1;

  const sortedCardTxs = useMemo(() => {
    if (!cardTxs) return [];
    return [...cardTxs].sort((a, b) =>
      b.purchaseDate.localeCompare(a.purchaseDate)
    );
  }, [cardTxs]);

  return (
    <div className="space-y-6">
      <PageHeader
        title="Transações"
        description="Registre e acompanhe todas as suas movimentações."
      >
        <Button onClick={handleNew}>
          <Plus className="mr-2 h-4 w-4" />
          {tab === "card" ? "Nova Compra" : "Nova Transação"}
        </Button>
      </PageHeader>

      <Tabs value={tab} onValueChange={(v) => setTab(v as TabKind)}>
        <TabsList>
          <TabsTrigger value="bank">Bancárias</TabsTrigger>
          <TabsTrigger value="card">Cartão</TabsTrigger>
        </TabsList>

        <TabsContent value="bank" className="mt-4 space-y-4">
          <Filters
            filters={{
              type: filters.type,
              accountId: filters.accountId,
              startDate: filters.startDate,
              endDate: filters.endDate,
            }}
            onFiltersChange={(f) => setFilters({ ...f, page: 1 })}
          />

          {bankLoading ? (
            <TableSkeleton />
          ) : !bankData || bankData.items.length === 0 ? (
            <EmptyState
              icon={Receipt}
              title="Nenhuma transação encontrada"
              description="Registre sua primeira transação ou ajuste os filtros."
              actionLabel="Nova Transação"
              onAction={handleNew}
            />
          ) : (
            <>
              <TransactionTable
                transactions={bankData.items}
                onEdit={handleEdit}
                onDelete={(tx) => setDeletingBankTx(tx)}
              />

              {totalPages > 1 && (
                <div className="flex items-center justify-between">
                  <p className="text-sm text-muted-foreground">
                    {bankData.totalCount} transações · Página {currentPage} de{" "}
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
                      Próxima
                      <ArrowRight className="ml-1 h-4 w-4" />
                    </Button>
                  </div>
                </div>
              )}
            </>
          )}
        </TabsContent>

        <TabsContent value="card" className="mt-4 space-y-4">
          {cardTxLoading ? (
            <TableSkeleton />
          ) : sortedCardTxs.length === 0 ? (
            <EmptyState
              icon={CreditCard}
              title="Nenhuma compra registrada"
              description="Registre sua primeira compra com cartão de crédito."
              actionLabel="Nova Compra"
              onAction={handleNew}
            />
          ) : (
            <CreditCardTransactionTable
              transactions={sortedCardTxs}
              cards={cards}
              onDelete={(tx) => setDeletingCardTx(tx)}
            />
          )}
        </TabsContent>
      </Tabs>

      <TransactionForm
        open={bankFormOpen}
        onOpenChange={(open) => {
          setBankFormOpen(open);
          if (!open) setEditingTx(null);
        }}
        editingTransaction={editingTx}
      />

      <CreditCardTransactionForm
        open={cardFormOpen}
        onOpenChange={setCardFormOpen}
      />

      <ConfirmDialog
        open={!!deletingBankTx}
        onOpenChange={(open) => {
          if (!open) setDeletingBankTx(null);
        }}
        title="Excluir transação"
        description={`Tem certeza que deseja excluir "${deletingBankTx?.description}"? Esta ação não pode ser desfeita.`}
        onConfirm={handleDeleteBank}
        confirmLabel="Excluir"
        variant="destructive"
      />

      <ConfirmDialog
        open={!!deletingCardTx}
        onOpenChange={(open) => {
          if (!open) setDeletingCardTx(null);
        }}
        title="Excluir compra do cartão"
        description={
          deletingCardTx?.totalInstallments && deletingCardTx.totalInstallments > 1
            ? `Tem certeza? Todas as ${deletingCardTx.totalInstallments} parcelas desta compra serão removidas. Só é possível excluir compras em faturas em aberto.`
            : `Tem certeza que deseja excluir "${deletingCardTx?.description}"? Só é possível excluir compras em faturas em aberto.`
        }
        onConfirm={handleDeleteCard}
        confirmLabel="Excluir"
        variant="destructive"
      />
    </div>
  );
}
