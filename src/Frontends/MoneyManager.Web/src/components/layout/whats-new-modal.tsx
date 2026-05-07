"use client";

import { useEffect, useState } from "react";
import ReactMarkdown from "react-markdown";
import { useUIStore } from "@/stores/ui-store";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

export function WhatsNewModal() {
  const { whatsNewOpen, setWhatsNewOpen } = useUIStore();
  const [content, setContent] = useState<string | null>(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    if (!whatsNewOpen || content !== null) return;
    fetch("/changelog.md")
      .then((res) => {
        if (!res.ok) throw new Error("fetch failed");
        return res.text();
      })
      .then(setContent)
      .catch(() => setError(true));
  }, [whatsNewOpen, content]);

  return (
    <Dialog open={whatsNewOpen} onOpenChange={setWhatsNewOpen}>
      <DialogContent className="sm:max-w-2xl max-h-[80vh] overflow-y-auto">
        <DialogHeader>
          <DialogTitle>O que há de novo</DialogTitle>
        </DialogHeader>

        {!content && !error && (
          <div className="flex items-center justify-center py-12">
            <div className="h-7 w-7 animate-spin rounded-full border-4 border-primary border-t-transparent" />
          </div>
        )}

        {error && (
          <p className="py-6 text-center text-sm text-muted-foreground">
            Não foi possível carregar o changelog.
          </p>
        )}

        {content && (
          <div className="prose prose-sm dark:prose-invert max-w-none">
            <ReactMarkdown>{content}</ReactMarkdown>
          </div>
        )}
      </DialogContent>
    </Dialog>
  );
}
