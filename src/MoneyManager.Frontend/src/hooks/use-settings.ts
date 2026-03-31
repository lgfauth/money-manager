"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { queryKeys } from "@/lib/query-client";
import { toast } from "sonner";
import type { UserSettingsDto } from "@/types/settings";

export function useSettings() {
  return useQuery({
    queryKey: queryKeys.settings,
    queryFn: () => apiClient.get<UserSettingsDto>("/api/settings"),
  });
}

export function useUpdateSettings() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: Partial<UserSettingsDto>) =>
      apiClient.put<UserSettingsDto>("/api/settings", data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.settings });
      toast.success("Configurações salvas");
    },
    onError: () => toast.error("Erro ao salvar configurações"),
  });
}
