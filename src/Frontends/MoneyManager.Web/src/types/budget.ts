export interface BudgetItemDto {
  categoryId: string;
  limitAmount: number;
}

export interface BudgetRequestDto {
  month: string;
  items: BudgetItemDto[];
}

export interface BudgetItemResponseDto {
  categoryId: string;
  categoryName: string;
  categoryColor: string;
  limitAmount: number;
  spentAmount: number;
}

export interface BudgetResponseDto {
  id: string;
  month: string;
  items: BudgetItemResponseDto[];
}
