"use client";

import ReactMarkdown from "react-markdown";
import { useLegalDocument } from "@/hooks/use-legal-document";
import { LEGAL_TERMS_VERSION } from "@/config/legal";

export default function TermsOfUsePage() {
  const { data, isLoading, isError } = useLegalDocument("termos");

  return (
    <main className="mx-auto max-w-3xl space-y-6 px-4 py-10">
      {isLoading && (
        <div className="flex items-center justify-center py-20">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-primary border-t-transparent" />
        </div>
      )}

      {isError && (
        <div className="rounded-lg border border-destructive/30 bg-destructive/10 px-4 py-6 text-center text-sm text-destructive">
          Nao foi possivel carregar o documento. Tente novamente mais tarde.
        </div>
      )}

      {data && (
        <>
          <header className="space-y-1">
            <h1 className="text-2xl font-semibold">{data.title}</h1>
            <p className="text-sm text-muted-foreground">
              Versao {data.version} &mdash; Ultima atualizacao:{" "}
              {new Intl.DateTimeFormat("pt-BR", { dateStyle: "long" }).format(
                new Date(data.lastUpdatedAt)
              )}
            </p>
          </header>

          <article className="prose prose-neutral dark:prose-invert max-w-none text-sm leading-relaxed">
            <ReactMarkdown>{data.content}</ReactMarkdown>
          </article>
        </>
      )}

      {!data && !isLoading && !isError && (
        <p className="text-sm text-muted-foreground">
          Versao {LEGAL_TERMS_VERSION} &mdash; Conteudo em breve.
        </p>
      )}
    </main>
  );
}
