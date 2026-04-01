"use client";

import { useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useRouter } from "next/navigation";
import {
  User,
  Lock,
  Mail,
  Trash2,
  AlertTriangle,
  Edit,
  X,
  Save,
} from "lucide-react";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Separator } from "@/components/ui/separator";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from "@/components/ui/dialog";
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from "@/components/ui/accordion";
import { FormErrorSummary } from "@/components/shared/form-error-summary";
import { PageHeader } from "@/components/shared/page-header";
import { CardGridSkeleton } from "@/components/shared/loading-skeleton";
import {
  changePasswordSchema,
  updateEmailSchema,
  type ChangePasswordFormData,
  type UpdateEmailFormData,
} from "@/lib/validators";
import {
  useProfile,
  useUpdateProfile,
  useChangePassword,
  useUpdateEmail,
  useDataCount,
  useDeleteAccount,
} from "@/hooks/use-profile";
import { useAuthStore } from "@/stores/auth-store";

export default function ProfilePage() {
  const router = useRouter();
  const profile = useProfile();
  const updateProfile = useUpdateProfile();
  const changePassword = useChangePassword();
  const updateEmail = useUpdateEmail();
  const dataCount = useDataCount();
  const deleteAccount = useDeleteAccount();
  const logout = useAuthStore((s) => s.logout);

  // Edit mode for personal info
  const [editing, setEditing] = useState(false);
  const [name, setName] = useState("");
  const [fullName, setFullName] = useState("");
  const [phone, setPhone] = useState("");
  const [profilePicture, setProfilePicture] = useState("");

  // Email dialog
  const [emailDialogOpen, setEmailDialogOpen] = useState(false);

  // LGPD
  const [deletePassword, setDeletePassword] = useState("");
  const [deleteConfirmText, setDeleteConfirmText] = useState("");
  const [deleteCheckbox, setDeleteCheckbox] = useState(false);

  const startEditing = () => {
    if (!profile.data) return;
    setName(profile.data.name);
    setFullName(profile.data.fullName ?? "");
    setPhone(profile.data.phone ?? "");
    setProfilePicture(profile.data.profilePicture ?? "");
    setEditing(true);
  };

  const cancelEditing = () => setEditing(false);

  const saveProfile = () => {
    updateProfile.mutate(
      { name, fullName: fullName || undefined, phone: phone || undefined, profilePicture: profilePicture || undefined },
      { onSuccess: () => setEditing(false) }
    );
  };

  // Password form
  const passwordForm = useForm<ChangePasswordFormData>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: { currentPassword: "", newPassword: "", confirmNewPassword: "" },
  });

  const onPasswordSubmit = passwordForm.handleSubmit((data) => {
    changePassword.mutate(data, {
      onSuccess: () => passwordForm.reset(),
    });
  });

  // Email form
  const emailForm = useForm<UpdateEmailFormData>({
    resolver: zodResolver(updateEmailSchema),
    defaultValues: { newEmail: "", password: "" },
  });

  const onEmailSubmit = emailForm.handleSubmit((data) => {
    updateEmail.mutate(data, {
      onSuccess: () => {
        emailForm.reset();
        setEmailDialogOpen(false);
      },
    });
  });

  // LGPD delete
  const CONFIRM_TEXT = "DELETAR MINHA CONTA";
  const canDelete =
    deletePassword.length > 0 &&
    deleteConfirmText === CONFIRM_TEXT &&
    deleteCheckbox;

  const handleDelete = () => {
    deleteAccount.mutate(
      { password: deletePassword, confirmationText: deleteConfirmText },
      {
        onSuccess: () => {
          localStorage.clear();
          sessionStorage.clear();
          logout();
          router.push("/account-deleted");
        },
        onError: () => {
          // Error toast handled by mutation
        },
      }
    );
  };

  if (profile.isLoading) {
    return (
      <div className="space-y-6">
        <PageHeader title="Perfil" description="Suas informações pessoais" />
        <CardGridSkeleton count={3} />
      </div>
    );
  }

  const p = profile.data;

  return (
    <div className="space-y-6">
      <PageHeader title="Perfil" description="Gerencie suas informações pessoais" />

      {/* Personal info */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle className="text-base flex items-center gap-2">
            <User className="h-4 w-4" />
            Informações Pessoais
          </CardTitle>
          {!editing ? (
            <Button variant="outline" size="sm" onClick={startEditing}>
              <Edit className="mr-2 h-3 w-3" />
              Editar
            </Button>
          ) : (
            <div className="flex gap-2">
              <Button variant="outline" size="sm" onClick={cancelEditing}>
                <X className="mr-1 h-3 w-3" />
                Cancelar
              </Button>
              <Button
                size="sm"
                onClick={saveProfile}
                disabled={updateProfile.isPending}
              >
                <Save className="mr-1 h-3 w-3" />
                Salvar
              </Button>
            </div>
          )}
        </CardHeader>
        <CardContent className="space-y-4">
          {editing ? (
            <>
              <div className="grid gap-4 sm:grid-cols-2">
                <div className="space-y-2">
                  <Label>Nome</Label>
                  <Input value={name} onChange={(e) => setName(e.target.value)} />
                </div>
                <div className="space-y-2">
                  <Label>Nome Completo</Label>
                  <Input
                    value={fullName}
                    onChange={(e) => setFullName(e.target.value)}
                    placeholder="Opcional"
                  />
                </div>
                <div className="space-y-2">
                  <Label>Telefone</Label>
                  <Input
                    value={phone}
                    onChange={(e) => setPhone(e.target.value)}
                    placeholder="Opcional"
                  />
                </div>
                <div className="space-y-2">
                  <Label>URL da Foto</Label>
                  <Input
                    value={profilePicture}
                    onChange={(e) => setProfilePicture(e.target.value)}
                    placeholder="Opcional"
                  />
                </div>
              </div>
            </>
          ) : (
            <div className="grid gap-4 sm:grid-cols-2">
              <div>
                <p className="text-sm text-muted-foreground">Nome</p>
                <p className="font-medium">{p?.name ?? "—"}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Nome Completo</p>
                <p className="font-medium">{p?.fullName || "—"}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">E-mail</p>
                <p className="font-medium">{p?.email ?? "—"}</p>
              </div>
              <div>
                <p className="text-sm text-muted-foreground">Telefone</p>
                <p className="font-medium">{p?.phone || "—"}</p>
              </div>
            </div>
          )}
        </CardContent>
      </Card>

      {/* Change password */}
      <Card>
        <CardHeader>
          <CardTitle className="text-base flex items-center gap-2">
            <Lock className="h-4 w-4" />
            Alterar Senha
          </CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={onPasswordSubmit} className="space-y-4 max-w-md">
            <FormErrorSummary
              errors={passwordForm.formState.errors}
              submitCount={passwordForm.formState.submitCount}
            />

            <div className="space-y-2">
              <Label>Senha Atual</Label>
              <Input
                type="password"
                {...passwordForm.register("currentPassword")}
              />
              {passwordForm.formState.errors.currentPassword && (
                <p className="text-xs text-destructive">
                  {passwordForm.formState.errors.currentPassword.message}
                </p>
              )}
            </div>
            <div className="space-y-2">
              <Label>Nova Senha</Label>
              <Input
                type="password"
                {...passwordForm.register("newPassword")}
              />
              {passwordForm.formState.errors.newPassword && (
                <p className="text-xs text-destructive">
                  {passwordForm.formState.errors.newPassword.message}
                </p>
              )}
            </div>
            <div className="space-y-2">
              <Label>Confirmar Nova Senha</Label>
              <Input
                type="password"
                {...passwordForm.register("confirmNewPassword")}
              />
              {passwordForm.formState.errors.confirmNewPassword && (
                <p className="text-xs text-destructive">
                  {passwordForm.formState.errors.confirmNewPassword.message}
                </p>
              )}
            </div>
            <Button type="submit" disabled={changePassword.isPending}>
              {changePassword.isPending ? "Alterando..." : "Alterar Senha"}
            </Button>
          </form>
        </CardContent>
      </Card>

      {/* Change email */}
      <Card>
        <CardHeader className="flex flex-row items-center justify-between">
          <CardTitle className="text-base flex items-center gap-2">
            <Mail className="h-4 w-4" />
            E-mail
          </CardTitle>
          <Button
            variant="outline"
            size="sm"
            onClick={() => setEmailDialogOpen(true)}
          >
            Alterar E-mail
          </Button>
        </CardHeader>
        <CardContent>
          <p className="text-sm">
            E-mail atual: <span className="font-medium">{p?.email ?? "—"}</span>
          </p>
        </CardContent>
      </Card>

      <Dialog open={emailDialogOpen} onOpenChange={setEmailDialogOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Alterar E-mail</DialogTitle>
            <DialogDescription>
              Informe o novo e-mail e sua senha para confirmar.
            </DialogDescription>
          </DialogHeader>
          <form onSubmit={onEmailSubmit} className="space-y-4">
            <FormErrorSummary
              errors={emailForm.formState.errors}
              submitCount={emailForm.formState.submitCount}
            />

            <div className="space-y-2">
              <Label>Novo E-mail</Label>
              <Input type="email" {...emailForm.register("newEmail")} />
              {emailForm.formState.errors.newEmail && (
                <p className="text-xs text-destructive">
                  {emailForm.formState.errors.newEmail.message}
                </p>
              )}
            </div>
            <div className="space-y-2">
              <Label>Senha (confirmação)</Label>
              <Input type="password" {...emailForm.register("password")} />
              {emailForm.formState.errors.password && (
                <p className="text-xs text-destructive">
                  {emailForm.formState.errors.password.message}
                </p>
              )}
            </div>
            <DialogFooter>
              <Button
                type="button"
                variant="outline"
                onClick={() => setEmailDialogOpen(false)}
              >
                Cancelar
              </Button>
              <Button type="submit" disabled={updateEmail.isPending}>
                {updateEmail.isPending ? "Atualizando..." : "Atualizar E-mail"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      {/* LGPD - Account Deletion */}
      <Separator />
      <Card className="border-destructive/20 bg-destructive/5">
        <Accordion>
          <AccordionItem value="delete" className="border-none">
            <AccordionTrigger className="px-6">
              <span className="flex items-center gap-2 text-destructive">
                <AlertTriangle className="h-4 w-4" />
                Excluir Conta
              </span>
            </AccordionTrigger>
            <AccordionContent className="px-6 pb-6">
              <div className="space-y-4">
                <div className="rounded-lg border border-destructive/30 bg-destructive/10 p-4">
                  <p className="text-sm font-medium text-destructive">
                    Esta ação é IRREVERSÍVEL
                  </p>
                  <p className="mt-1 text-sm text-muted-foreground">
                    Todos os seus dados serão permanentemente excluídos, incluindo
                    contas, transações, categorias, orçamentos e recorrentes.
                  </p>
                </div>

                {dataCount.data && (
                  <p className="text-sm">
                    Total de registros a serem excluídos:{" "}
                    <span className="font-bold text-destructive">
                      {dataCount.data.totalRecords}
                    </span>
                  </p>
                )}

                <Button
                  variant="outline"
                  size="sm"
                  onClick={() => dataCount.refetch()}
                  disabled={dataCount.isFetching}
                >
                  {dataCount.isFetching
                    ? "Contando..."
                    : "Ver contagem de dados"}
                </Button>

                <Separator />

                <div className="space-y-4 max-w-md">
                  <div className="space-y-2">
                    <Label>1. Digite sua senha</Label>
                    <Input
                      type="password"
                      value={deletePassword}
                      onChange={(e) => setDeletePassword(e.target.value)}
                      placeholder="Sua senha"
                    />
                  </div>

                  <div className="space-y-2">
                    <Label>
                      2. Digite exatamente:{" "}
                      <code className="bg-muted px-1 rounded">
                        {CONFIRM_TEXT}
                      </code>
                    </Label>
                    <Input
                      value={deleteConfirmText}
                      onChange={(e) => setDeleteConfirmText(e.target.value)}
                      placeholder={CONFIRM_TEXT}
                    />
                  </div>

                  <div className="flex items-center gap-2">
                    <input
                      type="checkbox"
                      checked={deleteCheckbox}
                      onChange={(e) => setDeleteCheckbox(e.target.checked)}
                      className="h-4 w-4 rounded border-border"
                    />
                    <Label className="text-sm font-normal">
                      3. Compreendo que esta ação é irreversível
                    </Label>
                  </div>
                </div>

                <Button
                  variant="destructive"
                  disabled={!canDelete || deleteAccount.isPending}
                  onClick={handleDelete}
                  className="gap-2"
                >
                  <Trash2 className="h-4 w-4" />
                  {deleteAccount.isPending
                    ? "Excluindo..."
                    : "Excluir Minha Conta Permanentemente"}
                </Button>
              </div>
            </AccordionContent>
          </AccordionItem>
        </Accordion>
      </Card>
    </div>
  );
}
