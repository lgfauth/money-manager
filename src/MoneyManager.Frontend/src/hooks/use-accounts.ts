"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { getApiErrorMessage } from "@/lib/api-errors";
import { queryKeys } from "@/lib/query-client";
import type {
  AccountRequestDto,
  AccountResponseDto,
} from "@/types/account";
import { toast } from "sonner";

export function useAccounts() {
  return useQuery({
    queryKey: queryKeys.accounts,
    queryFn: () => apiClient.get<AccountResponseDto[]>("/api/accounts"),
  });
}

export function useCreateAccount() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: AccountRequestDto) =>
      apiClient.post<AccountResponseDto>("/api/accounts", data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.accounts });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Conta criada com sucesso");
    },
    onError: (error) => toast.error(getApiErrorMessage(error, "Erro ao criar conta")),
  });
}

export function useUpdateAccount() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, data }: { id: string; data: AccountRequestDto }) =>
      apiClient.put<void>(`/api/accounts/${id}`, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.accounts });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Conta atualizada");
    },
    onError: (error) => toast.error(getApiErrorMessage(error, "Erro ao atualizar conta")),
  });
}

export function useDeleteAccount() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete<void>(`/api/accounts/${id}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.accounts });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Conta excluida");
    },
    onError: (error) => toast.error(getApiErrorMessage(error, "Erro ao excluir conta")),
  });
}
