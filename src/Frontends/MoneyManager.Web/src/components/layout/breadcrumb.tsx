"use client";

import { Fragment } from "react";
import Link from "next/link";
import { ChevronRight, Home } from "lucide-react";

const routeLabels: Record<string, string> = {
  "": "Dashboard",
  accounts: "Contas",
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

interface BreadcrumbProps {
  pathname: string;
}


export function Breadcrumb({ pathname }: BreadcrumbProps) {
  const segments = pathname.split("/").filter(Boolean);

  if (segments.length === 0) {
    return (
      <div className="flex items-center gap-1.5 text-sm text-muted-foreground">
        <Home className="h-4 w-4" />
        <span className="font-medium text-foreground">Dashboard</span>
      </div>
    );
  }

  return (
    <nav className="flex items-center gap-1.5 text-sm text-muted-foreground">
      <Link href="/" className="hover:text-foreground transition-colors">
        <Home className="h-4 w-4" />
      </Link>
      {segments.map((segment, index) => {
        const href = "/" + segments.slice(0, index + 1).join("/");
        const isLast = index === segments.length - 1;
        const label = routeLabels[segment] ?? segment;
        const isNavigable = !nonNavigableBreadcrumbHrefs.has(href);

        return (
          <Fragment key={href}>
            <ChevronRight className="h-3 w-3" />
            {isLast || !isNavigable ? (
              <span className="font-medium text-foreground">{label}</span>
            ) : (
              <Link
                href={href}
                className="hover:text-foreground transition-colors"
              >
                {label}
              </Link>
            )}
          </Fragment>
        );
      })}
    </nav>
  );
}
