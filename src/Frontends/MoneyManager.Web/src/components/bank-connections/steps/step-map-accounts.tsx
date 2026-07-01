"use client";

import { useState } from "react";
import { useConnectionAccounts } from "@/hooks/use-bank-connections";
import { useAccounts } from "@/hooks/use-accounts";
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
import type { AccountMappingDto, BankMcpAccountDto } from "@/types/bank-connection";

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
  const [mappings, setMappings] = useState<Record<string, string>>({});

  const mcpAccounts: BankMcpAccountDto[] = connectionData?.accounts ?? [];
  const isLoading = loadingAccounts || loadingMmAccounts;

  function handleMappingChange(externalAccountId: string, mmAccountId: string) {
    setMappings((prev) => ({ ...prev, [externalAccountId]: mmAccountId }));
  }

  function handleContinue() {
    const result: AccountMappingDto[] = mcpAccounts
      .filter((a) => mappings[a.externalAccountId])
      .map((a) => ({
        externalAccountId: a.externalAccountId,
        externalAccountName: a.name,
        externalAccountType: a.type,
        moneyManagerAccountId: mappings[a.externalAccountId],
      }));

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

      <div className="space-y-3">
        {mcpAccounts.map((account) => (
          <div
            key={account.externalAccountId}
            className="rounded-lg border p-3 space-y-2"
          >
            <div className="flex items-center justify-between">
              <div>
                <p className="text-sm font-medium">{account.name}</p>
                <p className="text-xs text-muted-foreground">
                  {account.type === "CREDIT"
                    ? "Cartão de crédito"
                    : "Conta bancária"}
                  {" · "}
                  {new Intl.NumberFormat("pt-BR", {
                    style: "currency",
                    currency: "BRL",
                  }).format(account.balance)}
                </p>
              </div>
              <Badge variant="outline" className="text-xs">
                {account.type}
              </Badge>
            </div>
            <Select
              value={mappings[account.externalAccountId] ?? ""}
              onValueChange={(value) =>
                handleMappingChange(account.externalAccountId, value)
              }
            >
              <SelectTrigger className="h-8 text-xs">
                <SelectValue placeholder="Selecionar conta no MoneyManager" />
              </SelectTrigger>
              <SelectContent>
                {mmAccounts.map((mmAccount) => (
                  <SelectItem
                    key={mmAccount.id}
                    value={mmAccount.id}
                    className="text-xs"
                  >
                    {mmAccount.name}
                  </SelectItem>
                ))}
              </SelectContent>
            </Select>
          </div>
        ))}
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
