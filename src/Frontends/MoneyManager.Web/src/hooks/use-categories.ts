"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { getApiErrorMessage } from "@/lib/api-errors";
import { invalidateFinancialSummaries, queryKeys } from "@/lib/query-client";
import type {
  CategoryRequestDto,
  CategoryResponseDto,
  CreateDefaultCategoriesResponseDto,
} from "@/types/category";
import { toast } from "sonner";

export function useCategories() {
  return useQuery({
    queryKey: queryKeys.categories,
    queryFn: () => apiClient.get<CategoryResponseDto[]>("/api/categories"),
  });
}

export function useCreateCategory() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CategoryRequestDto) =>
      apiClient.post<CategoryResponseDto>("/api/categories", data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.categories });
      invalidateFinancialSummaries(qc);
      toast.success("Categoria criada com sucesso");
    },
    onError: (error) => toast.error(getApiErrorMessage(error, "Erro ao criar categoria")),
  });
}

export function useUpdateCategory() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: CategoryRequestDto }) =>
      apiClient.put<void>(`/api/categories/${id}`, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.categories });
      invalidateFinancialSummaries(qc);
      toast.success("Categoria atualizada");
    },
    onError: (error) => toast.error(getApiErrorMessage(error, "Erro ao atualizar categoria")),
  });
}

export function useDeleteCategory() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete<void>(`/api/categories/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.categories });
      invalidateFinancialSummaries(qc);
      toast.success("Categoria excluída");
    },
    onError: (error) => toast.error(getApiErrorMessage(error, "Erro ao excluir categoria")),
  });
}

/** Cria as categorias da lista padrão que o usuário ainda não tem (idempotente no servidor). */
export function useCreateDefaultCategories() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () =>
      apiClient.post<CreateDefaultCategoriesResponseDto>("/api/categories/defaults", {}),
    onSuccess: (result) => {
      qc.setQueryData(queryKeys.categories, result.categories);
      qc.invalidateQueries({ queryKey: queryKeys.categories });
      invalidateFinancialSummaries(qc);
      toast.success(
        result.createdCount === 0
          ? "Você já tem todas as categorias padrão"
          : result.createdCount === 1
            ? "1 categoria padrão criada"
            : `${result.createdCount} categorias padrão criadas`
      );
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao criar as categorias padrão")),
  });
}
