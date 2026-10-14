"use client";

import type { ReactNode } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

interface DashboardBlockProps {
  title: string;
  isLoading: boolean;
  isError: boolean;
  children: ReactNode;
}

// Estado de carregamento/erro por bloco: uma consulta com falha não zera nem esconde o resto do dashboard.
export function DashboardBlock({ title, isLoading, isError, children }: DashboardBlockProps) {
  if (isLoading) return <Skeleton className="h-56 rounded-xl" />;

  if (isError) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="text-sm font-medium">{title}</CardTitle>
        </CardHeader>
        <CardContent>
          <p className="text-sm text-muted-foreground text-center py-8">
            Não foi possível carregar este bloco. Tente novamente em alguns instantes.
          </p>
        </CardContent>
      </Card>
    );
  }

  return <>{children}</>;
}
