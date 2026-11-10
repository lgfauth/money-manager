"use client";

import { useEffect, useRef } from "react";
import { usePathname, useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { useRecentInvoiceImports } from "@/hooks/use-invoice-imports";
import { queryKeys } from "@/lib/query-client";
import { invoiceImportHref, isActiveInvoiceImport } from "@/lib/invoice-import";
import type {
  CreditCardInvoiceImportResponseDto,
  InvoiceImportStatus,
} from "@/types/invoice-import";

/**
 * Avisa por toast, em qualquer tela do dashboard, quando uma importação sai de um estado ativo
 * para "aguardando revisão" ou "falhou". Compara cada consulta da lista com a anterior: a
 * primeira consulta (ao abrir ou recarregar a página) só registra os estados, então nada é
 * avisado de novo. Não avisa se o usuário já viu a mudança na tela da importação.
 */
export function InvoiceImportWatcher() {
  const { data } = useRecentInvoiceImports();
  const pathname = usePathname();
  const router = useRouter();
  const qc = useQueryClient();

  const previousStatuses = useRef<Map<string, InvoiceImportStatus> | null>(null);
  const pathnameRef = useRef(pathname);

  useEffect(() => {
    pathnameRef.current = pathname;
  }, [pathname]);

  useEffect(() => {
    if (!data) return;
    const previous = previousStatuses.current;
    previousStatuses.current = new Map(data.map((job) => [job.id, job.status]));
    if (!previous) return;

    for (const job of data) {
      const before = previous.get(job.id);
      if (!before || !isActiveInvoiceImport(before)) continue;
      if (job.status !== "AwaitingReview" && job.status !== "Failed") continue;

      // Na tela da importação a própria tela mostra a mudança
      if (pathnameRef.current === invoiceImportHref(job.id)) continue;
      const detail = qc.getQueryData<CreditCardInvoiceImportResponseDto>(
        queryKeys.invoiceImport(job.id)
      );
      if (detail?.status === job.status) continue;

      notifyTransition(job, () => router.push(invoiceImportHref(job.id)));
    }
  }, [data, qc, router]);

  return null;
}

function notifyTransition(job: CreditCardInvoiceImportResponseDto, open: () => void) {
  // O id evita toast duplicado da mesma transição
  const id = `invoice-import-${job.id}-${job.status}`;

  if (job.status === "AwaitingReview") {
    const count = job.items.length;
    toast.success(
      `Fatura pronta para revisão — ${count === 1 ? "1 lançamento" : `${count} lançamentos`}`,
      {
        id,
        description: "Confira os lançamentos antes de cadastrar.",
        duration: 10_000,
        action: { label: "Revisar", onClick: open },
      }
    );
    return;
  }

  toast.error("Não foi possível importar a fatura", {
    id,
    description: job.errorMessage ?? "Ocorreu um erro ao ler os arquivos enviados.",
    duration: 10_000,
    action: { label: "Ver detalhes", onClick: open },
  });
}
