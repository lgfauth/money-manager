"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { Tags, Plus, MoreHorizontal, Pencil, Trash2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Badge } from "@/components/ui/badge";
import { Card, CardContent } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
} from "@/components/ui/dialog";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";
import { PageHeader } from "@/components/shared/page-header";
import { EmptyState } from "@/components/shared/empty-state";
import { ConfirmDialog } from "@/components/shared/confirm-dialog";
import { ColorPicker } from "@/components/shared/color-picker";
import { CardGridSkeleton } from "@/components/shared/loading-skeleton";
import { categorySchema, type CategoryFormData } from "@/lib/validators";
import {
  useCategories,
  useCreateCategory,
  useUpdateCategory,
  useDeleteCategory,
} from "@/hooks/use-categories";
import type { CategoryResponseDto } from "@/types/category";
import { CategoryType } from "@/types/category";

export default function CategoriesPage() {
  const { data: categories, isLoading } = useCategories();
  const createMutation = useCreateCategory();
  const updateMutation = useUpdateCategory();
  const deleteMutation = useDeleteCategory();

  const [dialogOpen, setDialogOpen] = useState(false);
  const [editingCategory, setEditingCategory] =
    useState<CategoryResponseDto | null>(null);
  const [deleteTarget, setDeleteTarget] = useState<string | null>(null);

  const {
    register,
    handleSubmit,
    reset,
    setValue,
    watch,
    formState: { errors },
  } = useForm<CategoryFormData>({
    resolver: zodResolver(categorySchema),
    defaultValues: { color: "#6366f1", type: CategoryType.Expense },
  });

  const selectedColor = watch("color");

  const openCreate = () => {
    setEditingCategory(null);
    reset({ name: "", type: CategoryType.Expense, color: "#6366f1" });
    setDialogOpen(true);
  };

  const openEdit = (cat: CategoryResponseDto) => {
    setEditingCategory(cat);
    reset({ name: cat.name, type: cat.type, color: cat.color });
    setDialogOpen(true);
  };

  const onSubmit = (data: CategoryFormData) => {
    if (editingCategory) {
      updateMutation.mutate(
        { id: editingCategory.id, data },
        { onSuccess: () => setDialogOpen(false) }
      );
    } else {
      createMutation.mutate(data, {
        onSuccess: () => setDialogOpen(false),
      });
    }
  };

  const handleDelete = () => {
    if (deleteTarget) {
      deleteMutation.mutate(deleteTarget, {
        onSuccess: () => setDeleteTarget(null),
      });
    }
  };

  const incomeCategories =
    categories?.filter((c) => c.type === CategoryType.Income) ?? [];
  const expenseCategories =
    categories?.filter((c) => c.type === CategoryType.Expense) ?? [];

  const renderGrid = (items: CategoryResponseDto[]) => {
    if (items.length === 0) {
      return (
        <EmptyState
          icon={Tags}
          title="Nenhuma categoria"
          description="Crie sua primeira categoria para organizar suas financas"
          actionLabel="Nova Categoria"
          onAction={openCreate}
        />
      );
    }

    return (
      <div className="grid grid-cols-2 gap-4 md:grid-cols-3 lg:grid-cols-4">
        {items.map((cat) => (
          <Card
            key={cat.id}
            className="rounded-xl hover:shadow-md transition-shadow"
          >
            <CardContent className="flex items-center justify-between p-4">
              <div className="flex items-center gap-3">
                <span
                  className="h-3 w-3 rounded-full"
                  style={{ backgroundColor: cat.color }}
                />
                <div>
                  <p className="text-sm font-medium">{cat.name}</p>
                  <Badge
                    variant="secondary"
                    className="mt-1 text-[10px]"
                  >
                    {cat.type === CategoryType.Income ? "Receita" : "Despesa"}
                  </Badge>
                </div>
              </div>
              <DropdownMenu>
                <DropdownMenuTrigger className="outline-none">
                  <MoreHorizontal className="h-4 w-4 text-muted-foreground" />
                </DropdownMenuTrigger>
                <DropdownMenuContent align="end">
                  <DropdownMenuItem onClick={() => openEdit(cat)}>
                    <Pencil className="mr-2 h-4 w-4" />
                    Editar
                  </DropdownMenuItem>
                  <DropdownMenuItem
                    variant="destructive"
                    onClick={() => setDeleteTarget(cat.id)}
                  >
                    <Trash2 className="mr-2 h-4 w-4" />
                    Excluir
                  </DropdownMenuItem>
                </DropdownMenuContent>
              </DropdownMenu>
            </CardContent>
          </Card>
        ))}
      </div>
    );
  };

  return (
    <div className="space-y-6">
      <PageHeader title="Categorias" description="Organize suas receitas e despesas">
        <Button onClick={openCreate}>
          <Plus className="mr-2 h-4 w-4" />
          Nova Categoria
        </Button>
      </PageHeader>

      {isLoading ? (
        <CardGridSkeleton />
      ) : (
        <Tabs defaultValue="all">
          <TabsList>
            <TabsTrigger value="all">
              Todas ({categories?.length ?? 0})
            </TabsTrigger>
            <TabsTrigger value="income">
              Receitas ({incomeCategories.length})
            </TabsTrigger>
            <TabsTrigger value="expense">
              Despesas ({expenseCategories.length})
            </TabsTrigger>
          </TabsList>
          <TabsContent value="all" className="mt-4">
            {renderGrid(categories ?? [])}
          </TabsContent>
          <TabsContent value="income" className="mt-4">
            {renderGrid(incomeCategories)}
          </TabsContent>
          <TabsContent value="expense" className="mt-4">
            {renderGrid(expenseCategories)}
          </TabsContent>
        </Tabs>
      )}

      {/* Create/Edit Dialog */}
      <Dialog open={dialogOpen} onOpenChange={setDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>
              {editingCategory ? "Editar Categoria" : "Nova Categoria"}
            </DialogTitle>
          </DialogHeader>
          <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
            <div className="space-y-2">
              <Label htmlFor="name">Nome</Label>
              <Input id="name" {...register("name")} />
              {errors.name && (
                <p className="text-sm text-destructive">
                  {errors.name.message}
                </p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="type">Tipo</Label>
              <Select
                defaultValue={watch("type")}
                onValueChange={(v) =>
                  setValue("type", v as CategoryType)
                }
              >
                <SelectTrigger>
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Income">Receita</SelectItem>
                  <SelectItem value="Expense">Despesa</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label>Cor</Label>
              <ColorPicker
                value={selectedColor}
                onChange={(c) => setValue("color", c)}
              />
            </div>

            <DialogFooter>
              <Button
                type="submit"
                disabled={
                  createMutation.isPending || updateMutation.isPending
                }
              >
                {editingCategory ? "Salvar" : "Criar"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* Delete Confirmation */}
      <ConfirmDialog
        open={!!deleteTarget}
        onOpenChange={(open) => !open && setDeleteTarget(null)}
        title="Excluir categoria"
        description="Tem certeza que deseja excluir esta categoria? Esta acao nao pode ser desfeita."
        confirmLabel="Excluir"
        variant="destructive"
        onConfirm={handleDelete}
      />
    </div>
  );
}
