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

/** Resposta de POST /api/categories/defaults: quantas foram criadas e as categorias ativas do usuário. */
export interface CreateDefaultCategoriesResponseDto {
  createdCount: number;
  categories: CategoryResponseDto[];
}
