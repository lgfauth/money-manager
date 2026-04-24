"use client";

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { CreditCardTransactionFormBody } from "@/components/transactions/credit-card-transaction-form-body";

interface CreditCardTransactionFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  defaultCardId?: string;
}

export function CreditCardTransactionForm({
  open,
  onOpenChange,
  defaultCardId,
}: CreditCardTransactionFormProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-md max-md:mt-[env(safe-area-inset-top,16px)] max-md:mb-[env(safe-area-inset-bottom,16px)] max-md:pt-10 max-md:pb-6">
        <DialogHeader>
          <DialogTitle>Compra no cartão</DialogTitle>
          <DialogDescription>
            Registre uma nova compra com parcelamento em até 18 vezes.
          </DialogDescription>
        </DialogHeader>

        <CreditCardTransactionFormBody
          open={open}
          defaultCardId={defaultCardId}
          onClose={() => onOpenChange(false)}
        />
      </DialogContent>
    </Dialog>
  );
}
