"use client";

import { useState } from "react";
import { CreditCard, Plus } from "lucide-react";

import { Button } from "@/components/ui/button";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState } from "@/components/shared/empty-state";
import { CardGridSkeleton } from "@/components/shared/loading-skeleton";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { CreditCardCard } from "@/components/credit-cards/credit-card-card";
import { CreditCardForm } from "@/components/credit-cards/credit-card-form";
import {
  useCreditCards,
  useDeleteCreditCard,
} from "@/hooks/use-credit-cards";
import type { CreditCardResponseDto } from "@/types/credit-card";

export default function CreditCardsPage() {
  const { data: cards, isLoading } = useCreditCards();
  const deleteCard = useDeleteCreditCard();

  const [formOpen, setFormOpen] = useState(false);
  const [editingCard, setEditingCard] =
    useState<CreditCardResponseDto | null>(null);
  const [deletingCard, setDeletingCard] =
    useState<CreditCardResponseDto | null>(null);

  const handleNew = () => {
    setEditingCard(null);
    setFormOpen(true);
  };

  const handleEdit = (card: CreditCardResponseDto) => {
    setEditingCard(card);
    setFormOpen(true);
  };

  const handleDelete = () => {
    if (!deletingCard) return;
    deleteCard.mutate(deletingCard.id, {
      onSuccess: () => setDeletingCard(null),
    });
  };

  const sortedCards = cards
    ? [...cards].sort((a, b) => a.name.localeCompare(b.name))
    : [];

  return (
    <div className="space-y-6">
      <PageHeader
        title="Cartões"
        description="Gerencie seus cartões de crédito, faturas e parcelamentos."
      >
        <Button onClick={handleNew}>
          <Plus className="mr-2 h-4 w-4" />
          Novo Cartão
        </Button>
      </PageHeader>

      {isLoading ? (
        <CardGridSkeleton />
      ) : sortedCards.length === 0 ? (
        <EmptyState
          icon={CreditCard}
          title="Nenhum cartão cadastrado"
          description="Cadastre seu primeiro cartão para registrar compras e acompanhar faturas."
          actionLabel="Novo Cartão"
          onAction={handleNew}
        />
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {sortedCards.map((card) => (
            <CreditCardCard
              key={card.id}
              card={card}
              onEdit={() => handleEdit(card)}
              onDelete={() => setDeletingCard(card)}
            />
          ))}
        </div>
      )}

      <CreditCardForm
        open={formOpen}
        onOpenChange={(open) => {
          setFormOpen(open);
          if (!open) setEditingCard(null);
        }}
        editingCard={editingCard}
      />

      <ConfirmDialog
        open={!!deletingCard}
        onOpenChange={(open) => {
          if (!open) setDeletingCard(null);
        }}
        title="Excluir cartão"
        description={`Tem certeza que deseja excluir o cartão "${deletingCard?.name}"? As faturas e transações vinculadas também serão removidas.`}
        onConfirm={handleDelete}
        confirmLabel="Excluir"
        variant="destructive"
      />
    </div>
  );
}
