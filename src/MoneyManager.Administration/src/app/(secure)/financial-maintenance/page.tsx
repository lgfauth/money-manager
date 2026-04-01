"use client";

import { useState } from "react";
import {
  createMissingOpenInvoices,
  migrateCreditCardInvoices,
  recalculateInvoices,
  reconcileCreditCards,
  type AdminCommandResult,
  type FinancialMaintenanceSummary,
} from "@/lib/admin-api";

type CommandName =
  | "reconcile-credit-cards"
  | "recalculate-invoices"
  | "create-missing-open-invoices"
  | "migrate-credit-card-invoices";

export default function FinancialMaintenancePage() {
  const [targetUserId, setTargetUserId] = useState("");
  const [reason, setReason] = useState("");
  const [runningCommand, setRunningCommand] = useState<CommandName | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [result, setResult] = useState<AdminCommandResult<FinancialMaintenanceSummary> | null>(null);

  async function runCommand(command: CommandName) {
    if (!targetUserId.trim()) {
      setError("Informe o targetUserId antes de executar uma acao.");
      return;
    }

    setRunningCommand(command);
    setError(null);

    try {
      const payload = {
        targetUserId: targetUserId.trim(),
        reason: reason.trim() || undefined,
      };

      let response: AdminCommandResult<FinancialMaintenanceSummary>;

      switch (command) {
        case "reconcile-credit-cards":
          response = await reconcileCreditCards(payload);
          break;
        case "recalculate-invoices":
          response = await recalculateInvoices(payload);
          break;
        case "create-missing-open-invoices":
          response = await createMissingOpenInvoices(payload);
          break;
        case "migrate-credit-card-invoices":
          response = await migrateCreditCardInvoices(payload);
          break;
      }

      setResult(response);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Falha ao executar a acao");
      setResult(null);
    } finally {
      setRunningCommand(null);
    }
  }

  return (
    <section className="stack">
      <article className="card">
        <h2>Manutencao Financeira</h2>
        <p className="muted">
          Essas operacoes executam comandos administrativos para um usuario alvo e gravam auditoria persistente.
        </p>
        <div className="form-grid">
          <label className="field">
            <span>Target User ID</span>
            <input
              placeholder="Id do usuario a ser operado"
              value={targetUserId}
              onChange={(event) => setTargetUserId(event.target.value)}
            />
          </label>
          <label className="field">
            <span>Justificativa</span>
            <textarea
              placeholder="Motivo operacional da acao"
              value={reason}
              onChange={(event) => setReason(event.target.value)}
            />
          </label>
          <div className="actions">
            <button
              className="btn btn-primary"
              type="button"
              disabled={runningCommand !== null}
              onClick={() => runCommand("reconcile-credit-cards")}
            >
              {runningCommand === "reconcile-credit-cards" ? "Executando..." : "Reconciliar Cartoes"}
            </button>
            <button
              className="btn"
              type="button"
              disabled={runningCommand !== null}
              onClick={() => runCommand("recalculate-invoices")}
            >
              {runningCommand === "recalculate-invoices" ? "Executando..." : "Recalcular Faturas"}
            </button>
            <button
              className="btn"
              type="button"
              disabled={runningCommand !== null}
              onClick={() => runCommand("create-missing-open-invoices")}
            >
              {runningCommand === "create-missing-open-invoices" ? "Executando..." : "Criar Faturas Abertas"}
            </button>
            <button
              className="btn btn-danger"
              type="button"
              disabled={runningCommand !== null}
              onClick={() => runCommand("migrate-credit-card-invoices")}
            >
              {runningCommand === "migrate-credit-card-invoices" ? "Executando..." : "Migrar Historico"}
            </button>
          </div>
          {error && <p className="error">{error}</p>}
        </div>
      </article>

      <article className="card">
        <h2>Ultimo Resultado</h2>
        {!result && !error && <p className="muted">Nenhuma acao executada ainda.</p>}
        {result && (
          <div className="stack">
            <div className="grid-compact">
              <div className="card">
                <strong>Status</strong>
                <p>{result.success ? "Sucesso" : "Falha"}</p>
              </div>
              <div className="card">
                <strong>Mensagem</strong>
                <p>{result.message}</p>
              </div>
            </div>
            <div className="result-box">
              <pre>{JSON.stringify(result.result, null, 2)}</pre>
            </div>
          </div>
        )}
      </article>
    </section>
  );
}