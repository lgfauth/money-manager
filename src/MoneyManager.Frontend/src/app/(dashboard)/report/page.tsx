"use client";

import { useState, useRef } from "react";
import { useRouter } from "next/navigation";
import { useMutation } from "@tanstack/react-query";
import {
  MessageSquarePlus,
  Upload,
  X,
  CheckCircle2,
  ImageIcon,
  VideoIcon,
} from "lucide-react";
import { toast } from "sonner";
import { apiClient } from "@/lib/api-client";
import { getApiErrorMessage } from "@/lib/api-errors";
import { useAuthStore } from "@/stores/auth-store";

import { FormErrorSummary } from "@/components/shared/form-error-summary";
import { PageHeader } from "@/components/shared/page-header";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Textarea } from "@/components/ui/textarea";
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from "@/components/ui/card";
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from "@/components/ui/select";

const reportCategories = [
  { value: "Elogio", label: "Elogio", icon: "👏" },
  { value: "Reclamação", label: "Reclamação", icon: "😞" },
  { value: "Sugestão de melhoria", label: "Sugestão de melhoria", icon: "💡" },
  { value: "Encontrei um problema", label: "Encontrei um problema", icon: "🐛" },
];

const ACCEPTED_TYPES = "image/jpeg,image/png,image/gif,image/webp,video/mp4,video/webm,video/quicktime";
const MAX_FILE_SIZE = 10 * 1024 * 1024; // 10 MB

export default function ReportPage() {
  const { user } = useAuthStore();
  const router = useRouter();
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [category, setCategory] = useState("");
  const [description, setDescription] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<string | null>(null);
  const [submitted, setSubmitted] = useState(false);
  const [submitCount, setSubmitCount] = useState(0);

  const validationErrors = [
    !category ? "Selecione uma categoria para o report." : null,
    description.trim().length < 10
      ? "A descricao precisa ter pelo menos 10 caracteres."
      : null,
  ].filter((message): message is string => Boolean(message));

  const submitReport = useMutation({
    mutationFn: async () => {
      const formData = new FormData();
      formData.append("category", category);
      formData.append("description", description);
      if (file) {
        formData.append("attachment", file);
      }
      return apiClient.postForm<unknown>("/api/UserReports", formData);
    },
    onSuccess: () => {
      setSubmitted(true);
      toast.success("Report enviado com sucesso!");
    },
    onError: (error) => {
      toast.error(getApiErrorMessage(error, "Erro ao enviar report."));
    },
  });

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selected = e.target.files?.[0];
    if (!selected) return;

    if (selected.size > MAX_FILE_SIZE) {
      toast.error("Arquivo muito grande. Máximo 10 MB.");
      return;
    }

    setFile(selected);

    if (selected.type.startsWith("image/")) {
      const reader = new FileReader();
      reader.onloadend = () => setPreview(reader.result as string);
      reader.readAsDataURL(selected);
    } else {
      setPreview(null);
    }
  };

  const removeFile = () => {
    setFile(null);
    setPreview(null);
    if (fileInputRef.current) fileInputRef.current.value = "";
  };

  const canSubmit =
    validationErrors.length === 0 && !submitReport.isPending;

  if (submitted) {
    return (
      <div className="space-y-6">
        <PageHeader title="Enviar Report" />
        <Card className="mx-auto max-w-lg">
          <CardContent className="flex flex-col items-center gap-4 py-12">
            <CheckCircle2 className="h-16 w-16 text-green-500" />
            <h2 className="text-xl font-semibold">Report enviado!</h2>
            <p className="text-center text-sm text-muted-foreground">
              Obrigado pelo seu feedback. Sua mensagem foi registrada e será
              analisada pela equipe.
            </p>
            <div className="flex gap-2 pt-4">
              <Button
                variant="outline"
                onClick={() => {
                  setSubmitted(false);
                  setCategory("");
                  setDescription("");
                  removeFile();
                }}
              >
                Enviar outro
              </Button>
              <Button onClick={() => router.push("/")}>
                Voltar ao Dashboard
              </Button>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        title="Enviar Report"
        description="Compartilhe elogios, sugestões, problemas ou reclamações."
      />

      <Card className="mx-auto max-w-lg">
        <CardHeader>
          <CardTitle className="flex items-center gap-2">
            <MessageSquarePlus className="h-5 w-5" />
            Novo Report
          </CardTitle>
          <CardDescription>
            Preencha os campos abaixo. Seu feedback é importante para nós.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form
            onSubmit={(e) => {
              e.preventDefault();

              setSubmitCount((currentCount) => currentCount + 1);

              if (!canSubmit) {
                return;
              }

              submitReport.mutate();
            }}
            className="space-y-5"
          >
            <FormErrorSummary
              submitCount={submitCount}
              messages={validationErrors}
              apiError={submitReport.error}
            />

            {/* Category */}
            <div className="space-y-2">
              <Label>Categoria do Report</Label>
              <Select value={category} onValueChange={(v) => v && setCategory(v)}>
                <SelectTrigger className="w-full">
                  <SelectValue placeholder="Selecione uma categoria" />
                </SelectTrigger>
                <SelectContent>
                  {reportCategories.map((cat) => (
                    <SelectItem key={cat.value} value={cat.value}>
                      {cat.icon} {cat.label}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {submitCount > 0 && !category && (
                <p className="text-xs text-destructive">
                  Selecione uma categoria para continuar.
                </p>
              )}
            </div>

            {/* Name (read-only from auth) */}
            <div className="space-y-2">
              <Label>Nome</Label>
              <Input value={user?.name ?? ""} disabled />
              <p className="text-xs text-muted-foreground">
                Capturado automaticamente do seu perfil.
              </p>
            </div>

            {/* Description */}
            <div className="space-y-2">
              <Label htmlFor="description">Descrição</Label>
              <Textarea
                id="description"
                placeholder="Descreva detalhadamente o que deseja reportar..."
                rows={6}
                value={description}
                onChange={(e) => setDescription(e.target.value)}
                maxLength={5000}
              />
              <div className="flex justify-between">
                <p className="text-xs text-muted-foreground">
                  Mínimo 10 caracteres
                </p>
                <p className="text-xs text-muted-foreground">
                  {description.length}/5000
                </p>
              </div>
              {submitCount > 0 && description.trim().length < 10 && (
                <p className="text-xs text-destructive">
                  Informe uma descricao com pelo menos 10 caracteres.
                </p>
              )}
            </div>

            {/* File upload */}
            <div className="space-y-2">
              <Label>Anexo (opcional)</Label>
              <p className="text-xs text-muted-foreground mb-2">
                Imagem ou vídeo (máx. 10 MB)
              </p>

              {!file ? (
                <button
                  type="button"
                  onClick={() => fileInputRef.current?.click()}
                  className="flex w-full cursor-pointer flex-col items-center gap-2 rounded-lg border-2 border-dashed border-muted-foreground/25 p-6 text-muted-foreground transition-colors hover:border-primary/50 hover:text-primary"
                >
                  <Upload className="h-8 w-8" />
                  <span className="text-sm">
                    Clique para selecionar um arquivo
                  </span>
                </button>
              ) : (
                <div className="flex items-center gap-3 rounded-lg border p-3">
                  {file.type.startsWith("image/") ? (
                    <ImageIcon className="h-5 w-5 text-blue-500 shrink-0" />
                  ) : (
                    <VideoIcon className="h-5 w-5 text-purple-500 shrink-0" />
                  )}
                  <div className="flex-1 min-w-0">
                    <p className="truncate text-sm font-medium">{file.name}</p>
                    <p className="text-xs text-muted-foreground">
                      {(file.size / 1024 / 1024).toFixed(2)} MB
                    </p>
                  </div>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    onClick={removeFile}
                  >
                    <X className="h-4 w-4" />
                  </Button>
                </div>
              )}

              {preview && (
                <img
                  src={preview}
                  alt="Preview"
                  className="mt-2 max-h-48 rounded-lg object-contain"
                />
              )}

              <input
                ref={fileInputRef}
                type="file"
                accept={ACCEPTED_TYPES}
                className="hidden"
                onChange={handleFileChange}
              />
            </div>

            <Button
              type="submit"
              className="w-full"
              disabled={submitReport.isPending}
            >
              {submitReport.isPending ? "Enviando..." : "Enviar Report"}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
