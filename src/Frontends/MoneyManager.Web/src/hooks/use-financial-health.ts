"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { queryKeys } from "@/lib/query-client";
import { getApiErrorMessage } from "@/lib/api-errors";
import { toast } from "sonner";
import type {
  FinancialHealthSettings,
  PatrimonyBucket,
  SnapshotStatus,
  MonthlySnapshot,
  HealthScore,
  UpsertFinancialHealthSettingsRequest,
  UpsertPatrimonyBucketRequest,
  ConfirmSnapshotRequest,
} from "@/types/financial-health";

export function useFinancialHealthSettings() {
  return useQuery({
    queryKey: queryKeys.financialHealthSettings,
    queryFn: () =>
      apiClient.get<FinancialHealthSettings | null>("/api/financial-health/settings").catch((err) => {
        if (err?.status === 404) return null;
        throw err;
      }),
  });
}

export function useUpsertFinancialHealthSettings() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpsertFinancialHealthSettingsRequest) =>
      apiClient.put<FinancialHealthSettings>("/api/financial-health/settings", data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.financialHealthSettings });
      qc.invalidateQueries({ queryKey: queryKeys.financialHealthScore });
      toast.success("Configurações salvas com sucesso");
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao salvar configurações")),
  });
}

export function useFinancialHealthBuckets() {
  return useQuery({
    queryKey: queryKeys.financialHealthBuckets,
    queryFn: () =>
      apiClient.get<PatrimonyBucket[]>("/api/financial-health/buckets"),
  });
}

export function useUpsertPatrimonyBucket() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: UpsertPatrimonyBucketRequest) =>
      apiClient.post<PatrimonyBucket>("/api/financial-health/buckets", data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.financialHealthBuckets });
      qc.invalidateQueries({ queryKey: queryKeys.financialHealthScore });
      qc.invalidateQueries({ queryKey: queryKeys.financialHealthSnapshotStatus });
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao salvar balde")),
  });
}

export function useSnapshotStatus() {
  return useQuery({
    queryKey: queryKeys.financialHealthSnapshotStatus,
    queryFn: () =>
      apiClient.get<SnapshotStatus>("/api/financial-health/snapshots/current"),
  });
}

export function useSnapshotHistory(year: number) {
  return useQuery({
    queryKey: queryKeys.financialHealthSnapshotHistory(year),
    queryFn: () =>
      apiClient.get<MonthlySnapshot[]>(
        `/api/financial-health/snapshots?year=${year}`
      ),
    enabled: !!year,
  });
}

export function useConfirmSnapshot() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      year,
      month,
      data,
    }: {
      year: number;
      month: number;
      data: ConfirmSnapshotRequest;
    }) =>
      apiClient.patch<void>(
        `/api/financial-health/snapshots/${year}/${month}/confirm`,
        data
      ),
    onSuccess: (_, { year }) => {
      qc.invalidateQueries({ queryKey: queryKeys.financialHealthSnapshotStatus });
      qc.invalidateQueries({
        queryKey: queryKeys.financialHealthSnapshotHistory(year),
      });
      qc.invalidateQueries({ queryKey: queryKeys.financialHealthScore });
      toast.success("Check-in confirmado com sucesso");
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao confirmar check-in")),
  });
}

export function useDismissSnapshot() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ year, month }: { year: number; month: number }) =>
      apiClient.patch<void>(
        `/api/financial-health/snapshots/${year}/${month}/dismiss`
      ),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.financialHealthSnapshotStatus });
      toast.success("Mês ignorado");
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao ignorar mês")),
  });
}

export function useHealthScore() {
  return useQuery({
    queryKey: queryKeys.financialHealthScore,
    queryFn: () =>
      apiClient.get<HealthScore | null>("/api/financial-health/score").catch((err) => {
        if (err?.status === 404) return null;
        throw err;
      }),
  });
}
