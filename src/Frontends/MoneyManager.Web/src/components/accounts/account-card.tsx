"use client";

import { cn } from "@/lib/utils";
import { MoreHorizontal, Pencil, Trash2 } from "lucide-react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import type { AccountResponseDto } from "@/types/account";

const accountTypeLabels: Record<string, string> = {
  Checking: "Conta Corrente",
  Savings: "Poupança",
  Cash: "Dinheiro",
};

interface AccountCardProps {
  account: AccountResponseDto;
  onEdit: () => void;
  onDelete: () => void;
}

export function AccountCard({ account, onEdit, onDelete }: AccountCardProps) {
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
      </CardContent>
    </Card>
  );
}
