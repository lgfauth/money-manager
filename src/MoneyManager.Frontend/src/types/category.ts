export enum CategoryType {
  Income = "Income",
  Expense = "Expense",
}

export interface CategoryRequestDto {
  name: string;
  type: CategoryType;
  color: string;
}

export interface CategoryResponseDto {
  id: string;
  name: string;
  type: CategoryType;
  color: string;
}
