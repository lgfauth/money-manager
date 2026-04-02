"use client";

import { format } from "date-fns";
import { ptBR } from "date-fns/locale";
import {
  Calendar,
  Clock,
  MoreHorizontal,
  Pause,
  Pencil,
  Play,
  Trash2,
} from "lucide-react";
import { cn } from "@/lib/utils";
import type { RecurringTransactionResponseDto } from "@/types/recurring";

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Badge } from "@/components/ui/badge";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";

const frequencyLabels: Record<string, string> = {
  Daily: "Diária",
  Weekly: "Semanal",
  Biweekly: "Quinzenal",
  Monthly: "Mensal",
  Quarterly: "Trimestral",
  Semiannual: "Semestral",
  Annual: "Anual",
};

const typeColors: Record<string, string> = {
  Income: "text-income",
  Expense: "text-expense",
};

interface RecurringCardProps {
  recurring: RecurringTransactionResponseDto;
  onEdit: () => void;
  onDelete: () => void;
  onToggle: () => void;
}

export function RecurringCard({
  recurring,
  onEdit,
  onDelete,
  onToggle,
}: RecurringCardProps) {
  return (
    <Card
      className={cn(
        "rounded-xl transition-shadow hover:shadow-md",
        !recurring.isActive && "opacity-60"
      )}
    >
      <CardHeader className="flex flex-row items-start justify-between pb-2">
        <div className="space-y-1">
          <CardTitle className="text-sm font-medium">
            {recurring.description}
          </CardTitle>
          <div className="flex items-center gap-2 text-xs text-muted-foreground">
            <Badge variant="secondary">
              {frequencyLabels[recurring.frequency] ?? recurring.frequency}
            </Badge>
            <Badge variant={recurring.isActive ? "default" : "outline"}>
              {recurring.isActive ? "Ativa" : "Pausada"}
            </Badge>
          </div>
        </div>
        <DropdownMenu>
          <DropdownMenuTrigger className="outline-none">
            <MoreHorizontal className="h-4 w-4 text-muted-foreground" />
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={onToggle}>
              {recurring.isActive ? (
                <>
                  <Pause className="mr-2 h-4 w-4" /> Pausar
                </>
              ) : (
                <>
                  <Play className="mr-2 h-4 w-4" /> Ativar
                </>
              )}
            </DropdownMenuItem>
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
      <CardContent className="space-y-2">
        <p className={cn("text-lg font-bold", typeColors[recurring.type])}>
          {recurring.type === "Income" ? "+" : "-"}
          {new Intl.NumberFormat("pt-BR", {
            style: "currency",
            currency: "BRL",
          }).format(recurring.amount)}
        </p>
        <div className="text-xs text-muted-foreground space-y-1">
          <p>
            {recurring.categoryName} · {recurring.accountName}
          </p>
          {recurring.nextOccurrence && (
            <p className="flex items-center gap-1">
              <Calendar className="h-3 w-3" />
              Próxima:{" "}
              {format(new Date(recurring.nextOccurrence), "dd/MM/yyyy", {
                locale: ptBR,
              })}
            </p>
          )}
          {recurring.lastProcessedDate && (
            <p className="flex items-center gap-1">
              <Clock className="h-3 w-3" />
              Ultima:{" "}
              {format(new Date(recurring.lastProcessedDate), "dd/MM/yyyy", {
                locale: ptBR,
              })}
            </p>
          )}
        </div>
      </CardContent>
    </Card>
  );
}
