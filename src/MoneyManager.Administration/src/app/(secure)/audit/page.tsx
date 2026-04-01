"use client";

import { useEffect, useState } from "react";
import { getAuditActions, getMonthlyAuditReport, type AuditActionItem, type AdminMonthlyAuditReport } from "@/lib/admin-api";

const ACTION_OPTIONS = [
  "reconcile-credit-cards",
  "recalculate-invoices",
  "create-missing-open-invoices",
  "migrate-credit-card-invoices",
];

export default function AuditPage() {
  const [tab, setTab] = useState<"recent" | "monthly">("recent");
  
  // Recent tab
  const [items, setItems] = useState<AuditActionItem[]>([]);
  const [targetUserId, setTargetUserId] = useState("");
  const [action, setAction] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  // Monthly tab
  const [report, setReport] = useState<AdminMonthlyAuditReport | null>(null);
  const [reportError, setReportError] = useState<string | null>(null);
  const [isLoadingReport, setIsLoadingReport] = useState(false);
  const [reportYear, setReportYear] = useState(new Date().getFullYear());
  const [reportMonth, setReportMonth] = useState(new Date().getMonth() + 1);

  async function loadAuditData(currentTargetUserId?: string, currentAction?: string) {
    setIsLoading(true);
    setError(null);

    try {
      const data = await getAuditActions(50, currentTargetUserId, currentAction);
      setItems(data);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Falha ao carregar auditoria");
      setItems([]);
    } finally {
      setIsLoading(false);
    }
  }

  async function loadMonthlyReport(year: number, month: number) {
    setIsLoadingReport(true);
    setReportError(null);
    setReport(null);

    try {
      const data = await getMonthlyAuditReport(year, month);
      setReport(data);
    } catch (requestError) {
      setReportError(requestError instanceof Error ? requestError.message : "Falha ao carregar relatório");
    } finally {
      setIsLoadingReport(false);
    }
  }

  useEffect(() => {
    loadAuditData();
  }, []);

  return (
    <section className="stack">
      <article className="card">
        <h2>Auditoria</h2>
        <p className="muted">Consulta das ultimas acoes administrativas executadas pelo portal.</p>
        
        <div className="tabs">
          <button
            className={`tab ${tab === "recent" ? "active" : ""}`}
            onClick={() => setTab("recent")}
          >
            Eventos Recentes
          </button>
          <button
            className={`tab ${tab === "monthly" ? "active" : ""}`}
            onClick={() => setTab("monthly")}
          >
            Relatório Mensal
          </button>
        </div>
      </article>

      {tab === "recent" && (
        <>
          <article className="card">
            <div className="form-grid">
              <label className="field">
                <span>Filtrar por Target User ID</span>
                <input
                  placeholder="Opcional"
                  value={targetUserId}
                  onChange={(event) => setTargetUserId(event.target.value)}
                />
              </label>
              <label className="field">
                <span>Filtrar por Acao</span>
                <select value={action} onChange={(event) => setAction(event.target.value)}>
                  <option value="">Todas</option>
                  {ACTION_OPTIONS.map((option) => (
                    <option key={option} value={option}>
                      {option}
                    </option>
                  ))}
                </select>
              </label>
              <div className="actions">
                <button
                  className="btn btn-primary"
                  type="button"
                  disabled={isLoading}
                  onClick={() => loadAuditData(targetUserId.trim() || undefined, action || undefined)}
                >
                  {isLoading ? "Carregando..." : "Aplicar Filtros"}
                </button>
                <button
                  className="btn"
                  type="button"
                  disabled={isLoading}
                  onClick={() => {
                    setTargetUserId("");
                    setAction("");
                    loadAuditData();
                  }}
                >
                  Limpar
                </button>
              </div>
              {error && <p className="error">{error}</p>}
            </div>
          </article>

          <article className="card">
            <h2>Eventos Recentes</h2>
            {isLoading && <p className="muted">Carregando auditoria...</p>}
            {!isLoading && !error && items.length === 0 && <p className="muted">Nenhum evento encontrado.</p>}
            {!isLoading && items.length > 0 && (
              <div className="table-wrap">
                <table className="audit-table">
                  <thead>
                    <tr>
                      <th>Quando</th>
                      <th>Acao</th>
                      <th>Operador</th>
                      <th>Target User</th>
                      <th>Status</th>
                      <th>Detalhes</th>
                    </tr>
                  </thead>
                  <tbody>
                    {items.map((item) => (
                      <tr key={item.id}>
                        <td>{new Date(item.createdAtUtc).toLocaleString("pt-BR")}</td>
                        <td className="code-inline">{item.action}</td>
                        <td>{item.operatorUsername}</td>
                        <td className="code-inline">{item.targetUserId}</td>
                        <td>
                          <span className={`status-pill ${item.isSuccess ? "success" : "failure"}`}>
                            {item.isSuccess ? "Sucesso" : "Falha"}
                          </span>
                        </td>
                        <td>
                          {item.errorMessage ? (
                            <span className="error">{item.errorMessage}</span>
                          ) : (
                            <span className="muted">Sem erro</span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </article>
        </>
      )}

      {tab === "monthly" && (
        <>
          <article className="card">
            <div className="form-grid">
              <label className="field">
                <span>Ano</span>
                <input
                  type="number"
                  min="2000"
                  max={new Date().getFullYear() + 1}
                  value={reportYear}
                  onChange={(event) => setReportYear(parseInt(event.target.value) || new Date().getFullYear())}
                />
              </label>
              <label className="field">
                <span>Mês</span>
                <select value={reportMonth} onChange={(event) => setReportMonth(parseInt(event.target.value))}>
                  {[1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12].map((m) => (
                    <option key={m} value={m}>
                      {new Date(reportYear, m - 1).toLocaleString("pt-BR", { month: "long" })}
                    </option>
                  ))}
                </select>
              </label>
              <div className="actions">
                <button
                  className="btn btn-primary"
                  type="button"
                  disabled={isLoadingReport}
                  onClick={() => loadMonthlyReport(reportYear, reportMonth)}
                >
                  {isLoadingReport ? "Carregando..." : "Gerar Relatório"}
                </button>
              </div>
              {reportError && <p className="error">{reportError}</p>}
            </div>
          </article>

          {report && (
            <>
              <article className="card">
                <h2>Resumo - {new Date(report.year, report.month - 1).toLocaleString("pt-BR", { month: "long", year: "numeric" })}</h2>
                <div className="summary-grid">
                  <div className="summary-item">
                    <h3>Total de Ações</h3>
                    <p className="value">{report.totalActions}</p>
                  </div>
                  <div className="summary-item">
                    <h3>Bem-sucedidas</h3>
                    <p className="value success">{report.successfulActions}</p>
                  </div>
                  <div className="summary-item">
                    <h3>Falhadas</h3>
                    <p className="value failure">{report.failedActions}</p>
                  </div>
                  <div className="summary-item">
                    <h3>Taxa de Sucesso</h3>
                    <p className="value">{report.successRate.toFixed(1)}%</p>
                  </div>
                  <div className="summary-item">
                    <h3>Operadores Únicos</h3>
                    <p className="value">{report.uniqueOperators}</p>
                  </div>
                  <div className="summary-item">
                    <h3>Usuários Afetados</h3>
                    <p className="value">{report.uniqueTargetUsers}</p>
                  </div>
                </div>
              </article>

              <article className="card">
                <h2>Ações por Tipo</h2>
                {Object.keys(report.actionCounts).length === 0 ? (
                  <p className="muted">Nenhuma ação registrada neste período.</p>
                ) : (
                  <div className="table-wrap">
                    <table className="stats-table">
                      <thead>
                        <tr>
                          <th>Tipo de Ação</th>
                          <th>Total</th>
                        </tr>
                      </thead>
                      <tbody>
                        {Object.entries(report.actionCounts).map(([actionType, count]) => (
                          <tr key={actionType}>
                            <td className="code-inline">{actionType}</td>
                            <td>{count}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </article>

              <article className="card">
                <h2>Top 10 Ações Recentes</h2>
                {report.topActions.length === 0 ? (
                  <p className="muted">Nenhuma ação encontrada.</p>
                ) : (
                  <div className="table-wrap">
                    <table className="audit-table">
                      <thead>
                        <tr>
                          <th>Quando</th>
                          <th>Acao</th>
                          <th>Operador</th>
                          <th>Target User</th>
                          <th>Status</th>
                        </tr>
                      </thead>
                      <tbody>
                        {report.topActions.map((item) => (
                          <tr key={item.id}>
                            <td>{new Date(item.createdAtUtc).toLocaleString("pt-BR")}</td>
                            <td className="code-inline">{item.action}</td>
                            <td>{item.operatorUsername}</td>
                            <td className="code-inline">{item.targetUserId}</td>
                            <td>
                              <span className={`status-pill ${item.isSuccess ? "success" : "failure"}`}>
                                {item.isSuccess ? "Sucesso" : "Falha"}
                              </span>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </article>
            </>
          )}

          {!report && !isLoadingReport && !reportError && (
            <article className="card">
              <p className="muted">Selecione um mês e ano para gerar o relatório.</p>
            </article>
          )}
        </>
      )}
    </section>
  );
}