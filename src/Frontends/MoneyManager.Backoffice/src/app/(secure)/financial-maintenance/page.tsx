"use client";

import { useState } from "react";
import {
  createMissingOpenInvoices,
  fullResyncBankConnections,
  migrateCreditCardInvoices,
  migrateOpenBankingCategories,
  recalculateInvoices,
  recategorizeOpenBankingTransactions,
  reconcileCreditCards,
  type AdminCommandResult,
  type BankFullResyncResult,
  type FinancialMaintenanceSummary,
  type OpenBankingCategoriesMigrationResult,
  type OpenBankingRecategorizeResult,
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

  const [categoriesReason, setCategoriesReason] = useState("");
  const [categoriesRunning, setCategoriesRunning] = useState(false);
  const [categoriesError, setCategoriesError] = useState<string | null>(null);
  const [categoriesResult, setCategoriesResult] = useState<OpenBankingCategoriesMigrationResult | null>(null);

  const [recategorizeUserId, setRecategorizeUserId] = useState("");
  const [recategorizeReason, setRecategorizeReason] = useState("");
  const [recategorizeRunning, setRecategorizeRunning] = useState(false);
  const [recategorizeError, setRecategorizeError] = useState<string | null>(null);
  const [recategorizeResult, setRecategorizeResult] = useState<OpenBankingRecategorizeResult | null>(null);

  async function runOpenBankingRecategorization() {
    if (recategorizeReason.trim().length < 10) {
      setRecategorizeError("Informe uma justificativa com pelo menos 10 caracteres.");
      return;
    }

    setRecategorizeRunning(true);
    setRecategorizeError(null);

    try {
      const response = await recategorizeOpenBankingTransactions(
        recategorizeReason.trim(),
        recategorizeUserId.trim() || undefined,
      );
      setRecategorizeResult(response);
    } catch (requestError) {
      setRecategorizeError(
        requestError instanceof Error ? requestError.message : "Falha ao executar a recategorizacao",
      );
      setRecategorizeResult(null);
    } finally {
      setRecategorizeRunning(false);
    }
  }

  const [resyncUserId, setResyncUserId] = useState("");
  const [resyncReason, setResyncReason] = useState("");
  const [resyncRunning, setResyncRunning] = useState(false);
  const [resyncError, setResyncError] = useState<string | null>(null);
  const [resyncResult, setResyncResult] = useState<BankFullResyncResult | null>(null);

  async function runFullResync() {
    if (resyncReason.trim().length < 10) {
      setResyncError("Informe uma justificativa com pelo menos 10 caracteres.");
      return;
    }

    setResyncRunning(true);
    setResyncError(null);

    try {
      const response = await fullResyncBankConnections(
        resyncReason.trim(),
        resyncUserId.trim() || undefined,
      );
      setResyncResult(response);
    } catch (requestError) {
      setResyncError(
        requestError instanceof Error ? requestError.message : "Falha ao executar o re-sync completo",
      );
      setResyncResult(null);
    } finally {
      setResyncRunning(false);
    }
  }

  async function runOpenBankingCategoriesMigration() {
    if (categoriesReason.trim().length < 10) {
      setCategoriesError("Informe uma justificativa com pelo menos 10 caracteres.");
      return;
    }

    setCategoriesRunning(true);
    setCategoriesError(null);

    try {
      const response = await migrateOpenBankingCategories(categoriesReason.trim());
      setCategoriesResult(response);
    } catch (requestError) {
      setCategoriesError(
        requestError instanceof Error ? requestError.message : "Falha ao executar a migracao de categorias",
      );
      setCategoriesResult(null);
    } finally {
      setCategoriesRunning(false);
    }
  }

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
        <h2>Categorias Open Banking</h2>
        <p className="muted">
          Migracao retroativa e global: migra as categorias para o padrao Open Banking (130 categorias Pluggy)
          de todos os usuarios com contas/cartoes vinculados ao Banco MCP que ainda nao foram migrados, e
          recategoriza as transacoes sincronizadas sem categoria. Operacao idempotente — usuarios ja migrados
          sao pulados.
        </p>
        <div className="form-grid">
          <label className="field">
            <span>Justificativa</span>
            <textarea
              placeholder="Motivo operacional da acao (minimo 10 caracteres)"
              value={categoriesReason}
              onChange={(event) => setCategoriesReason(event.target.value)}
            />
          </label>
          <div className="actions">
            <button
              className="btn btn-primary"
              type="button"
              disabled={categoriesRunning}
              onClick={() => runOpenBankingCategoriesMigration()}
            >
              {categoriesRunning ? "Executando..." : "Migrar Categorias Open Banking"}
            </button>
          </div>
          {categoriesError && <p className="error">{categoriesError}</p>}
        </div>
        {categoriesResult && (
          <div className="stack">
            <div className="grid-compact">
              <div className="card">
                <strong>Usuarios elegiveis</strong>
                <p>{categoriesResult.totalCandidates}</p>
              </div>
              <div className="card">
                <strong>Migrados</strong>
                <p>{categoriesResult.migratedCount}</p>
              </div>
              <div className="card">
                <strong>Pulados</strong>
                <p>{categoriesResult.skippedCount}</p>
              </div>
              <div className="card">
                <strong>Erros</strong>
                <p>{categoriesResult.errors.length}</p>
              </div>
            </div>
            {categoriesResult.errors.length > 0 && (
              <div className="result-box">
                <pre>{JSON.stringify(categoriesResult.errors, null, 2)}</pre>
              </div>
            )}
          </div>
        )}
      </article>

      <article className="card">
        <h2>Recategorizar Transacoes Sincronizadas</h2>
        <p className="muted">
          Ajusta a categoria das transacoes ja sincronizadas (banco e cartao) que estao sem categoria e
          possuem o categoryId de origem do Open Banking persistido, usando as categorias Open Banking do
          usuario. Roda apenas para usuarios ja migrados — os demais sao pulados. Deixe o Target User ID
          vazio para processar todos os usuarios elegiveis.
        </p>
        <div className="form-grid">
          <label className="field">
            <span>Target User ID (opcional)</span>
            <input
              placeholder="Vazio = todos os usuarios elegiveis"
              value={recategorizeUserId}
              onChange={(event) => setRecategorizeUserId(event.target.value)}
            />
          </label>
          <label className="field">
            <span>Justificativa</span>
            <textarea
              placeholder="Motivo operacional da acao (minimo 10 caracteres)"
              value={recategorizeReason}
              onChange={(event) => setRecategorizeReason(event.target.value)}
            />
          </label>
          <div className="actions">
            <button
              className="btn btn-primary"
              type="button"
              disabled={recategorizeRunning}
              onClick={() => runOpenBankingRecategorization()}
            >
              {recategorizeRunning ? "Executando..." : "Recategorizar Transacoes"}
            </button>
          </div>
          {recategorizeError && <p className="error">{recategorizeError}</p>}
        </div>
        {recategorizeResult && (
          <div className="stack">
            <div className="grid-compact">
              <div className="card">
                <strong>Usuarios elegiveis</strong>
                <p>{recategorizeResult.totalCandidates}</p>
              </div>
              <div className="card">
                <strong>Processados</strong>
                <p>{recategorizeResult.usersProcessed}</p>
              </div>
              <div className="card">
                <strong>Pulados (nao migrados)</strong>
                <p>{recategorizeResult.skippedCount}</p>
              </div>
              <div className="card">
                <strong>Transacoes recategorizadas</strong>
                <p>{recategorizeResult.recategorizedTransactions}</p>
              </div>
              <div className="card">
                <strong>Erros</strong>
                <p>{recategorizeResult.errors.length}</p>
              </div>
            </div>
            {recategorizeResult.errors.length > 0 && (
              <div className="result-box">
                <pre>{JSON.stringify(recategorizeResult.errors, null, 2)}</pre>
              </div>
            )}
          </div>
        )}
      </article>

      <article className="card">
        <h2>Re-sync Completo (Open Banking)</h2>
        <p className="muted">
          Reseta a janela de sincronizacao das contas vinculadas para que o proximo sync re-busque as
          transacoes desde o inicio (CutoffDate, ate 12 meses) e preencha a categoria das transacoes
          importadas antes da resolucao de categorias (backfill — nunca sobrescreve categoria existente).
          Ao final, o BankSyncWorker e disparado imediatamente. Custo: re-busca de dados na API do Banco
          MCP para as conexoes processadas. Deixe o Target User ID vazio para todas as conexoes ativas.
        </p>
        <div className="form-grid">
          <label className="field">
            <span>Target User ID (opcional)</span>
            <input
              placeholder="Vazio = todas as conexoes ativas"
              value={resyncUserId}
              onChange={(event) => setResyncUserId(event.target.value)}
            />
          </label>
          <label className="field">
            <span>Justificativa</span>
            <textarea
              placeholder="Motivo operacional da acao (minimo 10 caracteres)"
              value={resyncReason}
              onChange={(event) => setResyncReason(event.target.value)}
            />
          </label>
          <div className="actions">
            <button
              className="btn btn-danger"
              type="button"
              disabled={resyncRunning}
              onClick={() => runFullResync()}
            >
              {resyncRunning ? "Executando..." : "Re-sync Completo"}
            </button>
          </div>
          {resyncError && <p className="error">{resyncError}</p>}
        </div>
        {resyncResult && (
          <div className="stack">
            <div className="grid-compact">
              <div className="card">
                <strong>Conexoes ativas</strong>
                <p>{resyncResult.totalConnections}</p>
              </div>
              <div className="card">
                <strong>Conexoes resetadas</strong>
                <p>{resyncResult.connectionsReset}</p>
              </div>
              <div className="card">
                <strong>Contas resetadas</strong>
                <p>{resyncResult.accountsReset}</p>
              </div>
              <div className="card">
                <strong>Sync disparado</strong>
                <p>{resyncResult.alreadyQueued ? "Ja havia comando na fila" : "Sim"}</p>
              </div>
            </div>
          </div>
        )}
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