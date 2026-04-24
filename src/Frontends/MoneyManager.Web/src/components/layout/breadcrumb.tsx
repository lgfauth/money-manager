"use client";

import { Fragment } from "react";
import Link from "next/link";
import { ChevronRight, Home } from "lucide-react";
import { useBreadcrumbStore } from "@/stores/breadcrumb-store";

const routeLabels: Record<string, string> = {
  "": "Dashboard",
  accounts: "Contas",
  "credit-cards": "Cartões",
  invoices: "Faturas",
  transactions: "Transações",
  categories: "Categorias",
  budgets: "Orçamentos",
  reports: "Relatórios",
  recurring: "Recorrentes",
  profile: "Perfil",
  settings: "Configurações",
  onboarding: "Onboarding",
};

const nonNavigableBreadcrumbHrefs = new Set<string>([]);

const ID_SEGMENT_PATTERN = /^(?:[0-9a-f]{24}|[0-9a-f]{32}|[0-9a-f-]{36})$/i;

interface BreadcrumbProps {
  pathname: string;
}


export function Breadcrumb({ pathname }: BreadcrumbProps) {
  const dynamicLabels = useBreadcrumbStore((s) => s.labels);
  const segments = pathname.split("/").filter(Boolean);

  if (segments.length === 0) {
    return (
      <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
        <Home className="h-4 w-4" />
        <span className="font-medium text-foreground">Dashboard</span>
      </div>
    );
  }

  const visibleSegments = segments
    .map((segment, index) => {
      const href = "/" + segments.slice(0, index + 1).join("/");
      const staticLabel = routeLabels[segment];
      const dynamicLabel = dynamicLabels[segment];
      const label = dynamicLabel ?? staticLabel;
      const isId = !label && ID_SEGMENT_PATTERN.test(segment);
      return {
        segment,
        href,
        index,
        label: label ?? segment,
        isId,
      };
    })
    .filter((item) => !item.isId);

  return (
    <nav className="flex items-center gap-1.5 text-sm text-muted-foreground">
      <Link href="/" className="hover:text-foreground transition-colors">
        <Home className="h-4 w-4" />
      </Link>
      {visibleSegments.map((item, i) => {
        const isLast = i === visibleSegments.length - 1;
        const isNavigable = !nonNavigableBreadcrumbHrefs.has(item.href);

        return (
          <Fragment key={item.href}>
            <ChevronRight className="h-3 w-3" />
            {isLast || !isNavigable ? (
              <span className="font-medium text-foreground">{item.label}</span>
            ) : (
              <Link
                href={item.href}
                className="hover:text-foreground transition-colors"
              >
                {item.label}
              </Link>
            )}
          </Fragment>
        );
      })}
    </nav>
  );
}
