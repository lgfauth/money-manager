"use client";

import { format } from "date-fns";
import { ptBR } from "date-fns/locale";
import {
  ArrowDownLeft,
  ArrowUpRight,
  TrendingUp,
  MoreHorizontal,
  Pencil,
  Trash2,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { TransactionType, type TransactionResponseDto } from "@/types/transaction";

import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from "@/components/ui/table";
import { Badge } from "@/components/ui/badge";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Card, CardContent } from "@/components/ui/card";

const typeConfig: Record<
  string,
  { label: string; icon: typeof ArrowUpRight; className: string }
> = {
  [TransactionType.Income]: {
    label: "Receita",
    icon: ArrowDownLeft,
    className: "text-income",
  },
  [TransactionType.Expense]: {
    label: "Despesa",
    icon: ArrowUpRight,
    className: "text-expense",
  },
  [TransactionType.Investment]: {
    label: "Investimento",
    icon: TrendingUp,
    className: "text-investment",
  },
};

interface TransactionTableProps {
  transactions: TransactionResponseDto[];
  onEdit: (transaction: TransactionResponseDto) => void;
  onDelete: (transaction: TransactionResponseDto) => void;
}

function MobileCard({
  transaction,
  onEdit,
  onDelete,
}: {
  transaction: TransactionResponseDto;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const config = typeConfig[transaction.type] ?? typeConfig[TransactionType.Expense];
  const Icon = config.icon;

  return (
    <Card className="rounded-[10px] hover:shadow-md transition-shadow">
      <CardContent className="flex items-center gap-3 p-3">
        <div className={cn("rounded-full p-2 bg-muted", config.className)}>
          <Icon className="h-4 w-4" />
        </div>
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium truncate">
            {transaction.description}
          </p>
          <p className="text-xs text-muted-foreground">
            {transaction.categoryName} · {transaction.accountName}
          </p>
        </div>
        <div className="text-right">
          <p className={cn("text-sm font-semibold font-heading", config.className)}>
            {transaction.type === TransactionType.Income ? "+" : "-"}
            {new Intl.NumberFormat("pt-BR", {
              style: "currency",
              currency: "BRL",
            }).format(transaction.amount)}
          </p>
          <p className="text-[10px] text-muted-foreground">
            {format(new Date(transaction.date), "dd/MM", { locale: ptBR })}
          </p>
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
      </CardContent>
    </Card>
  );
}

export function TransactionTable({
  transactions,
  onEdit,
  onDelete,
}: TransactionTableProps) {
  return (
    <>
      {/* Desktop table */}
      <div className="hidden md:block rounded-[10px] border overflow-hidden">
        <Table>
          <TableHeader>
            <TableRow>
              <TableHead>Data</TableHead>
              <TableHead>Descricao</TableHead>
              <TableHead>Categoria</TableHead>
              <TableHead>Conta</TableHead>
              <TableHead>Tipo</TableHead>
              <TableHead className="text-right">Valor</TableHead>
              <TableHead className="w-10" />
            </TableRow>
          </TableHeader>
          <TableBody>
            {transactions.map((tx) => {
              const config =
                typeConfig[tx.type] ?? typeConfig[TransactionType.Expense];
              return (
                <TableRow key={tx.id}>
                  <TableCell className="whitespace-nowrap">
                    {format(new Date(tx.date), "dd/MM/yyyy", { locale: ptBR })}
                  </TableCell>
                  <TableCell className="max-w-[200px] truncate">
                    {tx.description}
                  </TableCell>
                  <TableCell>{tx.categoryName}</TableCell>
                  <TableCell>{tx.accountName}</TableCell>
                  <TableCell>
                    <Badge variant="secondary" className={config.className}>
                      {config.label}
                    </Badge>
                  </TableCell>
                  <TableCell
                    className={cn("text-right font-semibold font-heading", config.className)}
                  >
                    {tx.type === TransactionType.Income ? "+" : "-"}
                    {new Intl.NumberFormat("pt-BR", {
                      style: "currency",
                      currency: "BRL",
                    }).format(tx.amount)}
                  </TableCell>
                  <TableCell>
                    <DropdownMenu>
                      <DropdownMenuTrigger className="outline-none">
                        <MoreHorizontal className="h-4 w-4 text-muted-foreground" />
                      </DropdownMenuTrigger>
                      <DropdownMenuContent align="end">
                        <DropdownMenuItem onClick={() => onEdit(tx)}>
                          <Pencil className="mr-2 h-4 w-4" />
                          Editar
                        </DropdownMenuItem>
                        <DropdownMenuItem
                          variant="destructive"
                          onClick={() => onDelete(tx)}
                        >
                          <Trash2 className="mr-2 h-4 w-4" />
                          Excluir
                        </DropdownMenuItem>
                      </DropdownMenuContent>
                    </DropdownMenu>
                  </TableCell>
                </TableRow>
              );
            })}
          </TableBody>
        </Table>
      </div>

      {/* Mobile cards */}
      <div className="md:hidden space-y-2">
        {transactions.map((tx) => (
          <MobileCard
            key={tx.id}
            transaction={tx}
            onEdit={() => onEdit(tx)}
            onDelete={() => onDelete(tx)}
          />
        ))}
      </div>
    </>
  );
}
