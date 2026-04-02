"use client";

import Link from "next/link";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { Button } from "@/components/ui/button";

interface TermsConsentModalProps {
  open: boolean;
  isSubmitting?: boolean;
  termsVersion: string;
  termsOfUseUrl: string;
  privacyPolicyUrl: string;
  onAccept: () => void;
}

export function TermsConsentModal({
  open,
  isSubmitting = false,
  termsVersion,
  termsOfUseUrl,
  privacyPolicyUrl,
  onAccept,
}: TermsConsentModalProps) {
  return (
    <Dialog open={open} onOpenChange={() => {}}>
      <DialogContent showCloseButton={false} className="max-w-lg">
        <DialogHeader>
          <DialogTitle>Aceite dos Termos (LGPD)</DialogTitle>
          <DialogDescription>
            Para continuar usando o MoneyManager, voce precisa aceitar os termos
            de uso e a politica de privacidade atualizados.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-3 text-sm text-muted-foreground">
          <p>
            Ao clicar em &quot;Aceitar e continuar&quot;, voce confirma que leu e concorda
            com a versao <strong>{termsVersion}</strong> dos termos.
          </p>
          <p>
            Este aceite e registrado para fins de conformidade com a LGPD e
            podera ser consultado no seu perfil posteriormente.
          </p>
          <p>
            Leia os documentos antes de aceitar: {" "}
            <Link
              href={termsOfUseUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="font-medium text-primary underline underline-offset-4"
            >
              Termos de Uso
            </Link>{" "}
            e{" "}
            <Link
              href={privacyPolicyUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="font-medium text-primary underline underline-offset-4"
            >
              Politica de Privacidade
            </Link>
            .
          </p>
        </div>

        <DialogFooter>
          <Button onClick={onAccept} disabled={isSubmitting} className="w-full sm:w-auto">
            {isSubmitting ? "Registrando aceite..." : "Aceitar e continuar"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
