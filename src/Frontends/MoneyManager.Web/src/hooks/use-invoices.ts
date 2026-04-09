"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { getApiErrorMessage } from "@/lib/api-errors";
import { queryKeys } from "@/lib/query-client";
import { toast } from "sonner";
import type {
  AdminActionResponse,
  CreditCardInvoiceResponseDto,
  CreditCardReconciliationSummaryDto,
  InvoiceSummaryDto,
  InvoicePaymentRequestDto,
} from "@/types/invoice";
import type { TransactionResponseDto } from "@/types/transaction";

export function useOpenInvoice(accountId: string) {
  return useQuery({
    queryKey: [...queryKeys.invoices(accountId), "open"],
    queryFn: () =>
      apiClient.get<CreditCardInvoiceResponseDto>(
        `/api/credit-card-invoices/accounts/${accountId}/open`
      ),
    enabled: !!accountId,
  });
}

export function useAccountInvoices(accountId: string) {
  return useQuery({
    queryKey: queryKeys.invoices(accountId),
    queryFn: () =>
      apiClient.get<CreditCardInvoiceResponseDto[]>(
        `/api/credit-card-invoices/accounts/${accountId}`
      ),
    enabled: !!accountId,
  });
}

export function useOverdueInvoices() {
  return useQuery({
    queryKey: queryKeys.overdueInvoices,
    queryFn: () =>
      apiClient.get<CreditCardInvoiceResponseDto[]>(
        "/api/credit-card-invoices/overdue"
      ),
  });
}

export function useInvoiceSummary(invoiceId: string) {
  return useQuery({
    queryKey: queryKeys.invoiceSummary(invoiceId),
    queryFn: () =>
      apiClient.get<InvoiceSummaryDto>(
        `/api/credit-card-invoices/${invoiceId}/summary`
      ),
    enabled: !!invoiceId,
  });
}

export function useInvoiceTransactions(invoiceId: string) {
  return useQuery({
    queryKey: queryKeys.invoiceTransactions(invoiceId),
    queryFn: () =>
      apiClient.get<TransactionResponseDto[]>(
        `/api/credit-card-invoices/${invoiceId}/transactions`
      ),
    enabled: !!invoiceId,
  });
}

export function usePayInvoice(accountId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: InvoicePaymentRequestDto) => {
      const endpoint =
        data.amount < (data as InvoicePaymentRequestDto & { remainingAmount?: number }).remainingAmount!
          ? "/api/credit-card-invoices/pay-partial"
          : "/api/credit-card-invoices/pay";
      return apiClient.post<void>(endpoint, data);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.accounts });
      qc.invalidateQueries({ queryKey: queryKeys.invoices(accountId) });
      qc.invalidateQueries({ queryKey: queryKeys.overdueInvoices });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Pagamento registrado com sucesso");
    },
    onError: (error) => toast.error(getApiErrorMessage(error, "Erro ao registrar pagamento")),
  });
}

export function usePayInvoiceDirect() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({
      data,
      remainingAmount,
    }: {
      data: InvoicePaymentRequestDto;
      remainingAmount: number;
    }) => {
      const endpoint =
        data.amount < remainingAmount
          ? "/api/credit-card-invoices/pay-partial"
          : "/api/credit-card-invoices/pay";
      return apiClient.post<void>(endpoint, data);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.accounts });
      qc.invalidateQueries({ queryKey: ["invoices"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Pagamento registrado com sucesso");
    },
    onError: (error) => toast.error(getApiErrorMessage(error, "Erro ao registrar pagamento")),
  });
}

export function useCloseInvoice() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (invoiceId: string) =>
      apiClient.post<void>(`/api/credit-card-invoices/${invoiceId}/close`, {}),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.accounts });
      qc.invalidateQueries({ queryKey: ["invoices"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Fatura fechada com sucesso. Novo período iniciado.");
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao fechar fatura")),
  });
}

export function useReconcileCreditCards() {
  const qc = useQueryClient();

  return useMutation({
    mutationFn: () =>
      apiClient.post<AdminActionResponse<CreditCardReconciliationSummaryDto>>(
        "/api/admin/reconcile-credit-cards",
        {}
      ),
    onSuccess: ({ result }) => {
      qc.invalidateQueries({ queryKey: queryKeys.accounts });
      qc.invalidateQueries({ queryKey: ["invoices"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success(
        `${result.accountsProcessed} cartões reconciliados e ${result.invoicesRecalculated} faturas recalculadas`
      );
    },
    onError: (error) =>
      toast.error(
        getApiErrorMessage(error, "Erro ao reconciliar cartões de crédito")
      ),
  });
}
