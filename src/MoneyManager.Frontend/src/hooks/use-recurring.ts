"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { queryKeys } from "@/lib/query-client";
import type {
  RecurringTransactionRequestDto,
  RecurringTransactionResponseDto,
} from "@/types/recurring";
import { toast } from "sonner";

export function useRecurring() {
  return useQuery({
    queryKey: queryKeys.recurring,
    queryFn: () =>
      apiClient.get<RecurringTransactionResponseDto[]>(
        "/api/recurring-transactions"
      ),
  });
}

export function useCreateRecurring() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: RecurringTransactionRequestDto) =>
      apiClient.post<RecurringTransactionResponseDto>(
        "/api/recurring-transactions",
        data
      ),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.recurring });
      toast.success("Transacao recorrente criada");
    },
    onError: () => toast.error("Erro ao criar recorrente"),
  });
}

export function useUpdateRecurring() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      data,
    }: {
      id: string;
      data: RecurringTransactionRequestDto;
    }) =>
      apiClient.put<void>(`/api/recurring-transactions/${id}`, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.recurring });
      toast.success("Recorrente atualizada");
    },
    onError: () => toast.error("Erro ao atualizar recorrente"),
  });
}

export function useDeleteRecurring() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      apiClient.delete<void>(`/api/recurring-transactions/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.recurring });
      toast.success("Recorrente excluida");
    },
    onError: () => toast.error("Erro ao excluir recorrente"),
  });
}
