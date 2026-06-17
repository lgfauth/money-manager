"use client";

import { useState } from "react";
import { X, CalendarCheck } from "lucide-react";
import { useDismissSnapshot } from "@/hooks/use-financial-health";
import { CheckinModal } from "@/components/financial-health/checkin-modal";
import { Button } from "@/components/ui/button";
import type { SnapshotStatus } from "@/types/financial-health";

interface CheckinBannerProps {
  status: SnapshotStatus;
}

export function CheckinBanner({ status }: CheckinBannerProps) {
  const [localDismissed, setLocalDismissed] = useState(false);
  const [showModal, setShowModal] = useState(false);
  const dismissSnapshot = useDismissSnapshot();

  if (!status.showBanner || localDismissed) return null;

  const [year, month] = (status.referenceMonth ?? "").split("-").map(Number);

  const handleDismiss = () => {
    dismissSnapshot.mutate(
      { year, month },
      { onSuccess: () => setLocalDismissed(true) }
    );
  };

  return (
    <>
      <div className="flex items-center justify-between gap-4 rounded-xl bg-primary/10 border border-primary/20 px-4 py-3">
        <div className="flex items-center gap-3">
          <CalendarCheck className="h-5 w-5 text-primary shrink-0" />
          <div>
            <p className="text-sm font-medium">
              Check-in disponível para{" "}
              <span className="font-bold">{status.referenceMonth}</span>
            </p>
            <p className="text-xs text-muted-foreground">
              Informe os saldos reais nas corretoras para manter o score preciso.
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2 shrink-0">
          <Button
            variant="ghost"
            size="sm"
            className="text-xs text-muted-foreground"
            onClick={handleDismiss}
            disabled={dismissSnapshot.isPending}
          >
            Ignorar este mês
          </Button>
          <Button size="sm" onClick={() => setShowModal(true)}>
            Fazer check-in
          </Button>
          <button
            onClick={() => setLocalDismissed(true)}
            className="ml-1 rounded p-1 text-muted-foreground hover:text-foreground transition-colors"
            aria-label="Fechar banner"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
      </div>

      {showModal && (
        <CheckinModal
          open={showModal}
          onClose={() => setShowModal(false)}
          referenceMonth={status.referenceMonth ?? ""}
          pendingBuckets={status.pendingBuckets}
        />
      )}
    </>
  );
}
