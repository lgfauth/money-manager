"use client";

import { Loader2, Sparkles } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useCreateDefaultCategories } from "@/hooks/use-categories";
import type { CategoryResponseDto } from "@/types/category";

interface CreateDefaultCategoriesButtonProps {
  /** Chamado com as categorias ativas do usuário depois da criação. */
  onCreated?: (categories: CategoryResponseDto[]) => void;
  variant?: "default" | "outline" | "secondary";
  size?: "default" | "sm";
  className?: string;
}

/** Cria as categorias padrão que faltam (usado na página de categorias e na revisão da importação). */
export function CreateDefaultCategoriesButton({
  onCreated,
  variant = "outline",
  size = "default",
  className,
}: CreateDefaultCategoriesButtonProps) {
  const createDefaults = useCreateDefaultCategories();

  return (
    <Button
      variant={variant}
      size={size}
      className={className}
      disabled={createDefaults.isPending}
      onClick={() =>
        createDefaults.mutate(undefined, {
          onSuccess: (result) => onCreated?.(result.categories),
        })
      }
    >
      {createDefaults.isPending ? (
        <Loader2 className="mr-2 h-4 w-4 animate-spin" />
      ) : (
        <Sparkles className="mr-2 h-4 w-4" />
      )}
      Criar categorias padrão
    </Button>
  );
}
