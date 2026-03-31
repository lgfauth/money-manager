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
  { href: "/transactions", icon: ArrowLeftRight, label: "Transacoes" },
  { href: "/budgets", icon: PieChart, label: "Orcamentos" },
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
          <SheetContent side="left" className="w-72 p-0">
            <div className="flex h-14 items-center gap-2 border-b px-4">
              <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-primary-foreground">
                <Wallet className="h-4 w-4" />
              </div>
              <span className="font-semibold text-sm">MoneyManager</span>
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
                          "flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors",
                          isActive
                            ? "bg-primary/10 text-primary font-medium"
                            : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
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
