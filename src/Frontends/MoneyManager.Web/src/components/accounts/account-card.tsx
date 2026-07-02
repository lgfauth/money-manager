"use client";

import { cn } from "@/lib/utils";
import { MoreHorizontal, Pencil, Trash2 } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import Image from "next/image";
import { FinancialEntityCard } from "@/components/shared/financial-entity-card";
import { BankSyncStatus } from "@/components/shared/bank-sync-status";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";
import type { BankSyncInfo } from "@/hooks/use-bank-sync-status";
import type { AccountResponseDto } from "@/types/account";

const accountTypeLabels: Record<string, string> = {
  Checking: "Conta Corrente",
  Savings: "Poupança",
  Cash: "Dinheiro",
};

interface AccountCardProps {
  account: AccountResponseDto;
  syncInfo?: BankSyncInfo;
  onEdit: () => void;
  onDelete: () => void;
}

export function AccountCard({
  account,
  syncInfo,
  onEdit,
  onDelete,
}: AccountCardProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();
  const formattedBalance = formatMonetaryValue(account.balance, account.currency);

  return (
    <FinancialEntityCard
      indicator={
        syncInfo ? (
          syncInfo.bankLogo ? (
            <Image
              src={syncInfo.bankLogo}
              width={24}
              height={24}
              className="h-6 w-6 rounded-full object-contain shrink-0"
              alt=""
            />
          ) : (
            <span className="h-6 w-6 rounded-full bg-primary/20 flex items-center justify-center text-[11px] font-bold text-primary shrink-0">
              {syncInfo.bankName.charAt(0).toUpperCase()}
            </span>
          )
        ) : (
          <span
            className="h-3 w-3 rounded-full shrink-0"
            style={{ backgroundColor: account.color }}
          />
        )
      }
      name={account.name}
      accentColor={account.color}
      accentPosition="bottom"
      menuTrigger={
        <DropdownMenu>
          <DropdownMenuTrigger className="outline-none">
            <MoreHorizontal className="h-4 w-4 text-muted-foreground" />
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={onEdit}>
              <Pencil className="mr-2 h-4 w-4" />
              Editar
            </DropdownMenuItem>
            <DropdownMenuItem variant="destructive" onClick={onDelete}>
              <Trash2 className="mr-2 h-4 w-4" />
              Excluir
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      }
      syncBadge={syncInfo ? <BankSyncStatus syncInfo={syncInfo} /> : undefined}
    >
      <div>
        <p
          className={cn(
            "text-xl font-bold",
            account.balance >= 0 ? "text-income" : "text-expense"
          )}
        >
          {formattedBalance}
        </p>
        <Badge variant="secondary" className="mt-1 text-[10px]">
          {accountTypeLabels[account.type] ?? account.type}
        </Badge>
      </div>
    </FinancialEntityCard>
  );
}
