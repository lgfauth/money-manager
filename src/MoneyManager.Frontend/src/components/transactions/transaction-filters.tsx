"use client";

import { useAccounts } from "@/hooks/use-accounts";
import { TransactionType } from "@/types/transaction";
import { X } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

interface FilterValues {
  type?: string;
  accountId?: string;
  startDate?: string;
  endDate?: string;
}

interface TransactionFiltersProps {
  filters: FilterValues;
  onFiltersChange: (filters: FilterValues) => void;
}

export function TransactionFilters({
  filters,
  onFiltersChange,
}: TransactionFiltersProps) {
  const { data: accounts } = useAccounts();

  const hasActiveFilters =
    filters.type || filters.accountId || filters.startDate || filters.endDate;

  const clearFilters = () => {
    onFiltersChange({ type: undefined, accountId: undefined, startDate: undefined, endDate: undefined });
  };

  return (
    <div className="flex flex-wrap items-end gap-3">
      <div className="space-y-1">
        <label className="text-xs text-muted-foreground">Tipo</label>
        <Select
          value={filters.type ?? ""}
          onValueChange={(v) =>
            onFiltersChange({ ...filters, type: v || undefined })
          }
        >
          <SelectTrigger className="w-[140px]">
            <SelectValue placeholder="Todos" />
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="">Todos</SelectItem>
            <SelectItem value={TransactionType.Income}>Receita</SelectItem>
            <SelectItem value={TransactionType.Expense}>Despesa</SelectItem>
            <SelectItem value={TransactionType.Investment}>
              Investimento
            </SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="space-y-1">
        <label className="text-xs text-muted-foreground">Conta</label>
        <Select
          value={filters.accountId ?? ""}
          onValueChange={(v) =>
            onFiltersChange({ ...filters, accountId: v || undefined })
          }
        >
          <SelectTrigger className="w-[160px]">
            <SelectValue placeholder="Todas">
              {filters.accountId
                ? accounts?.find((a) => a.id === filters.accountId)?.name
                : undefined}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="">Todas</SelectItem>
            {accounts?.map((acc) => (
              <SelectItem key={acc.id} value={acc.id}>
                {acc.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="space-y-1">
        <label className="text-xs text-muted-foreground">De</label>
        <Input
          type="date"
          value={filters.startDate ?? ""}
          onChange={(e) =>
            onFiltersChange({
              ...filters,
              startDate: e.target.value || undefined,
            })
          }
          className="w-[150px]"
        />
      </div>

      <div className="space-y-1">
        <label className="text-xs text-muted-foreground">Ate</label>
        <Input
          type="date"
          value={filters.endDate ?? ""}
          onChange={(e) =>
            onFiltersChange({
              ...filters,
              endDate: e.target.value || undefined,
            })
          }
          className="w-[150px]"
        />
      </div>

      {hasActiveFilters && (
        <Button variant="ghost" size="sm" onClick={clearFilters}>
          <X className="mr-1 h-3 w-3" />
          Limpar
        </Button>
      )}
    </div>
  );
}
