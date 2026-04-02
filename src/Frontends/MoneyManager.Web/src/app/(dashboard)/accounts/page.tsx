"use client";

import { useState } from "react";
import { Plus, Wallet } from "lucide-react";
import { useAccounts, useDeleteAccount } from "@/hooks/use-accounts";
import { AccountType, type AccountResponseDto } from "@/types/account";

import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState } from "@/components/shared/empty-state";
import { CardGridSkeleton } from "@/components/shared/loading-skeleton";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { AccountCard } from "@/components/accounts/account-card";
import { AccountForm } from "@/components/accounts/account-form";
import { InvoicePaymentModal } from "@/components/accounts/invoice-payment-modal";

type SortField = "name" | "balance" | "type";

const typeOrder: Record<string, number> = {
  [AccountType.Checking]: 0,
  [AccountType.Savings]: 1,
  [AccountType.Investment]: 2,
  [AccountType.CreditCard]: 3,
  [AccountType.Cash]: 4,
};

export default function AccountsPage() {
  const { data: accounts, isLoading } = useAccounts();
  const deleteAccount = useDeleteAccount();

  const [formOpen, setFormOpen] = useState(false);
  const [editingAccount, setEditingAccount] =
    useState<AccountResponseDto | null>(null);
  const [deletingAccount, setDeletingAccount] =
    useState<AccountResponseDto | null>(null);
  const [paymentAccount, setPaymentAccount] =
    useState<AccountResponseDto | null>(null);
  const [sortBy, setSortBy] = useState<SortField>("type");

  const handleEdit = (account: AccountResponseDto) => {
    setEditingAccount(account);
    setFormOpen(true);
  };

  const handleNew = () => {
    setEditingAccount(null);
    setFormOpen(true);
  };

  const handleDelete = () => {
    if (deletingAccount) {
      deleteAccount.mutate(deletingAccount.id, {
        onSuccess: () => setDeletingAccount(null),
      });
    }
  };

  const sortedAccounts = accounts
    ? [...accounts].sort((a, b) => {
        if (sortBy === "name") return a.name.localeCompare(b.name);
        if (sortBy === "balance") return b.balance - a.balance;
        return (typeOrder[a.type] ?? 99) - (typeOrder[b.type] ?? 99);
      })
    : [];

  return (
    <div className="space-y-6">
      <PageHeader title="Contas" description="Gerencie suas contas e cartões.">
        <Button onClick={handleNew}>
          <Plus className="mr-2 h-4 w-4" />
          Nova Conta
        </Button>
      </PageHeader>

      {/* Sort controls */}
      <div className="flex items-center gap-2">
        <span className="text-sm text-muted-foreground">Ordenar por:</span>
        {(
          [
            ["type", "Tipo"],
            ["name", "Nome"],
            ["balance", "Saldo"],
          ] as const
        ).map(([field, label]) => (
          <Button
            key={field}
            variant={sortBy === field ? "default" : "outline"}
            size="sm"
            onClick={() => setSortBy(field)}
          >
            {label}
          </Button>
        ))}
      </div>

      {isLoading ? (
        <CardGridSkeleton />
      ) : sortedAccounts.length === 0 ? (
        <EmptyState
          icon={Wallet}
          title="Nenhuma conta cadastrada"
          description="Crie sua primeira conta para começar a registrar transações."
          actionLabel="Nova Conta"
          onAction={handleNew}
        />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {sortedAccounts.map((account) => (
            <AccountCard
              key={account.id}
              account={account}
              onEdit={() => handleEdit(account)}
              onDelete={() => setDeletingAccount(account)}
            />
          ))}
        </div>
      )}

      <AccountForm
        open={formOpen}
        onOpenChange={(open) => {
          setFormOpen(open);
          if (!open) setEditingAccount(null);
        }}
        editingAccount={editingAccount}
      />

      <ConfirmDialog
        open={!!deletingAccount}
        onOpenChange={(open) => {
          if (!open) setDeletingAccount(null);
        }}
        title="Excluir conta"
        description={`Tem certeza que deseja excluir a conta "${deletingAccount?.name}"? Esta ação não pode ser desfeita.`}
        onConfirm={handleDelete}
        confirmLabel="Excluir"
        variant="destructive"
      />

      {paymentAccount && (
        <InvoicePaymentModal
          open={!!paymentAccount}
          onOpenChange={(open) => {
            if (!open) setPaymentAccount(null);
          }}
          creditCardAccount={paymentAccount}
        />
      )}
    </div>
  );
}
