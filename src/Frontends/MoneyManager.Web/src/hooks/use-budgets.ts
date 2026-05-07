"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { queryKeys } from "@/lib/query-client";
import type {
  BudgetRequestDto,
  BudgetResponseDto,
} from "@/types/budget";
import { toast } from "sonner";

export function useBudget(month: string) {
  return useQuery({
    queryKey: queryKeys.budgets(month),
    queryFn: () =>
      apiClient.get<BudgetResponseDto>(`/api/budgets/${month}`),
    enabled: !!month,
  });
}

export function useCreateBudget() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: BudgetRequestDto) =>
      apiClient.post<BudgetResponseDto>("/api/budgets", data),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: queryKeys.budgets(variables.month) });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Orcamento salvo com sucesso");
    },
    onError: () => toast.error("Erro ao salvar orcamento"),
  });
}

export function useUpdateBudget() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      data,
    }: {
      id: string;
      data: BudgetRequestDto;
    }) => apiClient.put<void>(`/api/budgets/${id}`, data),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({
        queryKey: queryKeys.budgets(variables.data.month),
      });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Orcamento atualizado");
    },
    onError: () => toast.error("Erro ao atualizar orcamento"),
  });
}

export function useAllBudgets(enabled = true) {
  return useQuery({
    queryKey: ["budgets"],
    queryFn: () => apiClient.get<BudgetResponseDto[]>("/api/budgets"),
    enabled,
  });
}

// Copia os itens de um orçamento de origem para o mês destino
export function useCopyBudget() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      sourceMonth,
      targetMonth,
    }: {
      sourceMonth: string;
      targetMonth: string;
    }) =>
      apiClient.post<BudgetResponseDto>(`/api/budgets/${sourceMonth}/copy`, {
        targetMonth,
      }),
    onSuccess: (_, variables) => {
      qc.invalidateQueries({ queryKey: queryKeys.budgets(variables.targetMonth) });
      qc.invalidateQueries({ queryKey: ["budgets"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Orçamento copiado com sucesso");
    },
    onError: () => toast.error("Erro ao copiar orçamento"),
  });
}
