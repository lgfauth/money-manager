"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { queryKeys } from "@/lib/query-client";
import { getApiErrorMessage } from "@/lib/api-errors";
import { toast } from "sonner";
import type {
  BankConnectionDto,
  BankMcpConnectionDto,
  BankMcpAccountDto,
  SaveApiKeyResultDto,
  CompleteOnboardingRequestDto,
} from "@/types/bank-connection";

export function useBankConnections() {
  return useQuery({
    queryKey: queryKeys.bankConnections,
    queryFn: () => apiClient.get<BankConnectionDto[]>("/api/bank-connections"),
  });
}

export function useAvailableConnections() {
  return useQuery({
    queryKey: queryKeys.bankConnectionsAvailable,
    queryFn: () =>
      apiClient.get<BankMcpConnectionDto[]>("/api/bank-connections/available"),
    enabled: false,
    retry: false,
  });
}

export function useConnectionAccounts(connectionId: string, enabled: boolean) {
  return useQuery({
    queryKey: queryKeys.bankConnectionAccounts(connectionId),
    queryFn: () =>
      apiClient.get<{ connectionId: string; accounts: BankMcpAccountDto[] }>(
        `/api/bank-connections/${connectionId}/accounts`
      ),
    enabled,
    retry: false,
  });
}

export function useSaveApiKey() {
  return useMutation({
    mutationFn: (apiKey: string) =>
      apiClient.post<SaveApiKeyResultDto>("/api/bank-connections/api-key", {
        apiKey,
      }),
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "API key inválida ou sem permissão")),
  });
}

export function useRegisterConnection() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (externalConnectionId: string) =>
      apiClient.post<BankConnectionDto>("/api/bank-connections/register", {
        externalConnectionId,
      }),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.bankConnections });
      qc.invalidateQueries({ queryKey: queryKeys.bankConnectionsAvailable });
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao registrar banco")),
  });
}

export function useCompleteOnboarding(connectionId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CompleteOnboardingRequestDto) =>
      apiClient.post<BankConnectionDto>(
        `/api/bank-connections/${connectionId}/onboarding`,
        data
      ),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.bankConnections });
      toast.success("Banco conectado! Importando transações...");
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao finalizar configuração")),
  });
}

export function useSyncBank() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (connectionId: string) =>
      apiClient.post<void>(`/api/bank-connections/${connectionId}/sync`, {}),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.bankConnections });
      toast.success("Sincronização concluída");
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao sincronizar")),
  });
}

export function useDisconnectBank() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (connectionId: string) =>
      apiClient.delete<void>(`/api/bank-connections/${connectionId}`),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.bankConnections });
      toast.success("Banco desconectado");
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao desconectar banco")),
  });
}
