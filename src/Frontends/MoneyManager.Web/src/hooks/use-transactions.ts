"use client";

import {
  useQuery,
  useMutation,
  useQueryClient,
} from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { getApiErrorMessage } from "@/lib/api-errors";
import { queryKeys } from "@/lib/query-client";
import type {
  TransactionRequestDto,
  TransactionResponseDto,
  TransactionFilters,
  PaginatedResponse,
} from "@/types/transaction";
import { toast } from "sonner";
import { DEFAULT_PAGE_SIZE } from "@/config/constants";

export const defaultFilters: TransactionFilters = {
  page: 1,
  pageSize: DEFAULT_PAGE_SIZE,
  sortBy: "date",
};

function buildQueryString(filters: TransactionFilters): string {
  const params = new URLSearchParams();
  params.set("page", String(filters.page));
  params.set("pageSize", String(filters.pageSize));
  params.set("sortBy", filters.sortBy);
  if (filters.startDate) params.set("startDate", filters.startDate);
  if (filters.endDate) params.set("endDate", filters.endDate);
  if (filters.type) params.set("type", filters.type);
  if (filters.accountId) params.set("accountId", filters.accountId);
  return params.toString();
}

export function useTransactions(filters: TransactionFilters) {
  return useQuery({
    queryKey: queryKeys.transactions(filters),
    queryFn: () =>
      apiClient.get<PaginatedResponse<TransactionResponseDto>>(
        `/api/transactions?${buildQueryString(filters)}`
      ),
  });
}

export function useCreateTransaction() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: TransactionRequestDto) =>
      apiClient.post<TransactionResponseDto>("/api/transactions", data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["transactions"] });
      qc.invalidateQueries({ queryKey: queryKeys.accounts });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Transação criada com sucesso");
    },
    onError: (error) => toast.error(getApiErrorMessage(error, "Erro ao criar transação")),
  });
}


export function useUpdateTransaction() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: TransactionRequestDto }) =>
      apiClient.put<void>(`/api/transactions/${id}`, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["transactions"] });
      qc.invalidateQueries({ queryKey: queryKeys.accounts });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Transação atualizada");
    },
    onError: (error) => toast.error(getApiErrorMessage(error, "Erro ao atualizar transação")),
  });
}

export function useDeleteTransaction() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) =>
      apiClient.delete<void>(`/api/transactions/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["transactions"] });
      qc.invalidateQueries({ queryKey: queryKeys.accounts });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Transação excluída");
    },
    onError: (error) => toast.error(getApiErrorMessage(error, "Erro ao excluir transação")),
  });
}
