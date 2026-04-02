"use client";

import { Fragment } from "react";
import Link from "next/link";
import { ChevronRight, Home } from "lucide-react";
import { useInvoiceSummary } from "@/hooks/use-invoices";

const routeLabels: Record<string, string> = {
  "": "Dashboard",
  accounts: "Contas",
  transactions: "Transações",
  categories: "Categorias",
  budgets: "Orçamentos",
  reports: "Relatórios",
  recurring: "Recorrentes",
  "credit-cards": "Cartões",
  invoices: "Faturas",
  profile: "Perfil",
  settings: "Configurações",
  onboarding: "Onboarding",
};

const nonNavigableBreadcrumbHrefs = new Set(["/credit-cards", "/invoices"]);

interface BreadcrumbProps {
  pathname: string;
}

function getInvoiceBreadcrumbLabel(referenceMonth?: string, accountName?: string) {
  if (referenceMonth) {
    const match = referenceMonth.match(/^(\d{4})-(\d{2})$/);
    if (match) {
      const [, year, month] = match;
      return `Fatura ${month}/${year}`;
    }
  }

  if (accountName && accountName.trim().length > 0) {
    return `Fatura ${accountName.trim()}`;
  }

  return "Fatura";
}

export function Breadcrumb({ pathname }: BreadcrumbProps) {
  const segments = pathname.split("/").filter(Boolean);
  const invoicesIndex = segments.findIndex((segment) => segment === "invoices");
  const invoiceId =
    invoicesIndex >= 0 && segments.length > invoicesIndex + 1
      ? segments[invoicesIndex + 1]
      : "";

  const { data: invoiceSummary } = useInvoiceSummary(invoiceId);
  const invoiceBreadcrumbLabel = getInvoiceBreadcrumbLabel(
    invoiceSummary?.invoice.referenceMonth,
    invoiceSummary?.invoice.accountName
  );

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
        const isInvoiceIdSegment =
          invoicesIndex >= 0 && index === invoicesIndex + 1 && segment === invoiceId;
        const label = isInvoiceIdSegment
          ? invoiceBreadcrumbLabel
          : (routeLabels[segment] ?? segment);
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
