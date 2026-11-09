"use client";

import { useEffect, useRef, useState } from "react";
import {
  ArrowDown,
  ArrowUp,
  Camera,
  FileSpreadsheet,
  FileText,
  FolderOpen,
  Upload,
  X,
} from "lucide-react";
import { Button } from "@/components/ui/button";
import { cn } from "@/lib/utils";
import {
  INVOICE_IMPORT_ACCEPT,
  classifyInvoiceImportFile,
  formatFileSize,
} from "@/lib/invoice-import";

interface InvoiceImportFilePickerProps {
  files: File[];
  onChange: (files: File[]) => void;
  disabled?: boolean;
}

const fileKey = (file: File) => `${file.name}-${file.size}-${file.lastModified}`;

/**
 * Seleção dos arquivos da fatura: arrastar e soltar (desktop), câmera e galeria/arquivos
 * (mobile). Novos arquivos são acrescentados aos já escolhidos, para fotografar várias páginas
 * em sequência; as imagens podem ser reordenadas, pois a ordem é a das páginas da fatura.
 */
export function InvoiceImportFilePicker({ files, onChange, disabled }: InvoiceImportFilePickerProps) {
  const filesInputRef = useRef<HTMLInputElement>(null);
  const cameraInputRef = useRef<HTMLInputElement>(null);
  const [dragging, setDragging] = useState(false);

  const addFiles = (list: FileList | null) => {
    if (!list || list.length === 0) return;
    const known = new Set(files.map(fileKey));
    const added = Array.from(list).filter((f) => !known.has(fileKey(f)));
    if (added.length > 0) onChange([...files, ...added]);
  };

  const handleInput = (event: React.ChangeEvent<HTMLInputElement>) => {
    addFiles(event.target.files);
    // Limpa o input para permitir escolher o mesmo arquivo de novo depois de removê-lo
    event.target.value = "";
  };

  const move = (index: number, delta: number) => {
    const target = index + delta;
    if (target < 0 || target >= files.length) return;
    const next = [...files];
    [next[index], next[target]] = [next[target], next[index]];
    onChange(next);
  };

  const remove = (index: number) => onChange(files.filter((_, i) => i !== index));

  const canReorder = files.length > 1 && files.every((f) => classifyInvoiceImportFile(f) === "Images");

  return (
    <div className="space-y-3">
      <div
        onDragOver={(event) => {
          event.preventDefault();
          if (!disabled) setDragging(true);
        }}
        onDragLeave={() => setDragging(false)}
        onDrop={(event) => {
          event.preventDefault();
          setDragging(false);
          if (!disabled) addFiles(event.dataTransfer.files);
        }}
        className={cn(
          "flex flex-col items-center gap-3 rounded-xl border-2 border-dashed bg-card px-4 py-6 text-center transition-colors",
          dragging && "border-primary bg-primary/5",
          disabled && "opacity-60"
        )}
      >
        <Upload className="hidden h-8 w-8 text-muted-foreground sm:block" aria-hidden />
        <p className="hidden text-sm sm:block">
          Arraste e solte aqui o PDF, as imagens ou a planilha .xlsx da fatura
        </p>
        <p className="text-xs text-muted-foreground">
          Um PDF (até 20 MB), até 10 imagens JPEG, PNG ou WebP (até 10 MB cada) ou uma planilha
          .xlsx (até 5 MB).
        </p>
        <div className="flex w-full flex-col gap-2 sm:w-auto sm:flex-row">
          <Button
            type="button"
            className="sm:hidden"
            onClick={() => cameraInputRef.current?.click()}
            disabled={disabled}
          >
            <Camera className="mr-2 h-4 w-4" />
            Tirar foto
          </Button>
          <Button
            type="button"
            variant="outline"
            onClick={() => filesInputRef.current?.click()}
            disabled={disabled}
          >
            <FolderOpen className="mr-2 h-4 w-4" />
            Escolher arquivos
          </Button>
        </div>
        <input
          ref={filesInputRef}
          type="file"
          multiple
          accept={INVOICE_IMPORT_ACCEPT}
          className="hidden"
          onChange={handleInput}
        />
        <input
          ref={cameraInputRef}
          type="file"
          accept="image/jpeg,image/png,image/webp"
          capture="environment"
          className="hidden"
          onChange={handleInput}
        />
      </div>

      {files.length > 0 && (
        <ul className="divide-y rounded-xl border bg-card" aria-label="Arquivos escolhidos">
          {files.map((file, index) => (
            <li key={fileKey(file)} className="flex items-center gap-3 p-2.5">
              <FilePreview file={file} />
              <div className="min-w-0 flex-1">
                <p className="truncate text-sm font-medium">
                  {canReorder && <span className="text-muted-foreground">{index + 1}. </span>}
                  {file.name}
                </p>
                <p className="text-xs text-muted-foreground">{formatFileSize(file.size)}</p>
              </div>
              {canReorder && (
                <div className="flex shrink-0">
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8"
                    onClick={() => move(index, -1)}
                    disabled={disabled || index === 0}
                    aria-label={`Mover ${file.name} para cima`}
                  >
                    <ArrowUp className="h-4 w-4" />
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="icon"
                    className="h-8 w-8"
                    onClick={() => move(index, 1)}
                    disabled={disabled || index === files.length - 1}
                    aria-label={`Mover ${file.name} para baixo`}
                  >
                    <ArrowDown className="h-4 w-4" />
                  </Button>
                </div>
              )}
              <Button
                type="button"
                variant="ghost"
                size="icon"
                className="h-8 w-8 shrink-0"
                onClick={() => remove(index)}
                disabled={disabled}
                aria-label={`Remover ${file.name}`}
              >
                <X className="h-4 w-4" />
              </Button>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

/** Miniatura para imagens; ícone para PDF, planilha e demais arquivos. */
function FilePreview({ file }: { file: File }) {
  const kind = classifyInvoiceImportFile(file);
  const imgRef = useRef<HTMLImageElement>(null);

  // A URL é criada e revogada no mesmo efeito (seguro com a execução dupla do StrictMode)
  useEffect(() => {
    if (kind !== "Images" || !imgRef.current) return;
    const url = URL.createObjectURL(file);
    imgRef.current.src = url;
    return () => URL.revokeObjectURL(url);
  }, [file, kind]);

  const box = "flex h-12 w-12 shrink-0 items-center justify-center overflow-hidden rounded-md border bg-muted";

  if (kind === "Images") {
    return (
      <span className={box}>
        {/* eslint-disable-next-line @next/next/no-img-element -- miniatura local (object URL) */}
        <img ref={imgRef} alt="" className="h-full w-full object-cover" />
      </span>
    );
  }

  const Icon = kind === "Spreadsheet" ? FileSpreadsheet : FileText;
  return (
    <span className={box}>
      <Icon className="h-5 w-5 text-muted-foreground" aria-hidden />
    </span>
  );
}
