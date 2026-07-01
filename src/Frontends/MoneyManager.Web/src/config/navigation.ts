import {
  LayoutDashboard,
  Wallet,
  CreditCard,
  ArrowLeftRight,
  Tags,
  PieChart,
  BarChart3,
  Repeat,
  User,
  Settings,
  HeartPulse,
  Building2,
  type LucideIcon,
} from "lucide-react";

export interface NavItem {
  title: string;
  href: string;
  icon: LucideIcon;
  group: "main" | "user";
  premiumOnly?: boolean;
}

export const navigationItems: NavItem[] = [
  { title: "Dashboard", href: "/", icon: LayoutDashboard, group: "main" },
  { title: "Contas", href: "/accounts", icon: Wallet, group: "main" },
  { title: "Cartões", href: "/credit-cards", icon: CreditCard, group: "main" },
  { title: "Transações", href: "/transactions", icon: ArrowLeftRight, group: "main" },
  { title: "Categorias", href: "/categories", icon: Tags, group: "main" },
  { title: "Orçamentos", href: "/budgets", icon: PieChart, group: "main" },
  { title: "Saúde Financeira", href: "/financial-health", icon: HeartPulse, group: "main" },
  { title: "Recorrentes", href: "/recurring", icon: Repeat, group: "main" },
  { title: "Relatórios", href: "/reports", icon: BarChart3, group: "main" },
  { title: "Bancos", href: "/bank-connections", icon: Building2, group: "main", premiumOnly: true },
  { title: "Perfil", href: "/profile", icon: User, group: "user" },
  { title: "Configurações", href: "/settings", icon: Settings, group: "user" },
];
