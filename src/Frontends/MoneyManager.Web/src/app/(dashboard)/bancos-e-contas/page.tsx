"use client";

import { useState } from "react";
import { formatDistanceToNow } from "date-fns";
import { ptBR } from "date-fns/locale";
import {
  Building2,
  CreditCard,
  Info,
  Link2Off,
  Plus,
  RefreshCw,
  Unplug,
  Wallet,
} from "lucide-react";

import { BankConnectionBanner } from "@/components/bank-connections/bank-connection-banner";
import { BankSetupModal } from "@/components/bank-connections/bank-setup-modal";
import { AccountCard } from "@/components/accounts/account-card";
import { AccountForm } from "@/components/accounts/account-form";
import { CreditCardCard } from "@/components/credit-cards/credit-card-card";
import { CreditCardForm } from "@/components/credit-cards/credit-card-form";
import { AddFinancialEntityButton } from "@/components/bancos-e-contas/add-financial-entity-button";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { EmptyState } from "@/components/shared/empty-state";
import { useIsPremium } from "@/hooks/use-subscription";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";
import { useAccounts, useDeleteAccount } from "@/hooks/use-accounts";
import {
  useAvailableConnections,
  useBankConnections,
  useDisconnectBank,
  useSyncBank,
  useUnlinkBankAccount,
} from "@/hooks/use-bank-connections";
import { useCreditCards, useDeleteCreditCard } from "@/hooks/use-credit-cards";
import { useBankSyncStatus } from "@/hooks/use-bank-sync-status";
import { AccountType, type AccountResponseDto } from "@/types/account";
import type { CreditCardResponseDto } from "@/types/credit-card";
import type { BankConnectionDto } from "@/types/bank-connection";

const typeOrder: Record<string, number> = {
  [AccountType.Checking]: 0,
  [AccountType.Savings]: 1,
  [AccountType.Cash]: 2,
};

export default function BancosEContasPage() {
  const isPremium = useIsPremium();
  const { data: accounts = [], isLoading: loadingAccounts } = useAccounts();
  const { data: creditCards = [], isLoading: loadingCards } = useCreditCards();
  const { data: connections = [], isLoading: loadingConnections } =
    useBankConnections();
  const { data: availableData } = useAvailableConnections(isPremium);
  const syncBank = useSyncBank();
  const disconnectBank = useDisconnectBank();
  const unlinkBankAccount = useUnlinkBankAccount();
  const syncStatus = useBankSyncStatus(); // único hook de sync — passado como prop para os cards

  const deleteAccount = useDeleteAccount();
  const deleteCard = useDeleteCreditCard();

  const [setupOpen, setSetupOpen] = useState(false);
  const [disconnectConnection, setDisconnectConnection] =
    useState<BankConnectionDto | null>(null);
  const [unlinkTarget, setUnlinkTarget] = useState<{
    accountId: string;
    displayName: string;
    entityType: "conta" | "cartão";
  } | null>(null);
  const [showUnlinkRecurrenceWarning, setShowUnlinkRecurrenceWarning] =
    useState(false);
  const [syncingId, setSyncingId] = useState<string | null>(null);
  const [editingAccount, setEditingAccount] =
    useState<AccountResponseDto | null>(null);
  const [deletingAccount, setDeletingAccount] =
    useState<AccountResponseDto | null>(null);
  const [editingCard, setEditingCard] =
    useState<CreditCardResponseDto | null>(null);
  const [deletingCard, setDeletingCard] =
    useState<CreditCardResponseDto | null>(null);

  const sortedAccounts = [...accounts].sort(
    (a, b) =>
      (typeOrder[a.type] ?? 99) - (typeOrder[b.type] ?? 99) ||
      a.name.localeCompare(b.name)
  );
  const sortedCards = [...creditCards].sort((a, b) =>
    a.name.localeCompare(b.name)
  );

  function handleSync(connectionId: string) {
    setSyncingId(connectionId);
    syncBank.mutate(connectionId, {
      onSettled: () => setSyncingId(null),
    });
  }

  function getConnectionLinkedEntities(connection: BankConnectionDto) {
    return connection.selectedAccounts
      .filter((selected) => !!selected.moneyManagerAccountId)
      .map((selected) => {
        const entityId = selected.moneyManagerAccountId!;
        const isCard = selected.moneyManagerEntityType === "CreditCard";

        if (isCard) {
          const card = creditCards.find((c) => c.id === entityId);
          return {
            id: entityId,
            label: card?.name || `Cartão final ${selected.number || "-"}`,
            type: "Cartão",
          };
        }

        const account = accounts.find((a) => a.id === entityId);
        return {
          id: entityId,
          label: account?.name || `Conta ${selected.number || "-"}`,
          type: "Conta",
        };
      });
  }

  return (
    <div className="space-y-8">
      {/* Header da página */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Bancos e Contas</h1>
          <p className="text-sm text-muted-foreground mt-1">
            Gerencie suas contas bancárias e cartões de crédito.
          </p>
        </div>
        <AddFinancialEntityButton />
      </div>

      {/* Seção Contas */}
      <section>
        <h2 className="text-base font-semibold mb-3">Contas</h2>
        {loadingAccounts ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <Skeleton className="h-28 rounded-xl" />
            <Skeleton className="h-28 rounded-xl" />
          </div>
        ) : sortedAccounts.length === 0 ? (
          <EmptyState
            icon={Wallet}
            title="Nenhuma conta cadastrada"
            description="Crie sua primeira conta para começar a registrar transações."
          />
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {sortedAccounts.map((account) => (
              <AccountCard
                key={account.id}
                account={account}
                syncInfo={syncStatus[account.id]}
                onEdit={() => setEditingAccount(account)}
                onDelete={() => setDeletingAccount(account)}
                onUnlink={
                  syncStatus[account.id]
                    ? () =>
                        setUnlinkTarget({
                          accountId: account.id,
                          displayName: account.name,
                          entityType: "conta",
                        })
                    : undefined
                }
              />
            ))}
          </div>
        )}
      </section>

      {/* Seção Cartões de Crédito */}
      <section>
        <h2 className="text-base font-semibold mb-3">Cartões de Crédito</h2>
        {loadingCards ? (
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            <Skeleton className="h-40 rounded-xl" />
          </div>
        ) : sortedCards.length === 0 ? (
          <EmptyState
            icon={CreditCard}
            title="Nenhum cartão cadastrado"
            description="Cadastre seu primeiro cartão para registrar compras e acompanhar faturas."
          />
        ) : (
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
            {sortedCards.map((card) => (
              <CreditCardCard
                key={card.id}
                card={card}
                syncInfo={syncStatus[card.id]}
                onEdit={() => setEditingCard(card)}
                onDelete={() => setDeletingCard(card)}
                onUnlink={
                  syncStatus[card.id]
                    ? () =>
                        setUnlinkTarget({
                          accountId: card.id,
                          displayName: card.name,
                          entityType: "cartão",
                        })
                    : undefined
                }
              />
            ))}
          </div>
        )}
      </section>

      {/* Banner de conexão bancária — só para usuários premium */}
      <BankConnectionBanner />

      {/* Resumo detalhado de bancos conectados */}
      {isPremium && (
        <section className="space-y-3">
          <div className="flex items-center justify-between gap-3">
            <h2 className="text-base font-semibold">Bancos conectados</h2>
            <Button size="sm" onClick={() => setSetupOpen(true)}>
              <Plus className="h-4 w-4 mr-1" /> Adicionar banco
            </Button>
          </div>

          <div className="flex items-start gap-3 rounded-lg border border-amber-500/20 bg-amber-500/5 px-4 py-3 text-sm text-muted-foreground">
            <span className="mt-0.5 text-amber-500">⚠</span>
            <p>
              A sincronização bancária usa o{" "}
              <a
                href="https://banco.mcp.ai"
                target="_blank"
                rel="noopener noreferrer"
                className="text-primary underline underline-offset-2 hover:opacity-80"
              >
                Banco MCP
              </a>
              . O plano gratuito permite 10 sincronizações por dia e não inclui
              cartão de crédito. Para mais bancos, cartão e sincronizações
              ilimitadas,{" "}
              <a
                href="https://banco.mcp.ai/#pricing"
                target="_blank"
                rel="noopener noreferrer"
                className="text-primary underline underline-offset-2 hover:opacity-80"
              >
                veja os planos disponíveis →
              </a>
            </p>
          </div>

          {availableData?.apiKeyExpired && (
            <div className="flex items-start gap-3 rounded-lg border border-destructive/30 bg-destructive/5 px-4 py-3 text-sm">
              <span className="mt-0.5 text-destructive">⚠</span>
              <div className="flex-1">
                <p className="font-medium text-destructive">
                  API key do Banco MCP expirada
                </p>
                <p className="mt-0.5 text-muted-foreground">
                  Sua chave de integração foi revogada ou expirou. A sincronização automática está pausada.
                </p>
              </div>
              <Button size="sm" variant="destructive" onClick={() => setSetupOpen(true)}>
                Atualizar key
              </Button>
            </div>
          )}

          {loadingConnections ? (
            <div className="space-y-3">
              {[1, 2].map((i) => (
                <Skeleton key={i} className="h-24 w-full rounded-xl" />
              ))}
            </div>
          ) : connections.length === 0 ? (
            <div className="rounded-xl border p-6 text-center space-y-3">
              <Building2 className="h-10 w-10 text-muted-foreground mx-auto" />
              <p className="text-muted-foreground text-sm">
                Nenhum banco conectado ainda.
              </p>
              <Button onClick={() => setSetupOpen(true)}>
                Conectar primeiro banco
              </Button>
            </div>
          ) : (
            <div className="space-y-3">
              {connections.map((connection) => (
                <div
                  key={connection.id}
                  className="rounded-xl border p-4 flex items-center justify-between gap-4"
                >
                  <div className="flex items-center gap-3">
                    <div className="h-10 w-10 rounded-lg bg-muted flex items-center justify-center">
                      <Building2 className="h-5 w-5 text-muted-foreground" />
                    </div>
                    <div>
                      <p className="font-medium text-sm">{connection.institutionName}</p>
                      <p className="text-xs text-muted-foreground">
                        {connection.selectedAccounts.length} conta
                        {connection.selectedAccounts.length !== 1 ? "s" : ""}{" "}
                        sincronizada
                        {connection.selectedAccounts.length !== 1 ? "s" : ""}
                        {connection.lastSyncAt && (
                          <>
                            {" · "}
                            Atualizado{" "}
                            {formatDistanceToNow(new Date(connection.lastSyncAt), {
                              addSuffix: true,
                              locale: ptBR,
                            })}
                          </>
                        )}
                      </p>
                    </div>
                  </div>
                  <div className="flex items-center gap-2 shrink-0">
                    <Badge
                      variant={
                        connection.status === "Connected"
                          ? "default"
                          : "destructive"
                      }
                      className="text-xs hidden sm:inline-flex"
                    >
                      {connection.status === "Connected" ? "Ativo" : connection.status}
                    </Badge>
                    <Button
                      variant="outline"
                      size="sm"
                      disabled={syncingId === connection.id}
                      onClick={() => handleSync(connection.id)}
                    >
                      <RefreshCw
                        className={`h-3.5 w-3.5 ${syncingId === connection.id ? "animate-spin" : ""}`}
                      />
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      className="text-destructive hover:text-destructive"
                      onClick={() => setDisconnectConnection(connection)}
                    >
                      <Unplug className="h-3.5 w-3.5" />
                    </Button>
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      )}

      {/* Edição — reutiliza os Sheets existentes */}
      <AccountForm
        open={!!editingAccount}
        onOpenChange={(open) => {
          if (!open) setEditingAccount(null);
        }}
        editingAccount={editingAccount}
      />
      <CreditCardForm
        open={!!editingCard}
        onOpenChange={(open) => {
          if (!open) setEditingCard(null);
        }}
        editingCard={editingCard}
      />

      <ConfirmDialog
        open={!!deletingAccount}
        onOpenChange={(open) => {
          if (!open) setDeletingAccount(null);
        }}
        title="Excluir conta"
        description={`Tem certeza que deseja excluir a conta "${deletingAccount?.name}"? Esta ação não pode ser desfeita.`}
        onConfirm={() => {
          if (deletingAccount) {
            deleteAccount.mutate(deletingAccount.id, {
              onSuccess: () => setDeletingAccount(null),
            });
          }
        }}
        confirmLabel="Excluir"
        variant="destructive"
      />
      <ConfirmDialog
        open={!!deletingCard}
        onOpenChange={(open) => {
          if (!open) setDeletingCard(null);
        }}
        title="Excluir cartão"
        description={`Tem certeza que deseja excluir o cartão "${deletingCard?.name}"? As faturas e transações vinculadas também serão removidas.`}
        onConfirm={() => {
          if (deletingCard) {
            deleteCard.mutate(deletingCard.id, {
              onSuccess: () => setDeletingCard(null),
            });
          }
        }}
        confirmLabel="Excluir"
        variant="destructive"
      />

      <BankSetupModal open={setupOpen} onOpenChange={setSetupOpen} />

      <AlertDialog
        open={!!unlinkTarget}
        onOpenChange={(open) => {
          if (!open) {
            setUnlinkTarget(null);
            setShowUnlinkRecurrenceWarning(false);
          }
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>
              {showUnlinkRecurrenceWarning
                ? "Desvínculo concluído com aviso"
                : `Desvincular ${unlinkTarget?.entityType ?? "item"}?`}
            </AlertDialogTitle>
            <AlertDialogDescription>
              {showUnlinkRecurrenceWarning
                ? `A ${unlinkTarget?.entityType ?? "entidade"} "${unlinkTarget?.displayName}" foi desvinculada. Recorrências vinculadas que estavam desativadas permanecem desativadas e precisam ser reativadas manualmente.`
                : `A ${unlinkTarget?.entityType ?? "entidade"} "${unlinkTarget?.displayName}" continuará existindo e editável manualmente, mas sem sincronização automática com o banco.`}
            </AlertDialogDescription>
          </AlertDialogHeader>

          {showUnlinkRecurrenceWarning && (
            <div className="rounded-lg border border-amber-500/30 bg-amber-500/10 px-3 py-2 text-sm text-amber-700">
              Recorrências associadas foram desativadas no momento do vínculo e não serão reativadas automaticamente.
            </div>
          )}

          <AlertDialogFooter>
            {showUnlinkRecurrenceWarning ? (
              <AlertDialogAction
                className="bg-amber-500 hover:bg-amber-500/90 text-black"
                onClick={() => {
                  setUnlinkTarget(null);
                  setShowUnlinkRecurrenceWarning(false);
                }}
              >
                Entendi
              </AlertDialogAction>
            ) : (
              <>
                <AlertDialogCancel disabled={unlinkBankAccount.isPending}>
                  Cancelar
                </AlertDialogCancel>
                <AlertDialogAction
                  className="bg-amber-500 hover:bg-amber-500/90 text-black"
                  disabled={unlinkBankAccount.isPending}
                  onClick={() => {
                    if (!unlinkTarget) return;

                    unlinkBankAccount.mutate(unlinkTarget.accountId, {
                      onSuccess: (response) => {
                        if (response.hasDeactivatedRecurrences) {
                          setShowUnlinkRecurrenceWarning(true);
                          return;
                        }

                        setUnlinkTarget(null);
                      },
                    });
                  }}
                >
                  {unlinkBankAccount.isPending ? "Desvinculando..." : "Desvincular"}
                </AlertDialogAction>
              </>
            )}
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>

      <AlertDialog
        open={!!disconnectConnection}
        onOpenChange={(open) => {
          if (!open) setDisconnectConnection(null);
        }}
      >
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Desconectar banco?</AlertDialogTitle>
            <AlertDialogDescription>
              O banco {disconnectConnection?.institutionName ?? "selecionado"} será desconectado e todas as contas/cartões abaixo serão desvinculados.
              As entidades continuarão existindo para edição manual, mas sem sincronização automática.
            </AlertDialogDescription>
          </AlertDialogHeader>

          {(disconnectConnection?.selectedAccounts?.length ?? 0) > 0 && (
            <div className="space-y-2 rounded-lg border border-destructive/20 bg-destructive/5 px-3 py-2">
              <p className="flex items-center gap-2 text-sm font-medium text-destructive">
                <Info className="h-4 w-4" />
                Itens que serão desvinculados
              </p>
              <ul className="space-y-1 text-sm text-muted-foreground">
                {disconnectConnection &&
                  getConnectionLinkedEntities(disconnectConnection).map((entity) => (
                    <li key={entity.id} className="flex items-center gap-2">
                      <Link2Off className="h-3.5 w-3.5 text-destructive" />
                      <span>{entity.type}: {entity.label}</span>
                    </li>
                  ))}
              </ul>
            </div>
          )}

          <AlertDialogFooter>
            <AlertDialogCancel disabled={disconnectBank.isPending}>Cancelar</AlertDialogCancel>
            <AlertDialogAction
              className="bg-destructive hover:bg-destructive/90"
              disabled={disconnectBank.isPending}
              onClick={() => {
                if (disconnectConnection) {
                  disconnectBank.mutate(
                    disconnectConnection.itemId ?? disconnectConnection.id,
                    {
                    onSuccess: () => setDisconnectConnection(null),
                    }
                  );
                }
              }}
            >
              {disconnectBank.isPending ? "Desconectando..." : "Desconectar"}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
