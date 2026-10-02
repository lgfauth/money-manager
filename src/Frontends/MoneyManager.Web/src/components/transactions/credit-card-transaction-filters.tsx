"use client";

import { useCreditCards } from "@/hooks/use-credit-cards";
import { useCategories } from "@/hooks/use-categories";
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

export interface CreditCardFilterValues {
  type?: string;
  creditCardId?: string;
  categoryId?: string;
  startDate?: string;
  endDate?: string;
}

interface CreditCardTransactionFiltersProps {
  filters: CreditCardFilterValues;
  onFiltersChange: (filters: CreditCardFilterValues) => void;
}

const typeLabels: Record<string, string> = {
  Purchase: "Compra",
  Refund: "Estorno",
};

const getTypeLabel = (type?: string): string => {
  if (!type) return "Todos";
  return typeLabels[type] || "Todos";
};

export function CreditCardTransactionFilters({
  filters,
  onFiltersChange,
}: CreditCardTransactionFiltersProps) {
  const { data: cards } = useCreditCards();
  const { data: categories } = useCategories();

  const hasActiveFilters =
    filters.type ||
    filters.creditCardId ||
    filters.categoryId ||
    filters.startDate ||
    filters.endDate;

  const clearFilters = () => {
    onFiltersChange({
      type: undefined,
      creditCardId: undefined,
      categoryId: undefined,
      startDate: undefined,
      endDate: undefined,
    });
  };

  const selectedCard = filters.creditCardId
    ? cards?.find((c) => c.id === filters.creditCardId)?.name
    : undefined;

  const selectedCategory = filters.categoryId
    ? categories?.find((c) => c.id === filters.categoryId)?.name
    : undefined;

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
            <SelectValue placeholder="Todos">
              {getTypeLabel(filters.type)}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="">Todos</SelectItem>
            <SelectItem value="Purchase">Compra</SelectItem>
            <SelectItem value="Refund">Estorno</SelectItem>
          </SelectContent>
        </Select>
      </div>

      <div className="space-y-1">
        <label className="text-xs text-muted-foreground">Cartão</label>
        <Select
          value={filters.creditCardId ?? ""}
          onValueChange={(v) =>
            onFiltersChange({ ...filters, creditCardId: v || undefined })
          }
        >
          <SelectTrigger className="w-[160px]">
            <SelectValue placeholder="Todos">
              {selectedCard || "Todos"}
            </SelectValue>
          </SelectTrigger>
          <SelectContent>
            <SelectItem value="">Todos</SelectItem>
            {cards?.map((card) => (
              <SelectItem key={card.id} value={card.id}>
                {card.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="space-y-1">
        <label className="text-xs text-muted-foreground">Categoria</label>
        <Select
          value={filters.categoryId ?? ""}
          onValueChange={(v) =>
            onFiltersChange({ ...filters, categoryId: v || undefined })
          }
        >
          <SelectTrigger className="w-[160px]">
            <SelectValue placeholder="Todas">
              {selectedCategory || "Todas"}
            </SelectValue>
          </SelectTrigger>
          <SelectContent className="w-64 max-w-[85vw]">
            <SelectItem value="">Todas</SelectItem>
            {categories?.map((cat) => (
              <SelectItem key={cat.id} value={cat.id} textClassName="whitespace-normal">
                {cat.name}
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
