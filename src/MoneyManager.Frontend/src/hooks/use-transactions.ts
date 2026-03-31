"use client";

import {
  useQuery,
  useMutation,
  useQueryClient,
} from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { queryKeys } from "@/lib/query-client";
import type {
  TransactionRequestDto,
  TransactionResponseDto,
  TransactionFilters,
  PaginatedResponse,
  InstallmentPurchaseRequestDto,
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
      toast.success("Transacao criada com sucesso");
    },
    onError: () => toast.error("Erro ao criar transacao"),
  });
}

export function useCreateInstallment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: InstallmentPurchaseRequestDto) =>
      apiClient.post<void>("/api/transactions/installment-purchase", data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["transactions"] });
      qc.invalidateQueries({ queryKey: queryKeys.accounts });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      qc.invalidateQueries({ queryKey: ["invoices"] });
      toast.success("Compra parcelada registrada");
    },
    onError: () => toast.error("Erro ao registrar parcelamento"),
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
      toast.success("Transacao atualizada");
    },
    onError: () => toast.error("Erro ao atualizar transacao"),
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
      toast.success("Transacao excluida");
    },
    onError: () => toast.error("Erro ao excluir transacao"),
  });
}
