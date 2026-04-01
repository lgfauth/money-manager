"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  Wallet,
  ArrowLeftRight,
  PieChart,
  MoreHorizontal,
  Plus,
} from "lucide-react";
import { cn } from "@/lib/utils";
import { Sheet, SheetContent, SheetTrigger } from "@/components/ui/sheet";
import { navigationItems } from "@/config/navigation";
import { useUIStore } from "@/stores/ui-store";

const bottomNavItems = [
  { href: "/", icon: LayoutDashboard, label: "Home" },
  { href: "/accounts", icon: Wallet, label: "Contas" },
  { href: "/transactions", icon: ArrowLeftRight, label: "Transações" },
  { href: "/budgets", icon: PieChart, label: "Orçamentos" },
];

export function MobileNav() {
  const pathname = usePathname();
  const { sidebarOpen, setSidebarOpen } = useUIStore();

  return (
    <>
      {/* Bottom navigation bar */}
      <nav className="fixed bottom-0 left-0 right-0 z-40 flex h-16 items-center justify-around border-t bg-card md:hidden">
        {bottomNavItems.map((item) => {
          const isActive =
            item.href === "/"
              ? pathname === "/"
              : pathname.startsWith(item.href);

          return (
            <Link
              key={item.href}
              href={item.href}
              className={cn(
                "flex flex-col items-center gap-0.5 px-3 py-1.5 text-xs transition-colors",
                isActive
                  ? "text-primary"
                  : "text-muted-foreground hover:text-foreground"
              )}
            >
              <item.icon className="h-5 w-5" />
              <span>{item.label}</span>
            </Link>
          );
        })}

        {/* More menu trigger */}
        <Sheet open={sidebarOpen} onOpenChange={setSidebarOpen}>
          <SheetTrigger>
            <span className="flex flex-col items-center gap-0.5 px-3 py-1.5 text-xs text-muted-foreground hover:text-foreground">
              <MoreHorizontal className="h-5 w-5" />
              <span>Mais</span>
            </span>
          </SheetTrigger>
          <SheetContent side="left" className="w-72 p-0 bg-sidebar text-sidebar-foreground border-r-0">
            <div className="flex h-14 items-center gap-2 border-b border-sidebar-border px-4">
              <svg width="28" height="28" viewBox="0 0 64 64" className="shrink-0">
                <rect width="64" height="64" rx="16" fill="#00C896"/>
                <polyline points="10,44 22,28 32,36 44,18 54,26" fill="none" stroke="white" strokeWidth="4.5" strokeLinecap="round" strokeLinejoin="round"/>
                <circle cx="44" cy="18" r="5" fill="white"/>
              </svg>
              <span className="font-heading font-semibold text-sm text-white">MoneyManager</span>
            </div>
            <nav className="p-2">
              <ul className="space-y-1">
                {navigationItems.map((item) => {
                  const isActive =
                    item.href === "/"
                      ? pathname === "/"
                      : pathname.startsWith(item.href);

                  return (
                    <li key={item.href}>
                      <Link
                        href={item.href}
                        onClick={() => setSidebarOpen(false)}
                        className={cn(
                          "flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-all",
                          isActive
                            ? "bg-sidebar-accent text-sidebar-accent-foreground"
                            : "text-sidebar-foreground hover:bg-sidebar-accent/60 hover:text-sidebar-accent-foreground"
                        )}
                      >
                        <item.icon className="h-4 w-4" />
                        <span>{item.title}</span>
                      </Link>
                    </li>
                  );
                })}
              </ul>
            </nav>
          </SheetContent>
        </Sheet>
      </nav>

      {/* FAB for quick add (mobile only) */}
      <Link
        href="/transactions?new=true"
        className="fixed bottom-20 right-4 z-50 flex h-14 w-14 items-center justify-center rounded-full bg-primary text-primary-foreground shadow-lg md:hidden"
      >
        <Plus className="h-6 w-6" />
      </Link>
    </>
  );
}
