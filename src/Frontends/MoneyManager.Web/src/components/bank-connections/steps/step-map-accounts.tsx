"use client";

import { useState } from "react";
import { useConnectionAccounts } from "@/hooks/use-bank-connections";
import { useAccounts } from "@/hooks/use-accounts";
import { useCreditCards } from "@/hooks/use-credit-cards";
import { Button } from "@/components/ui/button";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { Skeleton } from "@/components/ui/skeleton";
import { Badge } from "@/components/ui/badge";
import { CreditCard, Wallet } from "lucide-react";
import type { AccountMappingDto, BankMcpAccountDto } from "@/types/bank-connection";
import type { AccountResponseDto } from "@/types/account";
import type { CreditCardResponseDto } from "@/types/credit-card";

interface StepMapAccountsProps {
  connectionId: string;
  institutionName: string;
  onBack: () => void;
  onSuccess: (mappings: AccountMappingDto[]) => void;
}

export function StepMapAccounts({
  connectionId,
  institutionName,
  onBack,
  onSuccess,
}: StepMapAccountsProps) {
  const { data: connectionData, isLoading: loadingAccounts } =
    useConnectionAccounts(connectionId, true);
  const { data: mmAccounts = [], isLoading: loadingMmAccounts } = useAccounts();
  const { data: mmCreditCards = [], isLoading: loadingCreditCards } = useCreditCards();
  const [mappings, setMappings] = useState<Record<string, string>>({});

  const mcpAccounts: BankMcpAccountDto[] = connectionData?.accounts ?? [];
  const isLoading = loadingAccounts || loadingMmAccounts || loadingCreditCards;

  type MappingTarget = {
    id: string;
    name: string;
    subtitle: string;
    entityType: "Account" | "CreditCard";
  };

  const accountTargets: MappingTarget[] = mmAccounts.map(
    (account: AccountResponseDto) => ({
      id: account.id,
      name: account.name,
      subtitle: getMoneyManagerAccountTypeLabel(account.type),
      entityType: "Account",
    })
  );

  const creditCardTargets: MappingTarget[] = mmCreditCards.map(
    (card: CreditCardResponseDto) => ({
      id: card.id,
      name: card.name,
      subtitle: "Cartão de crédito",
      entityType: "CreditCard",
    })
  );

  function handleMappingChange(externalAccountId: string, mmAccountId: string) {
    setMappings((prev) => ({ ...prev, [externalAccountId]: mmAccountId }));
  }

  function handleContinue() {
    const result: AccountMappingDto[] = mcpAccounts
      .map((a) => {
        const selectedTargetId = mappings[a.accountId];
        if (!selectedTargetId) return null;

        const selectedTarget = [...accountTargets, ...creditCardTargets].find(
          (target) => target.id === selectedTargetId
        );

        if (!selectedTarget) return null;

        return {
          externalAccountId: a.accountId,
          externalAccountType: a.type,
          externalAccountSubtype: a.subtype,
          externalAccountNumber: a.number,
          bankName: a.displayName,
          moneyManagerAccountId: selectedTargetId,
          moneyManagerEntityType: selectedTarget.entityType,
        } satisfies AccountMappingDto;
      })
      .filter((value): value is AccountMappingDto => value !== null);

    if (result.length === 0) return;

    onSuccess(result);
  }

  const mappedCount = Object.values(mappings).filter(Boolean).length;

  if (isLoading) {
    return (
      <div className="space-y-3">
        {[1, 2].map((i) => (
          <Skeleton key={i} className="h-20 w-full rounded-lg" />
        ))}
      </div>
    );
  }

  return (
    <div className="space-y-4">
      <p className="text-sm text-muted-foreground">
        Associe cada conta do <strong>{institutionName}</strong> a uma conta já
        cadastrada no MoneyManager. Contas não associadas não serão
        sincronizadas.
      </p>

      <p className="text-xs text-muted-foreground rounded-md border border-muted px-3 py-2 bg-muted/40">
        Você pode vincular contas bancárias e também cartões de crédito já cadastrados.
      </p>

      <div className="space-y-3">
        {mcpAccounts.map((account) => {
          const Icon = getExternalAccountTypeIcon(account.type, account.subtype);
          const availableTargets =
            account.type === "CREDIT" || account.subtype === "CREDIT_CARD"
              ? creditCardTargets
              : accountTargets;

          return (
            <div
              key={account.accountId}
              className="rounded-lg border p-3 space-y-2"
            >
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2">
                <Icon className="h-4 w-4 text-muted-foreground" />
                <div>
                  <p className="text-sm font-medium">{account.displayName}</p>
                  <p className="text-xs text-muted-foreground">
                    {getExternalAccountTypeLabel(account.type, account.subtype)}
                    {account.number ? ` • ${account.number}` : ""}
                  </p>
                </div>
              </div>
              <Badge variant="outline" className="text-xs">
                {new Intl.NumberFormat("pt-BR", {
                  style: "currency",
                  currency: "BRL",
                }).format(account.balance)}
              </Badge>
            </div>
            <Select
              value={mappings[account.accountId] ?? ""}
              onValueChange={(value) =>
                handleMappingChange(account.accountId, value ?? "")
              }
            >
              <SelectTrigger className="h-8 text-xs">
                <SelectValue
                  placeholder={
                    account.type === "CREDIT"
                      ? "Selecionar cartão no MoneyManager"
                      : "Selecionar conta no MoneyManager"
                  }
                >
                  {(() => {
                    const selectedId = mappings[account.accountId];
                    if (!selectedId) return undefined;
                    const selectedTarget = [...accountTargets, ...creditCardTargets].find(
                      (t) => t.id === selectedId
                    );
                    if (!selectedTarget) return undefined;
                    return `${selectedTarget.name} (${selectedTarget.subtitle})`;
                  })()}
                </SelectValue>
              </SelectTrigger>
              <SelectContent>
                {availableTargets.map((target) => (
                  <SelectItem
                    key={target.id}
                    value={target.id}
                    className="text-xs"
                  >
                    {target.name} ({target.subtitle})
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
            {availableTargets.length === 0 && (
              <p className="text-xs text-muted-foreground">
                {account.type === "CREDIT"
                  ? "Cadastre um cartão de crédito no MoneyManager para vincular esta conta."
                  : "Cadastre uma conta no MoneyManager para vincular esta conta bancária."}
              </p>
            )}
            </div>
          );
        })}
      </div>

      <div className="flex gap-2 pt-2">
        <Button variant="outline" className="flex-1" onClick={onBack}>
          Voltar
        </Button>
        <Button
          className="flex-1"
          onClick={handleContinue}
          disabled={mappedCount === 0}
        >
          Continuar ({mappedCount} conta{mappedCount !== 1 ? "s" : ""})
        </Button>
      </div>
    </div>
  );
}

function getExternalAccountTypeLabel(type: string, subtype: string | null) {
  if (subtype === "CREDIT_CARD") return "Cartão de crédito";
  if (subtype === "SAVINGS_ACCOUNT") return "Poupança";
  if (type === "CREDIT") return "Cartão de crédito";
  return "Conta corrente";
}

function getExternalAccountTypeIcon(type: string, subtype: string | null) {
  if (type === "CREDIT" || subtype === "CREDIT_CARD") return CreditCard;
  return Wallet;
}

function getMoneyManagerAccountTypeLabel(type: AccountResponseDto["type"]) {
  switch (type) {
    case "Checking":
      return "Conta corrente";
    case "Savings":
      return "Poupança";
    case "Cash":
      return "Carteira";
    default:
      return "Conta";
  }
}
