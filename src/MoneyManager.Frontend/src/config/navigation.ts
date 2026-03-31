import {
  LayoutDashboard,
  Wallet,
  ArrowLeftRight,
  Tags,
  PieChart,
  BarChart3,
  Repeat,
  User,
  Settings,
  type LucideIcon,
} from "lucide-react";

export interface NavItem {
  title: string;
  href: string;
  icon: LucideIcon;
  group: "main" | "user";
}

export const navigationItems: NavItem[] = [
  { title: "Dashboard", href: "/", icon: LayoutDashboard, group: "main" },
  { title: "Contas", href: "/accounts", icon: Wallet, group: "main" },
  { title: "Transações", href: "/transactions", icon: ArrowLeftRight, group: "main" },
  { title: "Categorias", href: "/categories", icon: Tags, group: "main" },
  { title: "Orçamentos", href: "/budgets", icon: PieChart, group: "main" },
  { title: "Relatórios", href: "/reports", icon: BarChart3, group: "main" },
  { title: "Recorrentes", href: "/recurring", icon: Repeat, group: "main" },
  { title: "Perfil", href: "/profile", icon: User, group: "user" },
  { title: "Configurações", href: "/settings", icon: Settings, group: "user" },
];
