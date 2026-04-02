"use client";

import { useEffect, useState } from "react";
import { getMetricsSummary, type MetricsSummary } from "@/lib/admin-api";

export default function ErrorsLatencyPage() {
  const [data, setData] = useState<MetricsSummary | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getMetricsSummary()
      .then(setData)
      .catch((err: Error) => setError(err.message));
  }, []);

  return (
    <section className="card">
      <h2>Erros e Latencia</h2>
      {error && <p className="error">{error}</p>}
      {!data && !error && <p className="muted">Carregando...</p>}
      {data && (
        <div className="grid">
          <article className="card">
            <strong>HTTP 5xx (24h)</strong>
            <p>{data.http5xxCount}</p>
          </article>
          <article className="card">
            <strong>HTTP 4xx (24h)</strong>
            <p>{data.http4xxCount}</p>
          </article>
          <article className="card">
            <strong>API P95 (ms)</strong>
            <p>{data.apiP95Ms ?? "N/A"}</p>
          </article>
          <article className="card">
            <strong>Falhas de Job (24h)</strong>
            <p>{data.jobFailures}</p>
          </article>
        </div>
      )}
    </section>
  );
}
