"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { getApiErrorMessage } from "@/lib/api-errors";
import { queryKeys } from "@/lib/query-client";
import { toast } from "sonner";
import type {
  UserProfileDto,
  ChangePasswordDto,
  UpdateEmailDto,
  AcceptTermsDto,
} from "@/types/profile";

export function useProfile(options?: { enabled?: boolean }) {
  return useQuery({
    queryKey: queryKeys.profile,
    queryFn: () => apiClient.get<UserProfileDto>("/api/profile"),
    enabled: options?.enabled ?? true,
  });
}

export function useRefreshProfile() {
  const qc = useQueryClient();

  return () => qc.invalidateQueries({ queryKey: queryKeys.profile });
}

export function useUpdateProfile() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Partial<UserProfileDto>) =>
      apiClient.put<UserProfileDto>("/api/profile", data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.profile });
      toast.success("Perfil atualizado com sucesso");
    },
    onError: (error) => toast.error(getApiErrorMessage(error, "Erro ao atualizar perfil")),
  });
}

export function useChangePassword() {
  return useMutation({
    mutationFn: (data: ChangePasswordDto) =>
      apiClient.post<void>("/api/profile/change-password", data),
    onSuccess: () => toast.success("Senha alterada com sucesso"),
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao alterar senha. Verifique a senha atual.")),
  });
}

export function useUpdateEmail() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpdateEmailDto) =>
      apiClient.post<UserProfileDto>("/api/profile/update-email", data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.profile });
      toast.success("E-mail atualizado com sucesso");
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao atualizar e-mail. Verifique a senha.")),
  });
}

export function useDataCount() {
  return useQuery({
    queryKey: queryKeys.dataCount,
    queryFn: () =>
      apiClient.get<{ totalRecords: number; message: string }>(
        "/api/accountdeletion/data-count"
      ),
    enabled: false, // Only fetch on demand
  });
}

export function useDeleteAccount() {
  return useMutation({
    mutationFn: (data: { password: string; confirmationText: string }) =>
      apiClient.post<void>("/api/accountdeletion/delete-account", data),
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao deletar conta")),
  });
}

export function useAcceptTerms() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: (data: AcceptTermsDto) =>
      apiClient.post<UserProfileDto>("/api/users/accept-terms", data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.profile });
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao aceitar os termos")),
  });
}
