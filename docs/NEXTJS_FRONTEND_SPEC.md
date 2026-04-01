# MoneyManager — Especificacao Frontend React + Next.js

> Documento de especificacao tecnica para reconstrucao do frontend MoneyManager utilizando React, Next.js 15 e ecossistema moderno. Baseado na documentacao funcional do frontend Blazor existente (`FRONTEND_DOCUMENTATION.md`).
>
> **Objetivo:** Modernizar a apresentacao, remodelando telas, graficos, paginas mais funcionais e com dados mais coerentes por pagina, com visual profissional.

---

## Sumario

1. [Stack Tecnologico](#1-stack-tecnologico)
2. [Arquitetura e Estrutura de Pastas](#2-arquitetura-e-estrutura-de-pastas)
3. [Design System e UI Kit](#3-design-system-e-ui-kit)
4. [State Management e Cache](#4-state-management-e-cache)
5. [Autenticacao e Seguranca](#5-autenticacao-e-seguranca)
6. [Layout Principal e Navegacao](#6-layout-principal-e-navegacao)
7. [Paginas — Especificacao por Tela](#7-paginas--especificacao-por-tela)
   - 7.1 [Login e Registro](#71-login-e-registro)
   - 7.2 [Onboarding](#72-onboarding)
   - 7.3 [Dashboard](#73-dashboard)
   - 7.4 [Contas](#74-contas)
   - 7.5 [Transacoes](#75-transacoes)
   - 7.6 [Categorias](#76-categorias)
   - 7.7 [Orcamentos](#77-orcamentos)
   - 7.8 [Relatorios](#78-relatorios)
   - 7.9 [Transacoes Recorrentes](#79-transacoes-recorrentes)
   - 7.10 [Dashboard Cartao de Credito](#710-dashboard-cartao-de-credito)
   - 7.11 [Detalhes da Fatura](#711-detalhes-da-fatura)
   - 7.12 [Perfil](#712-perfil)
   - 7.13 [Configuracoes](#713-configuracoes)
   - 7.14 [Exclusao de Conta (LGPD)](#714-exclusao-de-conta-lgpd)
8. [Componentes Reutilizaveis](#8-componentes-reutilizaveis)
9. [Animacoes e Micro-interacoes](#9-animacoes-e-micro-interacoes)
10. [PWA e Notificacoes Push](#10-pwa-e-notificacoes-push)
11. [Internacionalizacao (i18n)](#11-internacionalizacao-i18n)
12. [Sistema de Temas](#12-sistema-de-temas)
13. [API Layer e Integracao](#13-api-layer-e-integracao)
14. [Performance e Otimizacao](#14-performance-e-otimizacao)
15. [Deploy e Infraestrutura](#15-deploy-e-infraestrutura)

---

## 1. Stack Tecnologico

### Core

| Tecnologia | Versao | Finalidade |
|---|---|---|
| **Next.js** | 15 (App Router) | Framework React com SSR/SSG, routing, API routes |
| **React** | 19 | Biblioteca de UI |
| **TypeScript** | 5.x | Tipagem estatica |

### UI e Design System

| Tecnologia | Finalidade |
|---|---|
| **shadcn/ui** | Componentes base acessiveis (Radix UI + Tailwind) |
| **Tailwind CSS** 4 | Utility-first CSS framework |
| **Framer Motion** | Animacoes declarativas e transicoes de pagina |
| **Recharts** | Graficos interativos (composable, baseado em D3) |
| **Lucide React** | Icones SVG consistentes |
| **next-themes** | Gerenciamento de temas (light/dark/system) |
| **tailwind-merge** + **clsx** | Class merging sem conflitos |
| **cmdk** | Command palette (busca rapida, atalhos) |

### State Management e Data Fetching

| Tecnologia | Finalidade |
|---|---|
| **TanStack Query (React Query)** v5 | Server state, cache, refetch, optimistic updates |
| **Zustand** | Client state global leve (auth, sidebar, modais) |
| **React Hook Form** + **Zod** | Formularios com validacao type-safe |
| **nuqs** | State na URL (filtros, paginacao, periodo) |

### Infraestrutura

| Tecnologia | Finalidade |
|---|---|
| **next-intl** | Internacionalizacao (i18n) |
| **next-pwa** (serwist) | Progressive Web App com service worker |
| **date-fns** | Manipulacao de datas (leve, tree-shakeable) |
| **currency.js** | Formatacao monetaria precisa |
| **sonner** | Toast notifications elegantes |
| **vaul** | Drawer para mobile |

---

## 2. Arquitetura e Estrutura de Pastas

```
money-manager-web/
├── public/
│   ├── icons/                      # Icones PWA (192, 512)
│   ├── manifest.json               # Manifesto PWA
│   └── locales/
│       ├── pt-BR.json              # Traducoes
│       ├── en-US.json
│       └── es-ES.json
├── src/
│   ├── app/                        # Next.js App Router
│   │   ├── (auth)/                 # Route group: paginas publicas
│   │   │   ├── login/page.tsx
│   │   │   ├── register/page.tsx
│   │   │   ├── account-deleted/page.tsx
│   │   │   └── layout.tsx          # Layout auth (sem sidebar)
│   │   ├── (dashboard)/            # Route group: paginas autenticadas
│   │   │   ├── page.tsx            # Dashboard (/)
│   │   │   ├── accounts/page.tsx
│   │   │   ├── transactions/page.tsx
│   │   │   ├── categories/page.tsx
│   │   │   ├── budgets/page.tsx
│   │   │   ├── reports/page.tsx
│   │   │   ├── recurring/page.tsx
│   │   │   ├── credit-cards/
│   │   │   │   └── [accountId]/page.tsx
│   │   │   ├── invoices/
│   │   │   │   └── [invoiceId]/page.tsx
│   │   │   ├── profile/page.tsx
│   │   │   ├── settings/page.tsx
│   │   │   ├── onboarding/page.tsx
│   │   │   └── layout.tsx          # Layout dashboard (sidebar + header)
│   │   ├── layout.tsx              # Root layout (providers)
│   │   └── globals.css
│   ├── components/
│   │   ├── ui/                     # shadcn/ui components
│   │   │   ├── button.tsx
│   │   │   ├── card.tsx
│   │   │   ├── dialog.tsx
│   │   │   ├── dropdown-menu.tsx
│   │   │   ├── input.tsx
│   │   │   ├── select.tsx
│   │   │   ├── sheet.tsx           # Side panel / drawer
│   │   │   ├── skeleton.tsx
│   │   │   ├── table.tsx
│   │   │   ├── tabs.tsx
│   │   │   ├── tooltip.tsx
│   │   │   ├── progress.tsx
│   │   │   ├── badge.tsx
│   │   │   ├── command.tsx         # Command palette (cmdk)
│   │   │   ├── chart.tsx           # Wrapper Recharts
│   │   │   └── ...
│   │   ├── layout/
│   │   │   ├── sidebar.tsx         # Sidebar colapsavel
│   │   │   ├── header.tsx          # Top bar com breadcrumb, search, user menu
│   │   │   ├── mobile-nav.tsx      # Bottom navigation mobile
│   │   │   └── breadcrumb.tsx
│   │   ├── shared/
│   │   │   ├── money-input.tsx     # Input monetario formatado
│   │   │   ├── color-picker.tsx    # Seletor de cores
│   │   │   ├── date-range-picker.tsx
│   │   │   ├── period-selector.tsx # Seletor de periodo com presets
│   │   │   ├── confirm-dialog.tsx  # Dialog de confirmacao reutilizavel
│   │   │   ├── empty-state.tsx     # Estado vazio com ilustracao
│   │   │   ├── stat-card.tsx       # Card de metrica com icone e tendencia
│   │   │   ├── page-header.tsx     # Cabecalho de pagina padronizado
│   │   │   └── loading-skeleton.tsx
│   │   ├── charts/
│   │   │   ├── pie-chart.tsx
│   │   │   ├── bar-chart.tsx
│   │   │   ├── line-chart.tsx
│   │   │   ├── area-chart.tsx
│   │   │   ├── radial-chart.tsx    # Gauge/progresso circular
│   │   │   └── chart-tooltip.tsx
│   │   ├── transactions/
│   │   │   ├── transaction-table.tsx
│   │   │   ├── transaction-form.tsx
│   │   │   ├── transaction-filters.tsx
│   │   │   └── installment-modal.tsx
│   │   ├── accounts/
│   │   │   ├── account-card.tsx
│   │   │   ├── account-form.tsx
│   │   │   └── invoice-payment-modal.tsx
│   │   ├── budgets/
│   │   │   ├── budget-card.tsx
│   │   │   ├── budget-wizard.tsx   # Wizard 3 etapas
│   │   │   └── budget-progress.tsx
│   │   ├── credit-cards/
│   │   │   ├── invoice-card.tsx
│   │   │   ├── invoice-status-badge.tsx
│   │   │   └── credit-limit-gauge.tsx
│   │   └── dashboard/
│   │       ├── overview-cards.tsx
│   │       ├── budget-usage-chart.tsx
│   │       ├── income-expense-chart.tsx
│   │       ├── accounts-chart.tsx
│   │       ├── recent-transactions.tsx
│   │       └── credit-card-summary.tsx
│   ├── hooks/
│   │   ├── use-accounts.ts         # TanStack Query hooks
│   │   ├── use-transactions.ts
│   │   ├── use-categories.ts
│   │   ├── use-budgets.ts
│   │   ├── use-invoices.ts
│   │   ├── use-recurring.ts
│   │   ├── use-dashboard.ts
│   │   ├── use-reports.ts
│   │   ├── use-profile.ts
│   │   ├── use-settings.ts
│   │   ├── use-auth.ts
│   │   ├── use-media-query.ts      # Responsive breakpoints
│   │   └── use-currency-format.ts  # Formatacao monetaria com locale
│   ├── lib/
│   │   ├── api-client.ts           # Fetch wrapper com JWT e refresh
│   │   ├── auth.ts                 # Logica de autenticacao
│   │   ├── query-client.ts         # Config do TanStack Query
│   │   ├── utils.ts                # cn(), formatters, helpers
│   │   └── validators.ts           # Schemas Zod compartilhados
│   ├── stores/
│   │   ├── auth-store.ts           # Zustand: token, user, isAuthenticated
│   │   ├── ui-store.ts             # Zustand: sidebar, modais, command palette
│   │   └── settings-store.ts       # Zustand: tema, idioma, moeda (client)
│   ├── types/
│   │   ├── account.ts
│   │   ├── transaction.ts
│   │   ├── category.ts
│   │   ├── budget.ts
│   │   ├── invoice.ts
│   │   ├── recurring.ts
│   │   ├── profile.ts
│   │   ├── settings.ts
│   │   └── api.ts                  # Tipos genericos de resposta API
│   └── config/
│       ├── navigation.ts           # Itens do menu
│       ├── currencies.ts           # Moedas suportadas
│       └── constants.ts            # Constantes globais
├── tailwind.config.ts
├── next.config.ts
├── tsconfig.json
├── package.json
├── Dockerfile
└── .env.example
```

---

## 3. Design System e UI Kit

### 3.1 Fundacao Visual

**Tipografia:**
- Font family: **Inter** (via `next/font/google`, auto-otimizada)
- Scale: 12px, 14px, 16px (base), 18px, 20px, 24px, 30px, 36px, 48px
- Headings: `font-semibold` ou `font-bold`
- Body: `font-normal`, line-height 1.5

**Espacamento:**
- Base unit: 4px (Tailwind default)
- Cards: `p-6`, gap entre secoes: `gap-6`
- Pagina: `px-4 md:px-6 lg:px-8`

**Border Radius:**
- Cards: `rounded-xl` (12px)
- Botoes: `rounded-lg` (8px)
- Inputs: `rounded-md` (6px)
- Badges: `rounded-full`
- Avatares: `rounded-full`

**Sombras:**
- Cards: `shadow-sm` em light, `shadow-none border` em dark
- Hover: `shadow-md` com transicao
- Modais: `shadow-xl`

### 3.2 Paleta de Cores (Design Tokens)

```css
/* Light Theme */
--background:        #fafafa;       /* fundo geral */
--card:              #ffffff;       /* fundo de cards */
--card-foreground:   #0a0a0a;       /* texto em cards */
--primary:           #6366f1;       /* indigo-500, acoes principais */
--primary-foreground:#ffffff;
--secondary:         #f4f4f5;       /* fundo secundario */
--muted:             #f4f4f5;       /* elementos desabilitados */
--muted-foreground:  #71717a;       /* texto secundario */
--accent:            #f4f4f5;       /* hover em menus */
--border:            #e4e4e7;       /* bordas */
--ring:              #6366f1;       /* focus ring */

/* Semanticas (financeiras) */
--income:            #22c55e;       /* verde: receitas */
--expense:           #ef4444;       /* vermelho: despesas */
--investment:        #3b82f6;       /* azul: investimentos */
--warning:           #f59e0b;       /* amarelo: alertas */
--credit-card:       #8b5cf6;       /* roxo: cartoes */

/* Dark Theme */
--background:        #09090b;
--card:              #18181b;
--card-foreground:   #fafafa;
--primary:           #818cf8;       /* indigo-400 */
--border:            #27272a;
/* ... demais invertidas */
```

### 3.3 Padroes de Card

Todos os cards seguem este padrao visual:

```tsx
<Card className="rounded-xl border bg-card shadow-sm hover:shadow-md transition-shadow">
  <CardHeader className="flex flex-row items-center justify-between pb-2">
    <CardTitle className="text-sm font-medium text-muted-foreground">
      Titulo
    </CardTitle>
    <Icon className="h-4 w-4 text-muted-foreground" />
  </CardHeader>
  <CardContent>
    <div className="text-2xl font-bold">R$ 12.500,00</div>
    <p className="text-xs text-muted-foreground mt-1">
      <span className="text-income">+12.5%</span> em relacao ao mes anterior
    </p>
  </CardContent>
</Card>
```

### 3.4 Stat Cards (Metrica com Tendencia)

Componente `<StatCard>` para metricas financeiras:

```tsx
interface StatCardProps {
  title: string;
  value: string;          // Valor formatado (ex: "R$ 12.500,00")
  icon: LucideIcon;
  trend?: {
    value: number;        // Percentual (ex: 12.5 ou -3.2)
    label: string;        // "vs mes anterior"
  };
  variant?: 'default' | 'income' | 'expense' | 'investment' | 'warning';
}
```

Visual:
- Icone no canto superior direito com fundo circular sutil
- Valor grande e bold
- Linha de tendencia com seta para cima (verde) ou para baixo (vermelho)
- Borda esquerda colorida pelo variant

---

## 4. State Management e Cache

### 4.1 TanStack Query — Server State

Toda comunicacao com a API usa TanStack Query para cache, deduplicacao e revalidacao.

**Configuracao global:**

```typescript
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000,      // 5 minutos
      gcTime: 10 * 60 * 1000,         // 10 minutos (garbage collection)
      retry: 1,
      refetchOnWindowFocus: false,
    },
  },
});
```

**Query Keys padronizadas:**

```typescript
export const queryKeys = {
  accounts:     ['accounts'] as const,
  account:      (id: string) => ['accounts', id] as const,
  categories:   ['categories'] as const,
  transactions: (filters: TransactionFilters) => ['transactions', filters] as const,
  budgets:      (month: string) => ['budgets', month] as const,
  invoices:     (accountId: string) => ['invoices', accountId] as const,
  invoice:      (id: string) => ['invoices', 'detail', id] as const,
  recurring:    ['recurring'] as const,
  dashboard:    (month: string) => ['dashboard', month] as const,
  reports:      (filters: ReportFilters) => ['reports', filters] as const,
  profile:      ['profile'] as const,
  settings:     ['settings'] as const,
};
```

**Exemplo de hook — `useAccounts`:**

```typescript
export function useAccounts() {
  return useQuery({
    queryKey: queryKeys.accounts,
    queryFn: () => apiClient.get<AccountResponseDto[]>('/api/accounts'),
  });
}

export function useCreateAccount() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: AccountRequestDto) =>
      apiClient.post<AccountResponseDto>('/api/accounts', data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: queryKeys.accounts });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      toast.success('Conta criada com sucesso');
    },
  });
}
```

**Optimistic Updates (transacoes):**

```typescript
export function useDeleteTransaction() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => apiClient.delete(`/api/transactions/${id}`),
    onMutate: async (id) => {
      // Cancel outgoing refetches
      await queryClient.cancelQueries({ queryKey: ['transactions'] });
      // Snapshot previous value
      const previous = queryClient.getQueriesData({ queryKey: ['transactions'] });
      // Optimistically remove
      queryClient.setQueriesData({ queryKey: ['transactions'] }, (old: any) => ({
        ...old,
        items: old.items.filter((t: any) => t.id !== id),
      }));
      return { previous };
    },
    onError: (_err, _id, context) => {
      // Rollback on error
      context?.previous?.forEach(([key, data]) =>
        queryClient.setQueryData(key, data)
      );
      toast.error('Erro ao excluir transacao');
    },
    onSettled: () => {
      queryClient.invalidateQueries({ queryKey: ['transactions'] });
      queryClient.invalidateQueries({ queryKey: ['dashboard'] });
      queryClient.invalidateQueries({ queryKey: queryKeys.accounts });
    },
  });
}
```

### 4.2 Zustand — Client State

**Auth Store:**

```typescript
interface AuthState {
  token: string | null;
  user: { id: string; name: string; email: string } | null;
  isAuthenticated: boolean;
  login: (token: string) => void;
  logout: () => void;
}
```

- Token armazenado em `sessionStorage` (mesmo comportamento do Blazor)
- Estado de usuario decodificado do JWT (claims)
- Ao montar a app, tenta restaurar token do `sessionStorage`

**UI Store:**

```typescript
interface UIState {
  sidebarOpen: boolean;
  sidebarCollapsed: boolean; // modo mini (so icones)
  commandOpen: boolean;      // command palette (Ctrl+K)
  toggleSidebar: () => void;
  toggleCommand: () => void;
}
```

### 4.3 URL State (nuqs)

Filtros e paginacao ficam na URL para permitir compartilhamento e deep linking:

```typescript
// Em /transactions
const [page, setPage] = useQueryState('page', parseAsInteger.withDefault(1));
const [type, setType] = useQueryState('type', parseAsString.withDefault('all'));
const [sortBy, setSortBy] = useQueryState('sortBy', parseAsString.withDefault('date_desc'));
const [startDate, setStartDate] = useQueryState('startDate');
const [endDate, setEndDate] = useQueryState('endDate');
const [accountId, setAccountId] = useQueryState('accountId');
```

---

## 5. Autenticacao e Seguranca

### 5.1 Fluxo de Autenticacao

```
[Login/Register] --> POST /api/auth/login --> JWT token
                                         --> sessionStorage
                                         --> Zustand auth store
                                         --> API client header
                                         --> Redirect

[Qualquer request] --> apiClient interceptor
                   --> Adiciona Authorization: Bearer {token}
                   --> Se 401: limpa token, redirect /login

[Logout] --> Limpa sessionStorage + Zustand
         --> Redirect /login
         --> queryClient.clear() (limpa cache)
```

### 5.2 API Client

```typescript
class ApiClient {
  private baseUrl: string;

  constructor() {
    this.baseUrl = process.env.NEXT_PUBLIC_API_URL || this.deriveApiUrl();
  }

  private deriveApiUrl(): string {
    if (typeof window === 'undefined') return '';
    const host = window.location.hostname;
    // Railway: money-manager.up.railway.app -> money-manager-api.up.railway.app
    if (host.includes('.up.railway.app')) {
      const subdomain = host.split('.')[0];
      return `https://${subdomain}-api.up.railway.app`;
    }
    return window.location.origin;
  }

  private getHeaders(): HeadersInit {
    const token = useAuthStore.getState().token;
    return {
      'Content-Type': 'application/json',
      ...(token && { Authorization: `Bearer ${token}` }),
    };
  }

  async get<T>(path: string): Promise<T> {
    const res = await fetch(`${this.baseUrl}${path}`, {
      headers: this.getHeaders(),
    });
    if (res.status === 401) {
      useAuthStore.getState().logout();
      throw new Error('Unauthorized');
    }
    if (!res.ok) throw new Error(await res.text());
    return res.json();
  }

  // post, put, delete similares...
}

export const apiClient = new ApiClient();
```

### 5.3 Middleware de Protecao de Rotas

```typescript
// src/app/(dashboard)/layout.tsx
export default function DashboardLayout({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuthStore();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (!isAuthenticated) {
      router.replace(`/login?returnUrl=${encodeURIComponent(pathname)}`);
    }
  }, [isAuthenticated]);

  if (!isAuthenticated) return <LoadingSkeleton />;

  return (
    <div className="flex h-screen">
      <Sidebar />
      <div className="flex-1 flex flex-col overflow-hidden">
        <Header />
        <main className="flex-1 overflow-auto p-4 md:p-6">
          {children}
        </main>
      </div>
    </div>
  );
}
```

---

## 6. Layout Principal e Navegacao

### 6.1 Desktop Layout

```
┌──────────────────────────────────────────────────────────┐
│ [=] MoneyManager          [Ctrl+K busca]  [🔔] [Avatar ▾]│
├──────┬───────────────────────────────────────────────────┤
│      │                                                    │
│  📊  │  Page Content                                      │
│  💳  │                                                    │
│  📈  │  ┌──────┐ ┌──────┐ ┌──────┐ ┌──────┐             │
│  🏷️  │  │Card 1│ │Card 2│ │Card 3│ │Card 4│             │
│  💰  │  └──────┘ └──────┘ └──────┘ └──────┘             │
│  📋  │                                                    │
│  🔄  │  ┌────────────────────────────────┐               │
│  💳  │  │                                │               │
│      │  │        Charts / Tables         │               │
│ ──── │  │                                │               │
│  👤  │  └────────────────────────────────┘               │
│  ⚙️  │                                                    │
└──────┴───────────────────────────────────────────────────┘
```

**Sidebar (Desktop):**
- Largura: 240px expandida, 64px colapsada (so icones)
- Toggle via botao hamburguer no header
- Estado persiste em `localStorage`
- Animacao suave com Framer Motion (`layout` prop)
- Logo no topo (com/sem texto conforme estado)
- Separador visual entre secoes principais e configuracoes
- Indicador de pagina ativa com fundo sutil + borda esquerda colorida
- Tooltip nos icones quando colapsada

**Itens de navegacao:**

| Grupo | Item | Icone (Lucide) | Rota |
|---|---|---|---|
| Principal | Dashboard | `LayoutDashboard` | `/` |
| | Contas | `Wallet` | `/accounts` |
| | Transacoes | `ArrowLeftRight` | `/transactions` |
| | Categorias | `Tags` | `/categories` |
| | Orcamentos | `PieChart` | `/budgets` |
| | Relatorios | `BarChart3` | `/reports` |
| | Recorrentes | `Repeat` | `/recurring` |
| Separador | — | — | — |
| Usuario | Perfil | `User` | `/profile` |
| | Configuracoes | `Settings` | `/settings` |

### 6.2 Mobile Layout

- Sidebar oculta, substituida por **bottom navigation** fixo (5 itens principais)
- Menu completo acessivel via hamburguer que abre `<Sheet>` (drawer lateral)
- Bottom nav: Dashboard, Contas, Transacoes (com FAB "+"), Orcamentos, Mais

```
┌────────────────────────────────┐
│ MoneyManager     [🔔] [Avatar] │
├────────────────────────────────┤
│                                │
│        Page Content            │
│        (scroll)                │
│                                │
├────────────────────────────────┤
│ 📊   💳   [+]   💰   •••    │
│ Home Contas Add  Orc. Mais    │
└────────────────────────────────┘
```

### 6.3 Header

- **Breadcrumb** automatico baseado na rota (ex: `Dashboard > Contas > Editar`)
- **Command Palette** ativado por `Ctrl+K` (ou icone de busca)
  - Busca rapida por: paginas, contas, categorias, acoes
  - Implementado com `cmdk` (shadcn Command)
- **Notificacoes** (icone sino com badge de contagem)
- **User menu** (dropdown): nome, email, foto, links para Perfil, Configuracoes, Sair

---

## 7. Paginas — Especificacao por Tela

### 7.1 Login e Registro

**Rota:** `/login`, `/register`
**Layout:** Auth layout (sem sidebar, fundo gradiente)

**Design:**
- Fundo com gradiente animado sutil (indigo -> purple, `animate-gradient`)
- Card central com blur (`backdrop-blur-sm bg-card/80`)
- Logo MoneyManager com icone
- Formulario limpo com labels flutuantes ou acima do input
- Botao primario com loading state (spinner inline)
- Link entre Login <-> Register
- Mensagens de erro inline abaixo do campo

**Login — Campos:**

| Campo | Tipo | Validacao (Zod) |
|---|---|---|
| Email | `email` | `z.string().email()` |
| Senha | `password` | `z.string().min(1, "Obrigatorio")` |

**Register — Campos:**

| Campo | Tipo | Validacao (Zod) |
|---|---|---|
| Nome | `text` | `z.string().min(2)` |
| Email | `email` | `z.string().email()` |
| Senha | `password` | `z.string().min(6)` |
| Confirmar Senha | `password` | `.refine(val => val === senha)` |

**Fluxo pos-registro:** Register -> auto-login -> redirect `/onboarding`

**API:**
- Login: `POST /api/auth/login` `{ email, password }` -> `{ token }`
- Register: `POST /api/auth/register` `{ name, email, password, confirmPassword }`

---

### 7.2 Onboarding

**Rota:** `/onboarding`
**Layout:** Dashboard layout (com sidebar)

**Design:**
- Card central com ilustracao animada (Framer Motion `staggerChildren`)
- Icone de foguete com animacao de "launch" (translateY + scale)
- Titulo: "Bem-vindo ao MoneyManager!"
- 3 cards de acao com icone, titulo e subtitulo:
  1. `Wallet` -> "Criar primeira conta" -> `/accounts`
  2. `Tags` -> "Configurar categorias" -> `/categories`
  3. `LayoutDashboard` -> "Ir para o Dashboard" -> `/`
- Animacao `stagger` nos cards (aparecem um a um com delay)
- Confetti sutil ao carregar (via `canvas-confetti`)

**API:** `POST /api/onboarding/complete` (chamado no mount)

---

### 7.3 Dashboard

**Rota:** `/` (pagina inicial)
**Layout:** Dashboard layout

Esta e a pagina mais importante. Deve ser visualmente impactante e informativa.

**Estrutura (grid responsivo):**

```
Desktop (lg):
┌──────────┬──────────┬──────────┬──────────┐
│ Saldo    │ Ativos   │ Receitas │ Despesas │   <- StatCards
│ Liquido  │ Totais   │ do Mes   │ do Mes   │
└──────────┴──────────┴──────────┴──────────┘
┌────────────────────┬───────────────────────┐
│                    │                       │
│  Receitas vs       │  Despesas por         │
│  Despesas          │  Categoria            │
│  (AreaChart)       │  (DonutChart)         │
│                    │                       │
└────────────────────┴───────────────────────┘
┌────────────────────┬───────────────────────┐
│                    │                       │
│  Saldos por        │  Uso do Orcamento     │
│  Conta (BarChart)  │  (RadialChart gauge)  │
│                    │                       │
└────────────────────┴───────────────────────┘
┌────────────────────────────────────────────┐
│  Transacoes Recentes (tabela compacta)     │
│  ultimas 10, com link "Ver todas"          │
└────────────────────────────────────────────┘
┌────────────────────────────────────────────┐
│  Cartoes de Credito (cards horizontais)    │
│  com gauge de utilizacao do limite         │
└────────────────────────────────────────────┘

Mobile (sm):
Tudo empilhado verticalmente, 1 coluna.
StatCards em grid 2x2.
```

**StatCards (4 cards no topo):**

| Card | Icone | Cor | Calculo |
|---|---|---|---|
| Saldo Liquido | `Wallet` | income/expense dinamico | Soma Balance (excl. CC e Investment) |
| Total em Ativos | `TrendingUp` | primary | Soma Balance (excl. CC) |
| Receitas do Mes | `ArrowUpCircle` | income | Soma Amount onde Type=Income |
| Despesas do Mes | `ArrowDownCircle` | expense | Soma Amount onde Type=Expense |

Cada card mostra tendencia vs mes anterior (se dados disponiveis).

**Grafico: Receitas vs Despesas (AreaChart):**
- Tipo: `AreaChart` (Recharts) com gradiente de preenchimento
- Eixo X: dias do mes (1-31)
- Duas areas: receitas (verde com `fill-opacity: 0.1`) e despesas (vermelho)
- Tooltip customizado mostrando valores formatados
- Grid sutil de fundo

**Grafico: Despesas por Categoria (DonutChart):**
- Tipo: Donut (PieChart com innerRadius)
- Centro: valor total formatado
- Legendas abaixo ou ao lado com porcentagem
- Cores das categorias
- Hover: destaca fatia + tooltip com nome, valor e %

**Grafico: Saldos por Conta (BarChart):**
- Barras horizontais ou verticais
- Cada barra = uma conta (excluindo CC)
- Cor = cor da conta
- Label do valor dentro ou acima da barra

**Grafico: Uso do Orcamento (RadialChart):**
- Gauge circular (0-100%)
- Centro: "X% utilizado"
- Cor: verde <= 75%, amarelo <= 90%, vermelho > 90%
- Se nao houver orcamento: estado vazio com CTA

**Transacoes Recentes:**
- Tabela compacta (sem bordas, rows com hover)
- Colunas: data (relativa: "Hoje", "Ontem", "3 dias atras"), descricao, categoria (badge), valor (colorido)
- Maximo 10 itens
- Link "Ver todas as transacoes ->"

**Cartoes de Credito:**
- Cards horizontais com scroll se necessario
- Cada card: nome, gauge circular de utilizacao, valor usado / limite
- Cores do gauge: verde <= 60%, amarelo <= 80%, vermelho > 80%
- Link "Ver dashboard ->" para `/credit-cards/{id}`

**API Calls (paralelas via `Promise.all` ou queries independentes do TanStack Query):**
- `GET /api/accounts`
- `GET /api/transactions?page=1&pageSize=100&startDate={1oDiaMes}&endDate={ultimoDiaMes}`
- `GET /api/categories`
- `GET /api/budgets/{mesAtual}`

**Loading State:**
- Skeleton animado para cada secao (cards, graficos, tabela)
- Carregamento progressivo: cards aparecem primeiro, graficos depois

---

### 7.4 Contas

**Rota:** `/accounts`

**Design:**
- Header: titulo "Contas" + botao "Nova Conta" (primario)
- Controle de ordenacao (dropdown: Tipo, Nome, Saldo)
- Grid de cards responsivo: `grid-cols-1 md:grid-cols-2 xl:grid-cols-3`

**Account Card:**

```
┌─────────────────────────────────┐
│ 🏦 Conta Corrente         [•••] │  <- Icone do tipo + dropdown menu (editar/excluir)
│                                  │
│ Nubank                           │  <- Nome da conta
│                                  │
│ R$ 5.230,45                      │  <- Saldo (grande, bold)
│ BRL                              │  <- Moeda (badge sutil)
│                                  │
│ [Para cartoes de credito:]       │
│ ████████░░░░  65%                │  <- Barra de progresso
│ Usado: R$ 3.250   Disp: R$ 1.750│
│ Limite: R$ 5.000                 │
│                                  │
│ [Ver Dashboard]                  │  <- Apenas para CC
└─────────────────────────────────┘
```

- Cards de cartao de credito tem visual diferenciado (borda colorida sutil, secao de limite)
- Dropdown menu (3 pontos): Editar, Ver Dashboard (CC), Pagar Fatura (CC), Excluir

**Form (Sheet/Dialog):**
- Abre em `Sheet` lateral (desktop) ou `Dialog` fullscreen (mobile)
- Campos condicionais para cartao de credito com animacao `AnimatePresence`
- Color picker integrado

**Campos:**

| Campo | Tipo | Obrigatorio | Condicional |
|---|---|---|---|
| Nome | text | Sim | |
| Tipo | select | Sim | |
| Saldo Inicial | MoneyInput | Sim | Apenas criacao |
| Moeda | select | Sim | |
| Cor | ColorPicker | Sim | |
| Dia Fechamento | number (1-28) | Sim | Type=CreditCard |
| Offset Vencimento | number (1-30) | Sim | Type=CreditCard |
| Limite de Credito | MoneyInput | Sim | Type=CreditCard |

**Tipos e icones (Lucide):**

| Tipo | Icone | Label |
|---|---|---|
| Checking | `Building` | Conta Corrente |
| Savings | `PiggyBank` | Poupanca |
| Cash | `Banknote` | Dinheiro |
| CreditCard | `CreditCard` | Cartao de Credito |
| Investment | `TrendingUp` | Investimento |

**Ordenacao:** Persistida em `localStorage["accountSortPreference"]`

**Modal de Pagamento de Fatura:**
- Dialog com selecao de fatura pendente, conta de debito, data e valor
- Toggle entre pagamento integral e parcial
- Validacao: valor <= restante da fatura

---

### 7.5 Transacoes

**Rota:** `/transactions`

**Design:**
- Header: titulo + botao "Nova Transacao"
- Barra de filtros colapsavel (sempre visivel em desktop, toggle em mobile)
- Tabela responsiva com paginacao

**Filtros (inline, com URL state via `nuqs`):**

| Filtro | Componente | Opcoes |
|---|---|---|
| Periodo | `PeriodSelector` | Mes atual, Personalizado (date range picker) |
| Tipo | `Select` | Todos, Receitas, Despesas, Investimentos |
| Conta | `Select` | Todas, lista de contas |
| Ordenacao | `Select` | Data (recente), Data (antigo), Maior valor, Menor valor |

**Tabela de Transacoes:**

| Coluna | Desktop | Mobile |
|---|---|---|
| Data | `dd/MM/yyyy` | Oculta (data no subtitulo) |
| Descricao | Texto | Texto (principal) |
| Categoria | Badge com cor | Badge pequeno |
| Conta | Nome | Oculta |
| Tipo | Badge (verde/vermelho/azul) | Cor no valor |
| Valor | Formatado + cor | Formatado + cor |
| Acoes | Editar, Excluir | Menu dropdown |

**Mobile:** Transacoes exibidas como lista de cards empilhados ao inves de tabela:

```
┌────────────────────────────────┐
│ 🛒 Supermercado     -R$ 350,00 │
│ Alimentacao  •  Nubank  •  15/03│
└────────────────────────────────┘
```

**Paginacao:**
- Tamanho: 50 por pagina
- Controles: Anterior, Proxima, com indicador "Pagina X de Y"
- Info: "Mostrando 1-50 de 234 transacoes"

**Form de Transacao (Dialog):**

| Campo | Tipo | Obrigatorio |
|---|---|---|
| Descricao | text | Sim |
| Valor | MoneyInput | Sim |
| Data | DatePicker | Sim (default: hoje) |
| Tipo | SegmentedControl | Sim (Receita/Despesa/Investimento) |
| Conta | Select | Sim |
| Categoria | Select (filtrado por tipo) | Sim |
| Observacoes | Textarea | Nao |
| Compra parcelada | Switch | Nao (apenas Despesa) |
| Num. parcelas | Number | Se parcelado (min 2) |
| 1a parcela na fatura atual | Checkbox | Se parcelado + CC |

**Fluxo de parcelamento:**
1. Ativar switch "Compra parcelada"
2. Preencher parcelas
3. Ao salvar: modal de confirmacao (InstallmentModal)
4. Mostra: valor total, num parcelas, valor unitario
5. Confirmar -> `POST /api/transactions/installment-purchase`
6. `ClientRequestId` gerado automaticamente (idempotencia)

---

### 7.6 Categorias

**Rota:** `/categories`

**Design:**
- Header: titulo + botao "Nova Categoria"
- Tabs: "Todas" | "Receitas" | "Despesas"
- Grid de cards: `grid-cols-2 md:grid-cols-3 lg:grid-cols-4`

**Category Card:**

```
┌────────────────────────┐
│ ● Alimentacao    [•••] │  <- Circulo de cor + nome + menu
│                        │
│ 🟢 Despesa             │  <- Badge de tipo
└────────────────────────┘
```

- Cards compactos com hover effect
- Dropdown menu: Editar, Excluir

**Form (Dialog):**

| Campo | Tipo | Obrigatorio |
|---|---|---|
| Nome | text | Sim |
| Tipo | SegmentedControl | Sim (Receita / Despesa) |
| Cor | ColorPicker (16 cores + custom) | Sim |

**Cor padrao:** `#6366f1` (indigo)

---

### 7.7 Orcamentos

**Rota:** `/budgets`

**Design:**
- Seletor de mes (mesNavigation com setas < > e label do mes)
- Resumo do mes no topo: total orcado, total gasto, % utilizado
- Botoes: "Novo Orcamento", "Copiar do Mes Anterior" (condicional)
- Grid de budget cards

**Budget Card (por categoria):**

```
┌────────────────────────────────────┐
│ ● Alimentacao                       │
│                                     │
│ R$ 1.350,00 / R$ 2.000,00         │
│ ████████████████░░░░░  67.5%       │  <- Barra de progresso com cor
│                                     │
│ Restante: R$ 650,00                │
└────────────────────────────────────┘
```

- Barra de progresso: verde <= 75%, amarelo <= 90%, vermelho > 90%
- Se estourado (> 100%): valor em vermelho, badge "ESTOURADO"

**Wizard de Criacao (3 etapas em Dialog):**

1. **Etapa 1 — Mes:** Seletor de mes
2. **Etapa 2 — Categorias:** Grid de categorias Expense como toggle cards (clicaveis, com animacao de selecao)
3. **Etapa 3 — Limites:** Para cada categoria selecionada, input MoneyInput. Default: R$ 500,00

- Indicador de progresso no topo do wizard (steps: 1/3, 2/3, 3/3)
- Navegacao: Voltar / Proximo / Salvar
- Animacao de transicao entre etapas (`AnimatePresence` com `slideInRight`)

---

### 7.8 Relatorios

**Rota:** `/reports`

**Design:**
- Seletor de periodo com presets rapidos (tabs): Mes Atual, Mes Anterior, 3 Meses, 6 Meses, Ano, Custom
- 4 StatCards no topo
- Graficos lado a lado
- Tabela de detalhamento

**StatCards:**

| Card | Calculo |
|---|---|
| Total Receitas | Soma Amount (Income) |
| Total Despesas | Soma Amount (Expense) |
| Saldo Liquido | Receitas - Despesas |
| Taxa de Poupanca | (Receitas - Despesas) / Receitas * 100% |

**Graficos:**

1. **Donut — Despesas por Categoria:**
   - Centro: valor total
   - Fatias com legenda lateral
   - Hover: tooltip com valor e %

2. **AreaChart — Tendencias Mensais (6 meses):**
   - Duas areas: receitas (verde) e despesas (vermelho)
   - Grid sutil, tooltip formatado
   - Eixo X: meses (Jan, Fev, Mar...)

**Tabela de Detalhamento:**

| Coluna | Descricao |
|---|---|
| Categoria | Badge de cor + nome |
| Total | Valor formatado |
| % do Total | Porcentagem |
| Progresso | Barra inline proporcional |

Ordenada por valor (maior primeiro).

---

### 7.9 Transacoes Recorrentes

**Rota:** `/recurring`

**Design:**
- 3 StatCards no topo: Receitas Recorrentes, Despesas Recorrentes, Liquido
- Botao "Nova Recorrente"
- Grid de cards

**Card Individual:**

```
┌────────────────────────────────────┐
│ 🟢 Receita               [Ativa]  │
│                                     │
│ Salario                             │
│ R$ 5.000,00  •  Mensal             │
│                                     │
│ Conta: Nubank                       │
│ Categoria: Renda                    │
│ Proxima: 05/04/2026                │
│ Ultima: 05/03/2026                 │
│                                     │
│ [Editar]  [Excluir]               │
└────────────────────────────────────┘
```

- Badge de status: "Ativa" (verde) ou "Inativa" (cinza)
- Badge de tipo com cor
- Frequencia traduzida

**Form (Dialog):**

| Campo | Tipo | Obrigatorio |
|---|---|---|
| Descricao | text | Sim |
| Valor | MoneyInput | Sim |
| Tipo | SegmentedControl | Sim |
| Conta | Select | Sim |
| Categoria | Select (filtrado) | Sim |
| Frequencia | Select (7 opcoes) | Sim |
| Data Inicio | DatePicker | Sim (default: hoje) |
| Data Fim | DatePicker | Nao |
| Ativa | Switch | Sim (default: true) |
| Observacoes | Textarea | Nao |

---

### 7.10 Dashboard Cartao de Credito

**Rota:** `/credit-cards/[accountId]`

**Design:**
- Header com nome do cartao + link "Voltar para Contas"
- Layout em grid

**Secoes:**

1. **Limite de Credito (Card com Gauge):**
   - Gauge circular (RadialChart) com % de utilizacao
   - Valores: limite, usado, disponivel
   - Cores do gauge: verde <= 60%, amarelo <= 80%, vermelho > 80%

2. **Fatura Atual (Card destaque):**
   - Status "ABERTA" em badge
   - Periodo, valor total, dias ate fechamento
   - Se nenhuma fatura aberta: estado vazio

3. **Proxima Fatura a Vencer (Card):**
   - Status, valor total, restante, vencimento
   - Se vencida: card com borda vermelha, badge "VENCIDA" pulsante
   - Botao "Pagar Fatura"

4. **Historico de Faturas (Tabela):**
   - Colunas: Mes, Periodo, Total, Pago, Restante, Status, Acoes
   - Link "Ver Detalhes" -> `/invoices/{id}`
   - StatusBadge: Open=azul, Closed=amarelo, Paid=verde, PartiallyPaid=amarelo, Overdue=vermelho

---

### 7.11 Detalhes da Fatura

**Rota:** `/invoices/[invoiceId]`

**Design:**

1. **4 StatCards:** Total da Fatura, Valor Pago, Restante, Vencimento
2. **Info do Periodo:** Mes referencia, data inicio-fim, status badge
3. **Detalhamento por Categoria:**
   - Cards com: nome da categoria, valor gasto, barra de progresso (% do total), qtd transacoes
4. **Lista de Transacoes da Fatura:** Tabela com data, descricao, categoria, valor
5. **Botao Pagar Fatura** (se nao totalmente paga)

---

### 7.12 Perfil

**Rota:** `/profile`

**Design:**
- Card com avatar grande, nome, email
- Secao editavel com toggle view/edit mode
- Cards separados para: Informacoes Pessoais, Alterar Senha, Alterar Email
- Secao de exclusao de conta (LGPD) no final

**Informacoes Pessoais:**

| Campo | Tipo | Obrigatorio |
|---|---|---|
| Nome | text | Sim |
| Nome Completo | text | Nao |
| Telefone | text | Nao |
| URL da Foto | text | Nao |

**Alterar Senha:**

| Campo | Tipo |
|---|---|
| Senha Atual | password |
| Nova Senha | password |
| Confirmar Nova Senha | password |

Validacao: nova == confirmacao (client-side)

**Alterar Email:** Dialog com campo de novo email + senha para confirmacao.

---

### 7.13 Configuracoes

**Rota:** `/settings`

**Design:**
- Cards organizados por secao
- Mudancas aplicadas imediatamente (sem botao "Salvar" global, cada secao salva independente)

**Secao 1 — Preferencias Financeiras:**

| Campo | Tipo |
|---|---|
| Moeda | Select (BRL, USD, EUR, GBP, JPY, ARS, CLP, COP, MXN, PEN) |
| Formato de Data | Select (dd/MM/yyyy, MM/dd/yyyy, yyyy-MM-dd) |
| Dia Fechamento do Mes | Number (1-28) |
| Orcamento Padrao | MoneyInput |

**Secao 2 — Notificacoes Push:**

| Campo | Tipo |
|---|---|
| Habilitar Notificacoes | Switch |
| Notificar Recorrentes Processadas | Switch |
| Lembrete Diario | Switch |
| Botao Testar | Button |

**Secao 3 — Aparencia:**

| Campo | Tipo |
|---|---|
| Tema | SegmentedControl (Claro / Escuro / Automatico) |
| Cor de Destaque | ColorPicker |

- Tema aplicado instantaneamente via `next-themes`
- Cor de destaque atualiza CSS custom property em tempo real

---

### 7.14 Exclusao de Conta (LGPD)

**Localizacao:** Secao inferior da pagina `/profile`

**Design:**
- Card com fundo `bg-destructive/5` e borda `border-destructive/20`
- Expansivel (accordion) — clica para revelar
- Exibe contagem de dados (contas, transacoes, categorias, orcamentos, recorrentes)
- 3 barreiras de confirmacao com progress indicator

**Barreiras:**
1. Input de senha
2. Input de texto exato: "EXCLUIR MINHA CONTA"
3. Checkbox: "Compreendo que esta acao e irreversivel"

- Botao "Excluir Minha Conta Permanentemente" habilitado apenas quando todas as 3 barreiras cumpridas
- Botao vermelho com icone `Trash2`, com confirmacao final via Dialog
- Pos-exclusao: limpa tudo, redireciona para `/account-deleted`

**Pagina `/account-deleted`:**
- Layout auth (sem sidebar)
- Card de confirmacao LGPD com mensagem de agradecimento
- Links: "Criar nova conta" e "Fazer login"

---

## 8. Componentes Reutilizaveis

### 8.1 MoneyInput

Input monetario com formatacao automatica.

```tsx
interface MoneyInputProps {
  value: number;
  onChange: (value: number) => void;
  currency?: string;       // default: "BRL"
  className?: string;
  disabled?: boolean;
}
```

**Comportamento:**
- **Blur:** Exibe valor formatado (ex: `R$ 1.500,00`)
- **Focus:** Exibe valor numerico cru (ex: `1500`)
- **Parsing:** Aceita `,` e `.` como separador decimal
- Usa `currency.js` para precisao

### 8.2 ColorPicker

```tsx
interface ColorPickerProps {
  value: string;           // hex
  onChange: (hex: string) => void;
}
```

- 16 cores predefinidas em grid 4x4
- Input hex custom
- Preview circular da cor selecionada
- Implementado via Popover do shadcn

### 8.3 PeriodSelector

```tsx
interface PeriodSelectorProps {
  startDate: Date;
  endDate: Date;
  onChange: (start: Date, end: Date) => void;
  presets?: Array<{ label: string; getValue: () => [Date, Date] }>;
}
```

- Presets como tabs clicaveis (Mes Atual, Mes Anterior, 3 Meses, 6 Meses, Ano)
- Mode "Custom" abre DateRangePicker
- DateRangePicker usa calendario duplo (shadcn Calendar)

### 8.4 ConfirmDialog

```tsx
interface ConfirmDialogProps {
  open: boolean;
  onConfirm: () => void;
  onCancel: () => void;
  title: string;
  description: string;
  variant?: 'default' | 'destructive';
  confirmText?: string;
  loading?: boolean;
}
```

- Substitui todos os `confirm()` nativos do Blazor
- Variant `destructive`: botao vermelho, icone de alerta
- Loading state no botao de confirmar

### 8.5 EmptyState

```tsx
interface EmptyStateProps {
  icon: LucideIcon;
  title: string;
  description: string;
  action?: { label: string; onClick: () => void };
}
```

- Exibido quando listas estao vazias
- Ilustracao (icone grande sutil), titulo, descricao
- Botao de acao opcional (ex: "Criar primeira transacao")

### 8.6 PageHeader

```tsx
interface PageHeaderProps {
  title: string;
  description?: string;
  actions?: React.ReactNode;   // Botoes no canto direito
}
```

- Titulo da pagina + descricao opcional
- Slot para botoes de acao (alinhados a direita)
- Responsivo: acoes abaixo do titulo em mobile

---

## 9. Animacoes e Micro-interacoes

### 9.1 Transicoes de Pagina

```tsx
// src/app/(dashboard)/template.tsx
'use client';
import { motion } from 'framer-motion';

export default function Template({ children }: { children: React.ReactNode }) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 8 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.2, ease: 'easeOut' }}
    >
      {children}
    </motion.div>
  );
}
```

### 9.2 Animacoes de Lista

Cards e itens de lista usam `staggerChildren`:

```tsx
const container = {
  hidden: { opacity: 0 },
  show: {
    opacity: 1,
    transition: { staggerChildren: 0.05 },
  },
};

const item = {
  hidden: { opacity: 0, y: 10 },
  show: { opacity: 1, y: 0 },
};
```

### 9.3 Micro-interacoes

| Elemento | Animacao |
|---|---|
| Cards | `hover:shadow-md hover:-translate-y-0.5 transition-all` |
| Botoes | `active:scale-[0.98] transition-transform` |
| Toggles/Switches | `transition-colors duration-200` |
| Sidebar toggle | `layout` animation (Framer Motion) |
| Numeros em StatCards | `useSpring` counter animation (0 -> valor) |
| Barras de progresso | `transition-all duration-700 ease-out` no width |
| Graficos | Animacao nativa do Recharts (`isAnimationActive`) |
| Toast notifications | Slide-in via Sonner |
| Skeleton loading | `animate-pulse` do Tailwind |
| Exclusao de item | `AnimatePresence` + `exit={{ opacity: 0, height: 0 }}` |
| FAB mobile | `animate-bounce` sutil ao aparecer |
| Command palette | `scale` + `opacity` transition |

### 9.4 Numeros Animados (Counter)

Para valores monetarios nos StatCards:

```tsx
function AnimatedNumber({ value, format }: { value: number; format: (n: number) => string }) {
  const spring = useSpring(0, { stiffness: 100, damping: 30 });
  const display = useTransform(spring, (v) => format(v));

  useEffect(() => { spring.set(value); }, [value]);

  return <motion.span>{display}</motion.span>;
}
```

---

## 10. PWA e Notificacoes Push

### 10.1 Configuracao PWA

Usar `serwist` (fork moderno do Workbox para Next.js):

```typescript
// next.config.ts
import withSerwistInit from '@serwist/next';

const withSerwist = withSerwistInit({
  swSrc: 'src/sw.ts',
  swDest: 'public/sw.js',
});

export default withSerwist(nextConfig);
```

**Estrategia de cache:**
- Assets estaticos (JS, CSS, imagens): `CacheFirst` com TTL de 30 dias
- Paginas: `NetworkFirst` com fallback offline
- Chamadas API (`/api/*`): `NetworkOnly` (nunca cachear dados financeiros)

### 10.2 Push Notifications

- Mesma integracao VAPID da versao Blazor
- `PushManager.subscribe()` no browser
- Envio da subscription para `POST /api/push/subscribe`
- Service Worker intercepta `push` events e exibe notificacoes nativas

---

## 11. Internacionalizacao (i18n)

### Configuracao com `next-intl`

```
public/locales/
  pt-BR.json    # Portugues (default, completo)
  en-US.json    # Ingles
  es-ES.json    # Espanhol
  fr-FR.json    # Frances
```

**Uso nos componentes:**

```tsx
import { useTranslations } from 'next-intl';

function DashboardPage() {
  const t = useTranslations('Dashboard');
  return <h1>{t('title')}</h1>; // "Visao Geral"
}
```

**Estrutura do JSON:**

```json
{
  "Common": {
    "save": "Salvar",
    "cancel": "Cancelar",
    "delete": "Excluir",
    "edit": "Editar",
    "create": "Criar",
    "loading": "Carregando...",
    "confirm": "Confirmar",
    "back": "Voltar"
  },
  "Navigation": {
    "dashboard": "Dashboard",
    "accounts": "Contas",
    "transactions": "Transacoes",
    "categories": "Categorias",
    "budgets": "Orcamentos",
    "reports": "Relatorios",
    "recurring": "Recorrentes",
    "profile": "Perfil",
    "settings": "Configuracoes"
  },
  "Dashboard": {
    "title": "Visao Geral",
    "netBalance": "Saldo Liquido",
    "totalAssets": "Total em Ativos",
    "monthIncome": "Receitas do Mes",
    "monthExpense": "Despesas do Mes"
  }
}
```

---

## 12. Sistema de Temas

### Implementacao com `next-themes`

```tsx
// Root layout
import { ThemeProvider } from 'next-themes';

<ThemeProvider attribute="class" defaultTheme="system" enableSystem>
  {children}
</ThemeProvider>
```

**Tailwind config:**

```typescript
// tailwind.config.ts
export default {
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        income: 'hsl(var(--income))',
        expense: 'hsl(var(--expense))',
        investment: 'hsl(var(--investment))',
      },
    },
  },
};
```

**CSS tokens em HSL (para composabilidade):**

```css
@layer base {
  :root {
    --background: 0 0% 98%;
    --foreground: 0 0% 4%;
    --card: 0 0% 100%;
    --primary: 239 84% 67%;
    --income: 142 71% 45%;
    --expense: 0 84% 60%;
    --investment: 217 91% 60%;
  }

  .dark {
    --background: 0 0% 4%;
    --foreground: 0 0% 98%;
    --card: 0 0% 9%;
    --primary: 239 84% 74%;
    --income: 142 71% 55%;
    --expense: 0 84% 70%;
    --investment: 217 91% 70%;
  }
}
```

---

## 13. API Layer e Integracao

### 13.1 Mapa de Endpoints

A API backend permanece a mesma. O frontend consome todos os endpoints documentados:

**Autenticacao:**
| Metodo | Endpoint |
|---|---|
| POST | `/api/auth/login` |
| POST | `/api/auth/register` |

**Contas:**
| Metodo | Endpoint |
|---|---|
| GET | `/api/accounts` |
| POST | `/api/accounts` |
| PUT | `/api/accounts/{id}` |
| DELETE | `/api/accounts/{id}` |

**Categorias:**
| Metodo | Endpoint |
|---|---|
| GET | `/api/categories` |
| POST | `/api/categories` |
| PUT | `/api/categories/{id}` |
| DELETE | `/api/categories/{id}` |

**Transacoes:**
| Metodo | Endpoint |
|---|---|
| GET | `/api/transactions` (com query params) |
| POST | `/api/transactions` |
| POST | `/api/transactions/installment-purchase` |
| PUT | `/api/transactions/{id}` |
| DELETE | `/api/transactions/{id}` |

**Transacoes Recorrentes:**
| Metodo | Endpoint |
|---|---|
| GET | `/api/recurringtransactions` |
| POST | `/api/recurringtransactions` |
| PUT | `/api/recurringtransactions/{id}` |
| DELETE | `/api/recurringtransactions/{id}` |

**Orcamentos:**
| Metodo | Endpoint |
|---|---|
| GET | `/api/budgets/{month}` |
| POST | `/api/budgets` |
| PUT | `/api/budgets/{id}` |

**Faturas de Cartao:**
| Metodo | Endpoint |
|---|---|
| GET | `/api/credit-card-invoices/{id}` |
| GET | `/api/credit-card-invoices/{id}/summary` |
| GET | `/api/credit-card-invoices/{id}/transactions` |
| GET | `/api/credit-card-invoices/accounts/{accountId}/open` |
| POST | `/api/credit-card-invoices/accounts/{accountId}/history` |
| GET | `/api/credit-card-invoices/pending` |
| GET | `/api/credit-card-invoices/overdue` |
| POST | `/api/credit-card-invoices/{id}/close` |
| POST | `/api/credit-card-invoices/pay` |
| POST | `/api/credit-card-invoices/pay-partial` |
| POST | `/api/credit-card-invoices/{id}/recalculate` |
| GET | `/api/credit-card-invoices/accounts/{accountId}/determine?transactionDate=` |

**Perfil:**
| Metodo | Endpoint |
|---|---|
| GET | `/api/profile` |
| PUT | `/api/profile` |
| POST | `/api/profile/change-password` |
| POST | `/api/profile/update-email` |

**Configuracoes:**
| Metodo | Endpoint |
|---|---|
| GET | `/api/settings` |
| PUT | `/api/settings` |

**Onboarding:**
| Metodo | Endpoint |
|---|---|
| GET | `/api/onboarding/status` |
| GET | `/api/onboarding/category-suggestions` |
| POST | `/api/onboarding/complete` |

**Exclusao de Conta (LGPD):**
| Metodo | Endpoint |
|---|---|
| GET | `/api/accountdeletion/data-count` |
| POST | `/api/accountdeletion/delete-account` |

**Push Notifications:**
| Metodo | Endpoint |
|---|---|
| GET | `/api/push/public-key` |
| GET | `/api/push/status` |
| POST | `/api/push/subscribe` |
| POST | `/api/push/unsubscribe` |
| POST | `/api/push/test` |

### 13.2 API Client

```typescript
// src/lib/api-client.ts

const API_URL = process.env.NEXT_PUBLIC_API_URL;

async function fetchWithAuth<T>(
  path: string,
  options: RequestInit = {}
): Promise<T> {
  const token = useAuthStore.getState().token;

  const res = await fetch(`${API_URL}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token && { Authorization: `Bearer ${token}` }),
      ...options.headers,
    },
  });

  if (res.status === 401) {
    useAuthStore.getState().logout();
    if (typeof window !== 'undefined') {
      window.location.href = '/login';
    }
    throw new Error('Unauthorized');
  }

  if (!res.ok) {
    const error = await res.text();
    throw new Error(error || `HTTP ${res.status}`);
  }

  return res.json();
}

export const apiClient = {
  get: <T>(path: string) => fetchWithAuth<T>(path),
  post: <T>(path: string, body?: unknown) =>
    fetchWithAuth<T>(path, { method: 'POST', body: JSON.stringify(body) }),
  put: <T>(path: string, body?: unknown) =>
    fetchWithAuth<T>(path, { method: 'PUT', body: JSON.stringify(body) }),
  delete: <T>(path: string) =>
    fetchWithAuth<T>(path, { method: 'DELETE' }),
};
```

---

## 14. Performance e Otimizacao

### 14.1 Code Splitting

- Next.js App Router faz code splitting automatico por rota
- Componentes pesados (graficos, modais) carregados via `dynamic()`:

```tsx
const PieChart = dynamic(() => import('@/components/charts/pie-chart'), {
  loading: () => <Skeleton className="h-[300px]" />,
  ssr: false,
});
```

### 14.2 Imagens

- Avatares e icones via `next/image` com otimizacao automatica
- Icones SVG via Lucide (tree-shakeable, apenas icones usados no bundle)

### 14.3 Bundle

- Tailwind CSS purgado automaticamente (apenas classes usadas)
- `date-fns` tree-shakeable (importar apenas funcoes usadas)
- Recharts importar componentes individuais (`import { PieChart, Pie } from 'recharts'`)

### 14.4 Cache e Prefetch

- TanStack Query com `staleTime: 5min` evita refetch desnecessario
- `queryClient.prefetchQuery` para pre-carregar dados de paginas adjacentes
- Next.js prefetch automatico de links visiveis

### 14.5 Virtualizacao

Para listas grandes de transacoes (relatorios com pageSize=10000):
- Usar `@tanstack/react-virtual` para renderizar apenas rows visiveis
- Melhora drastica de performance com grandes datasets

---

## 15. Deploy e Infraestrutura

### 15.1 Dockerfile

```dockerfile
# Build
FROM node:20-alpine AS builder
WORKDIR /app
COPY package*.json ./
RUN npm ci
COPY . .

# Injectar API URL no build
ARG NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL

RUN npm run build

# Runtime
FROM node:20-alpine AS runner
WORKDIR /app
ENV NODE_ENV=production

COPY --from=builder /app/.next/standalone ./
COPY --from=builder /app/.next/static ./.next/static
COPY --from=builder /app/public ./public

EXPOSE 3000
CMD ["node", "server.js"]
```

### 15.2 Configuracao Railway

**`railway.web.toml`:**
```toml
[build]
builder = "DOCKERFILE"
dockerfilePath = "Dockerfile.web"

[build.args]
NEXT_PUBLIC_API_URL = "${{API_URL}}"

[deploy]
restartPolicyType = "ON_FAILURE"
restartPolicyMaxRetries = 10
```

**Variaveis de ambiente no Railway:**

| Variavel | Valor | Descricao |
|---|---|---|
| `API_URL` | `https://money-manager-api.up.railway.app` | URL da API |
| `NEXT_PUBLIC_API_URL` | `${{API_URL}}` | Referencia a mesma variavel (Railway resolve) |

### 15.3 Next.js Config

```typescript
// next.config.ts
import type { NextConfig } from 'next';

const nextConfig: NextConfig = {
  output: 'standalone', // Para Docker otimizado
  images: {
    remotePatterns: [
      { protocol: 'https', hostname: '**' }, // Avatares externos
    ],
  },
};

export default nextConfig;
```

---

> **Nota:** Este documento e a especificacao tecnica completa para reconstrucao do frontend. A API backend (ASP.NET Core + MongoDB) permanece inalterada — apenas o frontend e substituido. Todos os endpoints, DTOs e regras de negocio estao mapeados para garantir paridade funcional com o frontend Blazor existente.
