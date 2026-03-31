"use client";

import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";

interface InstallmentModalProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  description: string;
  totalAmount: number;
  installmentCount: number;
  onConfirm: () => void;
  isPending?: boolean;
}

export function InstallmentModal({
  open,
  onOpenChange,
  description,
  totalAmount,
  installmentCount,
  onConfirm,
  isPending,
}: InstallmentModalProps) {
  const perInstallment = totalAmount / installmentCount;
  const fmt = (v: number) =>
    new Intl.NumberFormat("pt-BR", {
      style: "currency",
      currency: "BRL",
    }).format(v);

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="sm:max-w-sm">
        <DialogHeader>
          <DialogTitle>Confirmar Parcelamento</DialogTitle>
          <DialogDescription>
            Revise os dados antes de confirmar.
          </DialogDescription>
        </DialogHeader>

        <div className="space-y-3 text-sm">
          <div className="flex justify-between">
            <span className="text-muted-foreground">Descricao</span>
            <span className="font-medium">{description}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Valor total</span>
            <span className="font-medium">{fmt(totalAmount)}</span>
          </div>
          <div className="flex justify-between">
            <span className="text-muted-foreground">Parcelas</span>
            <span className="font-medium">
              {installmentCount}x de {fmt(perInstallment)}
            </span>
          </div>
        </div>

        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>
            Cancelar
          </Button>
          <Button onClick={onConfirm} disabled={isPending}>
            {isPending ? "Processando..." : "Confirmar"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
