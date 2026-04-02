"use client";

import { cn } from "@/lib/utils";
import { CreditCard, MoreHorizontal, Pencil, Trash2 } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import { Progress } from "@/components/ui/progress";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import type { AccountResponseDto } from "@/types/account";
import { AccountType } from "@/types/account";
import Link from "next/link";

const accountTypeLabels: Record<string, string> = {
  Checking: "Conta Corrente",
  Savings: "Poupança",
  Cash: "Dinheiro",
  CreditCard: "Cartão de Crédito",
};

interface AccountCardProps {
  account: AccountResponseDto;
  onEdit: () => void;
  onDelete: () => void;
}

export function AccountCard({ account, onEdit, onDelete }: AccountCardProps) {
  const isCreditCard = account.type === AccountType.CreditCard;
  const usedAmount = isCreditCard ? account.committedCredit ?? Math.abs(account.balance) : 0;
  const availableAmount = isCreditCard
    ? account.availableCredit ?? Math.max((account.creditLimit ?? 0) - usedAmount, 0)
    : 0;
  const cardDebt = isCreditCard ? Math.abs(account.balance) : 0;
  const usedPercent =
    isCreditCard && account.creditLimit
      ? (usedAmount / account.creditLimit) * 100
      : 0;

  const formattedBalance = new Intl.NumberFormat("pt-BR", {
    style: "currency",
    currency: account.currency,
  }).format(account.balance);

  return (
    <Card className="rounded-xl hover:shadow-md transition-shadow">
      <CardHeader className="flex flex-row items-start justify-between pb-2">
        <div className="flex items-center gap-2">
          <div
            className="h-3 w-3 rounded-full"
            style={{ backgroundColor: account.color }}
          />
          <CardTitle className="text-sm font-medium">{account.name}</CardTitle>
        </div>
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
      </CardHeader>
      <CardContent className="space-y-3">
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

        {isCreditCard && account.creditLimit && (
          <div className="space-y-1.5">
            <div className="flex items-center justify-between text-xs text-muted-foreground">
              <span className="flex items-center gap-1">
                <CreditCard className="h-3 w-3" />
                Limite comprometido
              </span>
              <span>{usedPercent.toFixed(0)}%</span>
            </div>
            <Progress
              value={Math.min(usedPercent, 100)}
              className="h-2"
            />
            <div className="flex justify-between text-xs text-muted-foreground">
              <span>
                {new Intl.NumberFormat("pt-BR", {
                  style: "currency",
                  currency: account.currency,
                }).format(usedAmount)}
              </span>
              <span>
                {new Intl.NumberFormat("pt-BR", {
                  style: "currency",
                  currency: account.currency,
                }).format(account.creditLimit)}
              </span>
            </div>
            <div className="grid grid-cols-2 gap-2 rounded-lg bg-muted/40 p-2 text-[11px] text-muted-foreground">
              <div>
                <p>Débito do cartão</p>
                <p className="mt-0.5 font-medium text-foreground">
                  {new Intl.NumberFormat("pt-BR", {
                    style: "currency",
                    currency: account.currency,
                  }).format(cardDebt)}
                </p>
              </div>
              <div>
                <p>Disponível</p>
                <p className="mt-0.5 font-medium text-income">
                  {new Intl.NumberFormat("pt-BR", {
                    style: "currency",
                    currency: account.currency,
                  }).format(availableAmount)}
                </p>
              </div>
            </div>
            <Link
              href={`/credit-cards/${account.id}`}
              className="block text-xs text-primary hover:underline"
            >
              Ver faturas →
            </Link>
          </div>
        )}
      </CardContent>
    </Card>
  );
}
