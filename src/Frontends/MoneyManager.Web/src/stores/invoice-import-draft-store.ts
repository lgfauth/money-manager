import { create } from "zustand";
import { createJSONStorage, persist } from "zustand/middleware";

import type { ReviewState } from "@/lib/invoice-import-review";

/** Rascunhos sem alteração há mais tempo que isso são apagados ao salvar outro. */
const DRAFT_MAX_AGE_MS = 30 * 24 * 60 * 60 * 1000;

export const INVOICE_IMPORT_DRAFT_STORAGE_KEY = "mm_invoice_import_drafts";

export interface InvoiceImportDraft {
  /** `Version` da importação no servidor quando o rascunho foi feito. */
  version: number;
  review: ReviewState;
  /** ISO 8601 da última alteração. */
  savedAt: string;
}

interface InvoiceImportDraftState {
  /** Usuário dono dos rascunhos: outro usuário no mesmo navegador não os herda. */
  userId: string | null;
  drafts: Record<string, InvoiceImportDraft>;
  saveDraft: (userId: string, importId: string, version: number, review: ReviewState) => void;
  clearDraft: (importId: string) => void;
  clearAll: () => void;
  /** Apaga tudo se os rascunhos guardados forem de outro usuário. */
  ensureOwner: (userId: string) => void;
}

/**
 * Rascunho local da revisão da importação de fatura (titulares, seleções e edições), por
 * importação, persistido no `localStorage`. Só vale para a mesma `Version` do servidor.
 * Apagado ao confirmar, ao descartar e no logout.
 */
export const useInvoiceImportDraftStore = create<InvoiceImportDraftState>()(
  persist(
    (set) => ({
      userId: null,
      drafts: {},

      saveDraft: (userId, importId, version, review) =>
        set((state) => {
          const now = Date.now();
          const drafts: Record<string, InvoiceImportDraft> = {};
          // Rascunho de outro usuário não é mantido junto
          if (state.userId === userId) {
            for (const [id, draft] of Object.entries(state.drafts)) {
              if (now - Date.parse(draft.savedAt) < DRAFT_MAX_AGE_MS) drafts[id] = draft;
            }
          }
          drafts[importId] = { version, review, savedAt: new Date(now).toISOString() };
          return { userId, drafts };
        }),

      clearDraft: (importId) =>
        set((state) => {
          if (!(importId in state.drafts)) return state;
          const drafts = { ...state.drafts };
          delete drafts[importId];
          return { drafts };
        }),

      clearAll: () => set({ userId: null, drafts: {} }),

      ensureOwner: (userId) =>
        set((state) =>
          state.userId !== null && state.userId !== userId ? { userId: null, drafts: {} } : state
        ),
    }),
    {
      name: INVOICE_IMPORT_DRAFT_STORAGE_KEY,
      storage: createJSONStorage(() => localStorage),
      partialize: (state) => ({ userId: state.userId, drafts: state.drafts }),
      version: 1,
    }
  )
);
