"use client";

import { useEffect, useState } from "react";
import {
  getJobHistory,
  getJobsHistory,
  pauseJob,
  resumeJob,
  runJobNow,
  updateJobSchedule,
  type JobExecutionHistoryEntry,
  type JobHistoryItem,
} from "@/lib/admin-api";

export default function JobsPage() {
  const [items, setItems] = useState<JobHistoryItem[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [selectedJob, setSelectedJob] = useState<string | null>(null);
  const [historyItems, setHistoryItems] = useState<JobExecutionHistoryEntry[]>([]);
  const [historyError, setHistoryError] = useState<string | null>(null);
  const [isHistoryLoading, setIsHistoryLoading] = useState(false);
  const [runningJobName, setRunningJobName] = useState<string | null>(null);
  const [runNowMessage, setRunNowMessage] = useState<string | null>(null);
  const [scheduleHour, setScheduleHour] = useState("8");
  const [scheduleMinute, setScheduleMinute] = useState("0");
  const [scheduleLoopDelay, setScheduleLoopDelay] = useState("30");
  const [scheduleTimeZone, setScheduleTimeZone] = useState("E. South America Standard Time");

  useEffect(() => {
    getJobsHistory()
      .then(setItems)
      .catch((err: Error) => setError(err.message));
  }, []);

  async function handleLoadHistory(jobName: string) {
    setSelectedJob(jobName);
    setIsHistoryLoading(true);
    setHistoryError(null);

    try {
      const data = await getJobHistory(jobName, 10);
      setHistoryItems(data);
    } catch (requestError) {
      setHistoryError(requestError instanceof Error ? requestError.message : "Falha ao carregar historico");
      setHistoryItems([]);
    } finally {
      setIsHistoryLoading(false);
    }
  }

  async function handleRunNow(jobName: string) {
    if (!window.confirm(`Confirma executar agora o job ${jobName}?`)) {
      return;
    }

    const reason = window.prompt("Informe a justificativa (minimo 10 caracteres):", "");
    if (!reason || reason.trim().length < 10) {
      setError("Justificativa obrigatoria com pelo menos 10 caracteres.");
      return;
    }

    setRunningJobName(jobName);
    setRunNowMessage(null);
    setError(null);

    try {
      const response = await runJobNow(jobName, reason.trim());
      setRunNowMessage(
        response.alreadyQueued
          ? `Comando run-now ja estava na fila para ${response.jobName} (id ${response.commandId.slice(0, 10)}...).`
          : `Comando enfileirado para ${response.jobName} (id ${response.commandId.slice(0, 10)}...) em ${new Date(response.requestedAtUtc).toLocaleString("pt-BR")}.`,
      );

      const refreshedItems = await getJobsHistory();
      setItems(refreshedItems);
      await handleLoadHistory(jobName);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Falha ao executar run-now");
    } finally {
      setRunningJobName(null);
    }
  }

  async function handlePause(jobName: string) {
    if (!window.confirm(`Confirma pausar o job ${jobName}?`)) {
      return;
    }

    const reason = window.prompt("Informe a justificativa (minimo 10 caracteres):", "");
    if (!reason || reason.trim().length < 10) {
      setError("Justificativa obrigatoria com pelo menos 10 caracteres.");
      return;
    }

    setRunningJobName(jobName);
    setRunNowMessage(null);
    setError(null);

    try {
      const response = await pauseJob(jobName, reason.trim());
      setRunNowMessage(
        response.alreadyQueued
          ? `Comando pause ja estava na fila para ${response.jobName}.`
          : `Comando pause enfileirado para ${response.jobName}.`,
      );

      const refreshedItems = await getJobsHistory();
      setItems(refreshedItems);
      await handleLoadHistory(jobName);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Falha ao executar pause");
    } finally {
      setRunningJobName(null);
    }
  }

  async function handleResume(jobName: string) {
    if (!window.confirm(`Confirma retomar o job ${jobName}?`)) {
      return;
    }

    const reason = window.prompt("Informe a justificativa (minimo 10 caracteres):", "");
    if (!reason || reason.trim().length < 10) {
      setError("Justificativa obrigatoria com pelo menos 10 caracteres.");
      return;
    }

    setRunningJobName(jobName);
    setRunNowMessage(null);
    setError(null);

    try {
      const response = await resumeJob(jobName, reason.trim());
      setRunNowMessage(
        response.alreadyQueued
          ? `Comando resume ja estava na fila para ${response.jobName}.`
          : `Comando resume enfileirado para ${response.jobName}.`,
      );

      const refreshedItems = await getJobsHistory();
      setItems(refreshedItems);
      await handleLoadHistory(jobName);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Falha ao executar resume");
    } finally {
      setRunningJobName(null);
    }
  }

  async function handleUpdateSchedule(jobName: string) {
    if (!window.confirm(`Confirma atualizar o schedule do job ${jobName}?`)) {
      return;
    }

    const reason = window.prompt("Informe a justificativa (minimo 10 caracteres):", "");
    if (!reason || reason.trim().length < 10) {
      setError("Justificativa obrigatoria com pelo menos 10 caracteres.");
      return;
    }

    setRunningJobName(jobName);
    setRunNowMessage(null);
    setError(null);

    try {
      const hour = Number(scheduleHour);
      const minute = Number(scheduleMinute);
      const loopDelaySeconds = Number(scheduleLoopDelay);

      const response = await updateJobSchedule(jobName, {
        timeZoneId: scheduleTimeZone.trim() || undefined,
        hour,
        minute,
        loopDelaySeconds,
        reason: reason.trim(),
      });

      setRunNowMessage(
        `Schedule atualizado para ${response.jobName}: ${String(response.hour).padStart(2, "0")}:${String(response.minute).padStart(2, "0")}, delay ${response.loopDelaySeconds}s (${response.timeZoneId ?? "Local"}).`,
      );

      const refreshedItems = await getJobsHistory();
      setItems(refreshedItems);
    } catch (requestError) {
      setError(requestError instanceof Error ? requestError.message : "Falha ao atualizar schedule");
    } finally {
      setRunningJobName(null);
    }
  }

  return (
    <section className="stack">
      <article className="card">
        <h2>Historico de Jobs</h2>
        {error && <p className="error">{error}</p>}
        {runNowMessage && <p>{runNowMessage}</p>}
        {!error && items.length === 0 && <p className="muted">Carregando...</p>}
        <div className="grid">
          {items.map((item) => (
            <article className="card" key={item.jobName}>
              <strong>{item.jobName}</strong>
              <p>Status: {item.lastStatus}</p>
              <p>Ultima execucao: {item.lastRunAtUtc ? new Date(item.lastRunAtUtc).toLocaleString("pt-BR") : "N/A"}</p>
              <p>Duracao: {item.lastDurationMs ? `${item.lastDurationMs} ms` : "N/A"}</p>
              <p className="muted">{item.notes}</p>
              <div className="actions">
                <button className="btn" type="button" onClick={() => handleLoadHistory(item.jobName)}>
                  Ver execucoes
                </button>
                <button
                  className="btn btn-primary"
                  type="button"
                  disabled={runningJobName !== null}
                  onClick={() => handleRunNow(item.jobName)}
                >
                  {runningJobName === item.jobName ? "Enfileirando..." : "Executar Agora"}
                </button>
                <button
                  className="btn btn-danger"
                  type="button"
                  disabled={runningJobName !== null}
                  onClick={() => handlePause(item.jobName)}
                >
                  {runningJobName === item.jobName ? "Enfileirando..." : "Pausar"}
                </button>
                <button
                  className="btn"
                  type="button"
                  disabled={runningJobName !== null}
                  onClick={() => handleResume(item.jobName)}
                >
                  {runningJobName === item.jobName ? "Enfileirando..." : "Retomar"}
                </button>
              </div>

              <div className="form-grid">
                <label className="field">
                  <span>TimeZoneId</span>
                  <input value={scheduleTimeZone} onChange={(event) => setScheduleTimeZone(event.target.value)} />
                </label>
                <div className="grid-compact">
                  <label className="field">
                    <span>Hora</span>
                    <input value={scheduleHour} onChange={(event) => setScheduleHour(event.target.value)} />
                  </label>
                  <label className="field">
                    <span>Minuto</span>
                    <input value={scheduleMinute} onChange={(event) => setScheduleMinute(event.target.value)} />
                  </label>
                  <label className="field">
                    <span>Loop Delay (s)</span>
                    <input value={scheduleLoopDelay} onChange={(event) => setScheduleLoopDelay(event.target.value)} />
                  </label>
                </div>
                <button
                  className="btn"
                  type="button"
                  disabled={runningJobName !== null}
                  onClick={() => handleUpdateSchedule(item.jobName)}
                >
                  {runningJobName === item.jobName ? "Aplicando..." : "Atualizar Schedule"}
                </button>
              </div>
            </article>
          ))}
        </div>
      </article>

      <article className="card">
        <h2>Execucoes Recentes</h2>
        {!selectedJob && <p className="muted">Selecione um job para ver o historico recente.</p>}
        {selectedJob && isHistoryLoading && <p className="muted">Carregando historico de {selectedJob}...</p>}
        {historyError && <p className="error">{historyError}</p>}
        {selectedJob && !isHistoryLoading && !historyError && historyItems.length === 0 && (
          <p className="muted">Nenhuma execucao persistida encontrada para {selectedJob}.</p>
        )}
        {historyItems.length > 0 && (
          <div className="table-wrap">
            <table className="audit-table">
              <thead>
                <tr>
                  <th>Inicio</th>
                  <th>Status</th>
                  <th>Duracao</th>
                  <th>Worker</th>
                  <th>Correlation</th>
                  <th>Erro</th>
                </tr>
              </thead>
              <tbody>
                {historyItems.map((entry) => (
                  <tr key={entry.correlationId}>
                    <td>{new Date(entry.startedAtUtc).toLocaleString("pt-BR")}</td>
                    <td>
                      <span className={`status-pill ${entry.status === "Success" ? "success" : "failure"}`}>
                        {entry.status}
                      </span>
                    </td>
                    <td>{entry.durationMs} ms</td>
                    <td>{entry.workerName ?? entry.jobName}</td>
                    <td className="code-inline">{entry.correlationId}</td>
                    <td>{entry.errorMessage ?? "-"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </article>
    </section>
  );
}
