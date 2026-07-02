import {
  LayoutDashboard,
  Landmark,
  ArrowLeftRight,
  Tags,
  PieChart,
  BarChart3,
  Repeat,
  User,
  Settings,
  HeartPulse,
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
  { title: "Bancos e Contas", href: "/bancos-e-contas", icon: Landmark, group: "main" },
  { title: "Transações", href: "/transactions", icon: ArrowLeftRight, group: "main" },
  { title: "Categorias", href: "/categories", icon: Tags, group: "main" },
  { title: "Orçamentos", href: "/budgets", icon: PieChart, group: "main" },
  { title: "Saúde Financeira", href: "/financial-health", icon: HeartPulse, group: "main" },
  { title: "Recorrentes", href: "/recurring", icon: Repeat, group: "main" },
  { title: "Relatórios", href: "/reports", icon: BarChart3, group: "main" },
  { title: "Perfil", href: "/profile", icon: User, group: "user" },
  { title: "Configurações", href: "/settings", icon: Settings, group: "user" },
];
