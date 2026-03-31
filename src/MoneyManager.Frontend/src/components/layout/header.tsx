"use client";

import { usePathname } from "next/navigation";
import { Menu, Search, LogOut, User, Settings } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuLabel,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { useAuthStore } from "@/stores/auth-store";
import { useUIStore } from "@/stores/ui-store";
import { useLogout } from "@/hooks/use-auth";
import { Breadcrumb } from "./breadcrumb";
import Link from "next/link";

export function Header() {
  const { user } = useAuthStore();
  const { toggleSidebar, toggleCommand } = useUIStore();
  const handleLogout = useLogout();
  const pathname = usePathname();

  const initials = user?.name
    ? user.name
        .split(" ")
        .map((n) => n[0])
        .join("")
        .toUpperCase()
        .slice(0, 2)
    : "?";

  return (
    <header className="sticky top-0 z-30 flex h-14 items-center gap-4 border-b bg-card/80 px-4 backdrop-blur-sm md:px-6">
      {/* Mobile menu toggle */}
      <Button
        variant="ghost"
        size="icon"
        className="md:hidden"
        onClick={toggleSidebar}
      >
        <Menu className="h-5 w-5" />
        <span className="sr-only">Menu</span>
      </Button>

      {/* Breadcrumb */}
      <div className="flex-1">
        <Breadcrumb pathname={pathname} />
      </div>

      {/* Command palette trigger */}
      <Button
        variant="outline"
        size="sm"
        className="hidden gap-2 text-muted-foreground md:flex"
        onClick={toggleCommand}
      >
        <Search className="h-4 w-4" />
        <span className="text-xs">Buscar...</span>
        <kbd className="pointer-events-none rounded border bg-muted px-1.5 py-0.5 text-[10px] font-medium">
          Ctrl+K
        </kbd>
      </Button>

      {/* User menu */}
      <DropdownMenu>
        <DropdownMenuTrigger className="rounded-full outline-none focus-visible:ring-2 focus-visible:ring-ring">
          <Avatar className="h-8 w-8">
            <AvatarFallback className="bg-primary/10 text-primary text-xs">
              {initials}
            </AvatarFallback>
          </Avatar>
        </DropdownMenuTrigger>
        <DropdownMenuContent align="end" className="w-56">
          <DropdownMenuLabel>
            <div className="flex flex-col space-y-1">
              <p className="text-sm font-medium">{user?.name}</p>
              <p className="text-xs text-muted-foreground">{user?.email}</p>
            </div>
          </DropdownMenuLabel>
          <DropdownMenuSeparator />
          <DropdownMenuItem>
            <Link href="/profile">
              <User className="mr-2 h-4 w-4" />
              Perfil
            </Link>
          </DropdownMenuItem>
          <DropdownMenuItem>
            <Link href="/settings">
              <Settings className="mr-2 h-4 w-4" />
              Configuracoes
            </Link>
          </DropdownMenuItem>
          <DropdownMenuSeparator />
          <DropdownMenuItem onClick={handleLogout}>
            <LogOut className="mr-2 h-4 w-4" />
            Sair
          </DropdownMenuItem>
        </DropdownMenuContent>
      </DropdownMenu>
    </header>
  );
}
