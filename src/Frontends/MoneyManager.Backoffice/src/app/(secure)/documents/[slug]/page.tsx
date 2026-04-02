"use client";

import dynamic from "next/dynamic";
import { useEffect, useState, useCallback } from "react";
import { useParams, useRouter } from "next/navigation";
import {
  getLegalDocument,
  updateLegalDocument,
  type LegalDocumentDetail,
  type UpdateLegalDocumentRequest,
} from "@/lib/admin-api";

const MDEditor = dynamic(() => import("@uiw/react-md-editor"), { ssr: false });

export default function DocumentEditorPage() {
  const params = useParams<{ slug: string }>();
  const router = useRouter();
  const slug = params.slug;

  const [doc, setDoc] = useState<LegalDocumentDetail | null>(null);
  const [title, setTitle] = useState("");
  const [content, setContent] = useState("");
  const [version, setVersion] = useState("");
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [saveMessage, setSaveMessage] = useState<string | null>(null);
  const [saveError, setSaveError] = useState<string | null>(null);

  useEffect(() => {
    if (!slug) return;
    setIsLoading(true);
    getLegalDocument(slug)
      .then((data) => {
        setDoc(data);
        setTitle(data.title);
        setContent(data.content);
        setVersion(data.version);
      })
      .catch((err: Error) => setLoadError(err.message))
      .finally(() => setIsLoading(false));
  }, [slug]);

  const handleSave = useCallback(async () => {
    if (!slug) return;

    const justificativa = window.prompt("Informe a justificativa para a alteracao (minimo 10 caracteres):", "");
    if (!justificativa || justificativa.trim().length < 10) {
      setSaveError("Justificativa obrigatoria com pelo menos 10 caracteres.");
      return;
    }

    setIsSaving(true);
    setSaveMessage(null);
    setSaveError(null);

    const body: UpdateLegalDocumentRequest = {
      title: title.trim(),
      content,
      version: version.trim(),
    };

    try {
      const updated = await updateLegalDocument(slug, body);
      setDoc(updated);
      setSaveMessage("Documento salvo com sucesso.");
    } catch (err) {
      setSaveError(err instanceof Error ? err.message : "Erro ao salvar documento.");
    } finally {
      setIsSaving(false);
    }
  }, [slug, title, content, version]);

  const handleCancel = useCallback(() => {
    if (doc) {
      setTitle(doc.title);
      setContent(doc.content);
      setVersion(doc.version);
      setSaveMessage(null);
      setSaveError(null);
    }
  }, [doc]);

  if (isLoading) return <p>Carregando...</p>;

  if (loadError) {
    return (
      <div className="card" style={{ borderColor: "var(--color-error, #c0392b)" }}>
        <p style={{ color: "var(--color-error, #c0392b)" }}>Erro ao carregar documento: {loadError}</p>
        <button className="btn" onClick={() => router.push("/documents")} style={{ marginTop: 12 }}>
          Voltar
        </button>
      </div>
    );
  }

  return (
    <div className="stack">
      <div style={{ display: "flex", alignItems: "center", gap: 12 }}>
        <button className="btn" onClick={() => router.push("/documents")}>
          &larr; Voltar
        </button>
        <h2 style={{ margin: 0 }}>Editar Documento: {slug}</h2>
      </div>

      <div className="card">
        <div className="form-grid">
          <div className="field">
            <label htmlFor="doc-title">Titulo</label>
            <input
              id="doc-title"
              type="text"
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              placeholder="Ex: Termos de Uso"
            />
          </div>

          <div className="field">
            <label htmlFor="doc-version">Versao</label>
            <input
              id="doc-version"
              type="text"
              value={version}
              onChange={(e) => setVersion(e.target.value)}
              placeholder="Ex: 2026-04"
            />
          </div>
        </div>
      </div>

      <div className="card">
        <label style={{ display: "block", marginBottom: 8, fontWeight: 500 }}>Conteudo (Markdown)</label>
        <div data-color-mode="light">
          <MDEditor
            value={content}
            onChange={(val) => setContent(val ?? "")}
            height={500}
            preview="live"
          />
        </div>
      </div>

      {saveMessage && (
        <div className="card" style={{ borderColor: "var(--color-success, #27ae60)" }}>
          <p style={{ color: "var(--color-success, #27ae60)", margin: 0 }}>{saveMessage}</p>
        </div>
      )}

      {saveError && (
        <div className="card" style={{ borderColor: "var(--color-error, #c0392b)" }}>
          <p style={{ color: "var(--color-error, #c0392b)", margin: 0 }}>{saveError}</p>
        </div>
      )}

      <div style={{ display: "flex", gap: 12 }}>
        <button className="btn" onClick={handleSave} disabled={isSaving}>
          {isSaving ? "Salvando..." : "Salvar"}
        </button>
        <button className="btn" onClick={handleCancel} disabled={isSaving}>
          Cancelar
        </button>
      </div>

      {doc && (
        <p style={{ fontSize: "0.75rem", color: "var(--muted)" }}>
          Ultima atualizacao:{" "}
          {new Intl.DateTimeFormat("pt-BR", { dateStyle: "short", timeStyle: "short" }).format(
            new Date(doc.lastUpdatedAt)
          )}{" "}
          por {doc.updatedBy}
        </p>
      )}
    </div>
  );
}
