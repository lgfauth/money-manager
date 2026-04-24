"use client";

import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import { BankTransactionFormBody } from "@/components/transactions/bank-transaction-form-body";
import {
  TransactionType,
  type TransactionResponseDto,
} from "@/types/transaction";

interface TransactionFormProps {
  open: boolean;
  onOpenChange: (open: boolean) => void;
  editingTransaction?: TransactionResponseDto | null;
  defaultType?: TransactionType;
}

export function TransactionForm({
  open,
  onOpenChange,
  editingTransaction,
  defaultType = TransactionType.Expense,
}: TransactionFormProps) {
  const isEditing = !!editingTransaction;

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[85vh] overflow-y-auto sm:max-w-md max-md:mt-[env(safe-area-inset-top,16px)] max-md:mb-[env(safe-area-inset-bottom,16px)] max-md:pt-10 max-md:pb-6">
        <DialogHeader>
          <DialogTitle>
            {isEditing ? "Editar Transação" : "Nova Transação"}
          </DialogTitle>
          <DialogDescription>
            {isEditing
              ? "Altere os dados da transação."
              : "Preencha os dados para registrar."}
          </DialogDescription>
        </DialogHeader>

        <BankTransactionFormBody
          open={open}
          editingTransaction={editingTransaction}
          defaultType={defaultType}
          onClose={() => onOpenChange(false)}
        />
      </DialogContent>
    </Dialog>
  );
}
