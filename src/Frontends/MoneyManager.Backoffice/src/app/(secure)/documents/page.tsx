"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import { getLegalDocuments, type LegalDocumentSummary } from "@/lib/admin-api";

export default function DocumentsPage() {
  const [documents, setDocuments] = useState<LegalDocumentSummary[]>([]);
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    getLegalDocuments()
      .then(setDocuments)
      .catch((err: Error) => setError(err.message))
      .finally(() => setIsLoading(false));
  }, []);

  function formatDate(value: string): string {
    return new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(new Date(value));
  }

  return (
    <div className="stack">
      <h2>Documentos Legais</h2>

      {isLoading && <p>Carregando...</p>}

      {error && (
        <div className="card" style={{ borderColor: "var(--color-error, #c0392b)" }}>
          <p style={{ color: "var(--color-error, #c0392b)" }}>Erro: {error}</p>
        </div>
      )}

      {!isLoading && !error && (
        <div className="card">
          <table style={{ width: "100%", borderCollapse: "collapse" }}>
            <thead>
              <tr style={{ textAlign: "left", borderBottom: "1px solid var(--border)" }}>
                <th style={{ padding: "8px 12px" }}>Slug</th>
                <th style={{ padding: "8px 12px" }}>Titulo</th>
                <th style={{ padding: "8px 12px" }}>Versao</th>
                <th style={{ padding: "8px 12px" }}>Ultima Atualizacao</th>
                <th style={{ padding: "8px 12px" }}>Atualizado Por</th>
                <th style={{ padding: "8px 12px" }}>Acoes</th>
              </tr>
            </thead>
            <tbody>
              {documents.map((doc) => (
                <tr key={doc.slug} style={{ borderBottom: "1px solid var(--border)" }}>
                  <td style={{ padding: "8px 12px", fontFamily: "monospace" }}>{doc.slug}</td>
                  <td style={{ padding: "8px 12px" }}>{doc.title}</td>
                  <td style={{ padding: "8px 12px" }}>{doc.version}</td>
                  <td style={{ padding: "8px 12px" }}>{formatDate(doc.lastUpdatedAt)}</td>
                  <td style={{ padding: "8px 12px" }}>{doc.updatedBy}</td>
                  <td style={{ padding: "8px 12px" }}>
                    <Link href={`/documents/${doc.slug}`} className="btn">
                      Editar
                    </Link>
                  </td>
                </tr>
              ))}

              {documents.length === 0 && (
                <tr>
                  <td colSpan={6} style={{ padding: "16px 12px", textAlign: "center", color: "var(--muted)" }}>
                    Nenhum documento encontrado.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
