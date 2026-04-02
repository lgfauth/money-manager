"use client";

import { useEffect, useState } from "react";
import { getSystemStatus, type SystemStatus } from "@/lib/admin-api";

export default function OverviewPage() {
  const [data, setData] = useState<SystemStatus | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    getSystemStatus()
      .then(setData)
      .catch((err: Error) => setError(err.message));
  }, []);

  return (
    <section className="card">
      <h2>Saude do Sistema</h2>
      {error && <p className="error">{error}</p>}
      {!data && !error && <p className="muted">Carregando...</p>}
      {data && (
        <div className="grid">
          <div className="card">
            <strong>API</strong>
            <p>{data.apiStatus}</p>
          </div>
          <div className="card">
            <strong>MongoDB</strong>
            <p>{data.mongoStatus}</p>
          </div>
          <div className="card">
            <strong>Worker</strong>
            <p>{data.workerStatus}</p>
          </div>
          <div className="card">
            <strong>Environment</strong>
            <p>{data.environment}</p>
          </div>
        </div>
      )}
    </section>
  );
}
