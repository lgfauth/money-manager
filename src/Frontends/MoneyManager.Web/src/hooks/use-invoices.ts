"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { getApiErrorMessage } from "@/lib/api-errors";
import { toast } from "sonner";

interface ReconcileResult {
  accountsProcessed: number;
  invoicesRecalculated: number;
  accountsUpdated: number;
  totalCommittedCredit: number;
}

interface ReconcileResponse {
  result: ReconcileResult;
}

export interface InvoiceResponseDto {
  id: string;
  accountId: string;
  status: string;
  totalAmount: number;
  paidAmount: number;
  remainingAmount: number;
  periodStart: string;
  periodEnd: string;
  dueDate: string;
  referenceMonth: string;
}

export interface PayInvoiceRequest {
  invoiceId: string;
  amount: number;
  paymentAccountId: string;
  paymentDate: string;
  remainingAmount: number;
}

export function useOpenInvoice(accountId: string | undefined) {
  return useQuery({
    queryKey: ["invoices", "open", accountId],
    queryFn: () =>
      apiClient.get<InvoiceResponseDto>(
        `/api/credit-card-invoices/accounts/${accountId}/open`
      ),
    enabled: !!accountId,
  });
}

export function usePayInvoice() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ remainingAmount, ...data }: PayInvoiceRequest) => {
      const endpoint =
        data.amount < remainingAmount
          ? "/api/credit-card-invoices/pay-partial"
          : "/api/credit-card-invoices/pay";
      return apiClient.post<void>(endpoint, data);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["invoices"] });
      qc.invalidateQueries({ queryKey: ["accounts"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Pagamento registrado com sucesso");
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao registrar pagamento")),
  });
}

export function useReconcileCreditCards() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: () =>
      apiClient.post<ReconcileResponse>("/api/admin/reconcile-credit-cards", {}),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ["accounts"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Cartões reconciliados com sucesso");
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao reconciliar cartões")),
  });
}
