"use client";

import { useEffect, useMemo, useState } from "react";
import { toast } from "sonner";
import { AlertTriangle, CheckCircle2, History, Info, Loader2, Pencil, RotateCcw } from "lucide-react";

import { Button } from "@/components/ui/button";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { CreditCardForm } from "@/components/credit-cards/credit-card-form";
import {
  InvoiceImportSummary,
  InvoiceImportWarnings,
} from "@/components/invoice-imports/invoice-import-summary";
import { InvoiceImportHolders } from "@/components/invoice-imports/invoice-import-holders";
import {
  InvoiceImportItems,
  type ReviewRow,
} from "@/components/invoice-imports/invoice-import-items";
import { InvoiceImportItemEditor } from "@/components/invoice-imports/invoice-import-item-editor";
import { useCategories } from "@/hooks/use-categories";
import { useConfirmInvoiceImport } from "@/hooks/use-invoice-imports";
import { useMoneyPrivacy } from "@/hooks/use-money-privacy";
import { ApiClientError } from "@/lib/api-errors";
import { formatDateTime, readInvoiceImportApiError } from "@/lib/invoice-import";
import { cn } from "@/lib/utils";
import {
  blockingScheduleMismatches,
  brazilToday,
  buildInitialReviewState,
  currentItemWarnings,
  describeItemForErrors,
  describeItemSource,
  isAlreadyImported,
  isBlockingItemWarning,
  isItemIncluded,
  isSameReviewState,
  requiresHolderChoice,
  restoreReviewState,
  signedAmount,
  toConfirmItem,
  type ReviewItemState,
  type ReviewState,
} from "@/lib/invoice-import-review";
import { useAuthStore } from "@/stores/auth-store";
import { useInvoiceImportDraftStore } from "@/stores/invoice-import-draft-store";
import type { CreditCardResponseDto } from "@/types/credit-card";
import type { CreditCardInvoiceImportResponseDto } from "@/types/invoice-import";

const CONFIRM_FALLBACK_MESSAGE = "Não foi possível importar os lançamentos. Tente de novo.";

interface InitialReview {
  review: ReviewState;
  /** Quando o rascunho recuperado foi salvo (ISO), ou null se a revisão partiu do servidor. */
  restoredAt: string | null;
  /** Havia rascunho de outra versão da importação: foi descartado. */
  staleDraft: boolean;
}

/** Parte do rascunho salvo no navegador quando ele é da mesma versão da importação. */
function loadInitialReview(
  job: CreditCardInvoiceImportResponseDto,
  userId: string | null
): InitialReview {
  const { drafts, userId: owner } = useInvoiceImportDraftStore.getState();
  const draft = userId && owner === userId ? drafts[job.id] : undefined;
  if (!draft) return { review: buildInitialReviewState(job), restoredAt: null, staleDraft: false };
  if (draft.version !== job.version)
    return { review: buildInitialReviewState(job), restoredAt: null, staleDraft: true };
  return { review: restoreReviewState(job, draft.review), restoredAt: draft.savedAt, staleDraft: false };
}

interface ConfirmErrors {
  general: string[];
  byItem: Record<string, string>;
}

interface InvoiceImportReviewProps {
  job: CreditCardInvoiceImportResponseDto;
  card: CreditCardResponseDto | undefined;
}

/**
 * Revisão de uma importação aguardando confirmação: titulares, lançamentos (seleção e edição),
 * totais e confirmação. As edições ficam só no navegador até a confirmação, salvas como
 * rascunho por importação e versão (`invoice-import-draft-store`).
 */
export function InvoiceImportReview({ job, card }: InvoiceImportReviewProps) {
  const { formatMonetaryValue } = useMoneyPrivacy();
  const { data: categories } = useCategories();
  const confirm = useConfirmInvoiceImport();

  const userId = useAuthStore((s) => s.user?.id ?? null);
  const saveDraft = useInvoiceImportDraftStore((s) => s.saveDraft);
  const clearDraft = useInvoiceImportDraftStore((s) => s.clearDraft);

  const [initial] = useState(() => loadInitialReview(job, userId));
  const [review, setReview] = useState<ReviewState>(initial.review);
  const [restoredAt, setRestoredAt] = useState(initial.restoredAt);
  const [undoOpen, setUndoOpen] = useState(false);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [confirmOpen, setConfirmOpen] = useState(false);
  const [cardFormOpen, setCardFormOpen] = useState(false);
  const [errors, setErrors] = useState<ConfirmErrors | null>(null);

  const closingDate = job.invoice?.closingDate ?? null;
  const today = brazilToday();
  const holderChoice = requiresHolderChoice(job);

  // Estado lido do servidor: base para saber se há alterações e para desfazê-las
  const serverReview = useMemo(() => buildInitialReviewState(job), [job]);
  const isDirty = !isSameReviewState(review, serverReview);

  // Rascunho de outra versão (a importação mudou no servidor): descartado com aviso
  useEffect(() => {
    if (!initial.staleDraft) return;
    clearDraft(job.id);
    toast.info("A importação foi atualizada. Suas alterações anteriores foram descartadas.", {
      id: `invoice-import-draft-stale-${job.id}`,
    });
  }, [initial.staleDraft, clearDraft, job.id]);

  // Salva o rascunho a cada alteração; sem alterações, não guarda nada
  useEffect(() => {
    if (!userId) return;
    if (isDirty) saveDraft(userId, job.id, job.version, review);
    else clearDraft(job.id);
  }, [review, isDirty, userId, job.id, job.version, saveDraft, clearDraft]);

  const categoryNames = useMemo(
    () => new Map((categories ?? []).map((c) => [c.id, c.name])),
    [categories]
  );

  const rows: ReviewRow[] = job.items
    .filter((item) => isItemIncluded(job, item, review.selectedHolders))
    .map((item) => {
      const state = review.items[item.id];
      const warnings = currentItemWarnings(item, state, closingDate, today);
      return {
        item,
        state,
        warnings,
        hasBlocking: warnings.some(isBlockingItemWarning),
        locked: isAlreadyImported(item),
        source: describeItemSource(job.sourceType, item),
        categoryName: state.categoryId ? (categoryNames.get(state.categoryId) ?? null) : null,
        serverError: errors?.byItem[item.id] ?? null,
      };
    });
  const hiddenByHolderCount = job.items.length - rows.length;

  const selectedRows = rows.filter((r) => r.state.selected && !r.locked);
  const selectedTotal = selectedRows.reduce((sum, r) => sum + signedAmount(r.item, r.state), 0);
  const blockedSelected = selectedRows.filter((r) => r.hasBlocking);
  const installmentPurchases = selectedRows.filter((r) => (r.state.totalInstallments ?? 1) > 1);

  // Fechamento/vencimento divergente do cadastro do cartão: confere com o cadastro ATUAL,
  // como o servidor faz na confirmação — ajustar o cartão destrava sem nova leitura
  const scheduleBlocking = job.warnings.filter((w) => w.isBlocking);
  const currentMismatches = card
    ? blockingScheduleMismatches(job.invoice, job.referenceMonth, card.closingDay, card.billingDueDay)
    : null;
  const scheduleBlocks = currentMismatches
    ? currentMismatches.length > 0
    : scheduleBlocking.length > 0;
  const scheduleFixed = scheduleBlocking.length > 0 && !scheduleBlocks;
  // Cadastro alterado depois da leitura a ponto de divergir: o alerta do servidor não existe
  const scheduleNewlyBlocking = scheduleBlocks && scheduleBlocking.length === 0;

  const blockers: string[] = [];
  if (scheduleBlocks) blockers.push("Ajuste o fechamento/vencimento do cartão (veja o alerta acima).");
  if (holderChoice && review.selectedHolders.length === 0)
    blockers.push("Escolha ao menos um titular.");
  if (selectedRows.length === 0) blockers.push("Marque ao menos um lançamento.");
  if (blockedSelected.length > 0)
    blockers.push(
      blockedSelected.length === 1
        ? "Corrija ou desmarque 1 lançamento com alerta que impede a importação."
        : `Corrija ou desmarque ${blockedSelected.length} lançamentos com alerta que impede a importação.`
    );
  const canConfirm = blockers.length === 0 && !confirm.isPending;

  const documentTotal = job.invoice?.totalAmount ?? null;
  const difference = documentTotal !== null ? documentTotal - selectedTotal : null;
  const hasDifference = difference !== null && Math.abs(difference) >= 0.01;

  // --- Ações ---

  const updateItem = (itemId: string, changes: Partial<ReviewItemState>) =>
    setReview((prev) => ({
      ...prev,
      items: { ...prev.items, [itemId]: { ...prev.items[itemId], ...changes } },
    }));

  const toggleItem = (itemId: string) =>
    updateItem(itemId, { selected: !review.items[itemId].selected });

  const setSelected = (itemIds: string[], selected: boolean) =>
    setReview((prev) => {
      const items = { ...prev.items };
      for (const id of itemIds) items[id] = { ...items[id], selected };
      return { ...prev, items };
    });

  const toggleHolder = (name: string) =>
    setReview((prev) => ({
      ...prev,
      selectedHolders: prev.selectedHolders.includes(name)
        ? prev.selectedHolders.filter((h) => h !== name)
        : [...prev.selectedHolders, name],
    }));

  const handleConfirm = () => {
    setConfirmOpen(false);
    setErrors(null);
    confirm.mutate(
      {
        id: job.id,
        data: {
          selectedHolders: holderChoice ? review.selectedHolders : [],
          items: selectedRows.map((r) => toConfirmItem(r.item.id, r.state)),
        },
      },
      {
        onError: (error) => {
          const { message, errors: problems } = readInvoiceImportApiError(error, CONFIRM_FALLBACK_MESSAGE);
          if (!(error instanceof ApiClientError) || error.statusCode !== 400) {
            setErrors({ general: [message, ...problems], byItem: {} });
            return;
          }
          // 400: "message" é o resumo; "errors" traz "Descrição (dd/MM): problema" por lançamento
          const byItem: Record<string, string> = {};
          const general: string[] = [message];
          for (const problem of problems) {
            const row = selectedRows.find((r) =>
              problem.startsWith(`${describeItemForErrors(r.state)}:`)
            );
            if (row && !byItem[row.item.id]) byItem[row.item.id] = problem;
            else general.push(problem);
          }
          setErrors({ general, byItem });
        },
      }
    );
  };

  const handleUndo = () => {
    setUndoOpen(false);
    setReview(serverReview);
    setRestoredAt(null);
    setErrors(null);
    clearDraft(job.id);
  };

  const editingRow = rows.find((r) => r.item.id === editingId) ?? null;

  return (
    <div className="space-y-6 pb-4">
      <InvoiceImportWarnings warnings={job.warnings.filter((w) => !w.isBlocking)} />

      {(scheduleBlocking.length > 0 || scheduleNewlyBlocking) && (
        <div
          role={scheduleBlocks ? "alert" : "status"}
          className={cn(
            "space-y-3 rounded-lg border p-3 text-sm",
            scheduleBlocks
              ? "border-expense/30 bg-expense/10 text-expense"
              : "border-income/30 bg-income/10 text-income"
          )}
        >
          <div className="flex gap-2">
            {scheduleBlocks ? (
              <AlertTriangle className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
            ) : (
              <CheckCircle2 className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
            )}
            <div className="space-y-1">
              {scheduleFixed ? (
                <p>
                  O fechamento e o vencimento da fatura agora conferem com o cadastro do cartão.
                  A conferência é refeita ao confirmar.
                </p>
              ) : scheduleNewlyBlocking ? (
                <p>
                  <span className="font-semibold">Impede a importação: </span>o fechamento ou o
                  vencimento da fatura não confere com o cadastro atual do cartão (diferença de
                  mais de 3 dias). Ajuste o cartão para continuar.
                </p>
              ) : (
                <>
                  {scheduleBlocking.map((w, i) => (
                    <p key={`${w.code}-${i}`}>
                      <span className="font-semibold">Impede a importação: </span>
                      {w.message}
                    </p>
                  ))}
                  <p className="text-foreground/80">
                    Ajuste o cadastro do cartão; a revisão continua disponível aqui depois.
                  </p>
                </>
              )}
            </div>
          </div>
          {card && scheduleBlocks && (
            <Button size="sm" variant="outline" onClick={() => setCardFormOpen(true)}>
              <Pencil className="mr-2 h-4 w-4" />
              Editar cartão
            </Button>
          )}
        </div>
      )}

      <InvoiceImportSummary job={job} />

      {(isDirty || initial.staleDraft) && (
        <div
          role="status"
          className="flex flex-col gap-3 rounded-lg border bg-muted/40 p-3 text-sm sm:flex-row sm:items-center sm:justify-between"
        >
          <p className="flex gap-2 text-muted-foreground">
            <History className="mt-0.5 h-4 w-4 shrink-0" aria-hidden />
            <span>
              {!isDirty
                ? "Esta importação foi atualizada desde a sua última visita, por isso as alterações que você tinha feito foram descartadas. A revisão mostra os dados atuais."
                : restoredAt
                  ? `Você voltou de onde parou: recuperamos as alterações feitas em ${formatDateTime(restoredAt)}. Elas ficam salvas neste navegador até você importar ou descartar.`
                  : "Suas alterações ficam salvas neste navegador: você pode sair e voltar depois sem perdê-las."}
            </span>
          </p>
          {isDirty && (
            <Button
              size="sm"
              variant="outline"
              className="shrink-0"
              onClick={() => setUndoOpen(true)}
              disabled={confirm.isPending}
            >
              <RotateCcw className="mr-2 h-4 w-4" />
              Desfazer alterações
            </Button>
          )}
        </div>
      )}

      {holderChoice && (
        <InvoiceImportHolders
          holders={job.holders}
          selected={review.selectedHolders}
          onToggle={toggleHolder}
        />
      )}

      <InvoiceImportItems
        rows={rows}
        hiddenByHolderCount={hiddenByHolderCount}
        onToggle={toggleItem}
        onSetSelected={setSelected}
        onEdit={setEditingId}
      />

      {errors && (errors.general.length > 0 || Object.keys(errors.byItem).length > 0) && (
        <div
          role="alert"
          className="space-y-1 rounded-lg border border-expense/30 bg-expense/10 p-3 text-sm text-expense"
        >
          {errors.general.map((m) => (
            <p key={m}>{m}</p>
          ))}
          {Object.keys(errors.byItem).length > 0 && (
            <p>Os lançamentos com problema estão indicados na lista.</p>
          )}
        </div>
      )}

      {/* Totais e confirmação: fixos no rodapé no desktop; no celular, no fim da lista
          (fixos ali disputariam espaço com a navegação e os botões flutuantes) */}
      <div className="rounded-xl border bg-card/95 p-4 shadow-lg backdrop-blur md:sticky md:bottom-4 md:z-10">
        <div className="flex flex-col gap-3 md:flex-row md:items-center md:justify-between">
        <dl className="grid flex-1 grid-cols-2 gap-3 text-sm sm:grid-cols-3">
          <div>
            <dt className="text-muted-foreground">Selecionados</dt>
            <dd className="font-semibold">
              {selectedRows.length} · {formatMonetaryValue(selectedTotal)}
            </dd>
          </div>
          <div>
            <dt className="text-muted-foreground">Total da fatura</dt>
            <dd className="font-semibold">
              {documentTotal !== null ? formatMonetaryValue(documentTotal) : "—"}
            </dd>
          </div>
          {difference !== null && (
            <div className="col-span-2 sm:col-span-1">
              <dt className="text-muted-foreground">Diferença</dt>
              <dd
                className={cn(
                  "font-semibold",
                  hasDifference && "text-amber-700 dark:text-amber-400"
                )}
              >
                {formatMonetaryValue(difference)}
              </dd>
            </div>
          )}
        </dl>
        <Button
          className="w-full md:w-auto"
          disabled={!canConfirm}
          onClick={() => setConfirmOpen(true)}
        >
          {confirm.isPending && <Loader2 className="mr-2 h-4 w-4 animate-spin" />}
          {confirm.isPending
            ? "Importando…"
            : `Importar ${selectedRows.length} ${selectedRows.length === 1 ? "lançamento" : "lançamentos"}`}
        </Button>
        </div>
        {hasDifference && (
          <p className="mt-2 flex gap-1.5 text-xs text-muted-foreground">
            <Info className="mt-0.5 h-3.5 w-3.5 shrink-0" aria-hidden />
            Lançamentos desmarcados, de titulares não escolhidos, já cadastrados ou encargos da
            fatura explicam diferenças. Confira se nenhuma compra ficou de fora.
          </p>
        )}
        {blockers.length > 0 && (
          <ul className="mt-2 space-y-0.5 text-xs text-expense">
            {blockers.map((b) => (
              <li key={b}>{b}</li>
            ))}
          </ul>
        )}
      </div>

      <InvoiceImportItemEditor
        item={editingRow?.item ?? null}
        state={editingRow?.state ?? null}
        warnings={editingRow?.warnings ?? []}
        onOpenChange={(open) => !open && setEditingId(null)}
        onSave={updateItem}
      />

      <ConfirmDialog
        open={confirmOpen}
        onOpenChange={setConfirmOpen}
        title="Importar lançamentos?"
        description={[
          `${selectedRows.length} ${selectedRows.length === 1 ? "lançamento será cadastrado" : "lançamentos serão cadastrados"} no cartão${card ? ` ${card.name}` : ""}, somando ${formatMonetaryValue(selectedTotal)}.`,
          installmentPurchases.length > 0
            ? `${installmentPurchases.length === 1 ? "1 compra parcelada também cria" : `${installmentPurchases.length} compras parceladas também criam`} as parcelas seguintes nas próximas faturas.`
            : null,
        ]
          .filter(Boolean)
          .join(" ")}
        confirmLabel="Importar"
        onConfirm={handleConfirm}
      />

      <ConfirmDialog
        open={undoOpen}
        onOpenChange={setUndoOpen}
        title="Desfazer alterações?"
        description="Titulares, seleções e edições voltam a ser os sugeridos na leitura da fatura."
        confirmLabel="Desfazer"
        onConfirm={handleUndo}
      />

      {card && (
        <CreditCardForm open={cardFormOpen} onOpenChange={setCardFormOpen} editingCard={card} />
      )}
    </div>
  );
}
