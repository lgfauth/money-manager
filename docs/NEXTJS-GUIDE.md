# Next.js Developer Guide — MoneyManager

## 1. Repository Overview

The monorepo contains two Next.js applications under `src/Frontends/`:

| Project | Path | Purpose | Next.js | React |
|---|---|---|---|---|
| **MoneyManager.Web** | `src/Frontends/MoneyManager.Web` | User-facing personal finance app (transactions, budgets, accounts, credit cards, reports, settings, PWA) | 15.5 | 19.1 |
| **MoneyManager.Backoffice** | `src/Frontends/MoneyManager.Backoffice` | Internal admin portal (job control, observability, audit, financial maintenance, legal documents) | 16.2 | 19.2 |

### Key differences between the two apps

| | MoneyManager.Web | MoneyManager.Backoffice |
|---|---|---|
| Design system | shadcn/ui + Tailwind v4 | Plain CSS (no component library) |
| Data fetching | TanStack Query v5 via custom hooks | Raw `fetch` + `useEffect/useState` |
| State management | Zustand + TanStack Query | Local `useState` only |
| i18n | `next-intl` (pt-BR default) | None |
| Form handling | `react-hook-form` + Zod | Controlled inputs only |
| Auth mechanism | JWT in `sessionStorage` + httpOnly cookie | httpOnly cookie (server-side only) |
| Complexity | Full feature-rich app | Minimal operational tool |

---

## 2. Architecture

### 2.1 Router Type and Folder Structure

Both apps use the **App Router** (Next.js 13+). There are no `pages/` directories.

#### MoneyManager.Web (`src/Frontends/MoneyManager.Web/src/`)

```
app/
  (auth)/           ← route group: public pages (login, register, forgot-password)
    login/
    register/
  (dashboard)/      ← route group: protected pages (require authentication)
    layout.tsx      ← client-side auth guard + main shell (sidebar, header, mobile-nav)
    page.tsx        ← dashboard home
    transactions/
    accounts/
    budgets/
    categories/
    credit-cards/
    reports/
    settings/
    profile/
    recurring/
    onboarding/
  layout.tsx        ← root layout: fonts, next-intl provider, Providers wrapper
  globals.css
components/
  ui/               ← shadcn/ui primitives (button, dialog, input, etc.)
  layout/           ← sidebar, header, mobile-nav, whats-new-modal
  shared/           ← reusable cross-feature components (page-header, empty-state, confirm-dialog)
  transactions/     ← feature-specific components
  accounts/
  budgets/
  credit-cards/
  receipts/
  lgpd/
config/
  constants.ts      ← app-wide constants (DEFAULT_PAGE_SIZE, DEFAULT_CURRENCY, COLOR_PRESETS)
  legal.ts          ← LGPD terms URLs and version
  navigation.ts     ← sidebar nav items
hooks/
  use-transactions.ts   ← TanStack Query hooks per domain
  use-accounts.ts
  use-auth.ts
  ...
i18n/
  config.ts         ← next-intl configuration (locale list, default locale)
  request.ts        ← server-side locale resolution for next-intl
lib/
  api-client.ts     ← fetch wrapper (fetchWithAuth, apiClient object)
  api-errors.ts     ← ApiClientError class + error message extraction
  query-client.ts   ← QueryClient singleton + queryKeys registry
  utils.ts          ← clsx/cn helpers
  validators.ts     ← Zod schemas
stores/
  auth-store.ts     ← Zustand auth state
  breadcrumb-store.ts
  money-privacy-store.ts
  settings-store.ts
  ui-store.ts
types/
  transaction.ts    ← TypeScript interfaces mirroring API DTOs
  account.ts
  ...
middleware.ts       ← Next.js middleware: cookie-based redirect for same-origin deploys
```

#### MoneyManager.Backoffice (`src/Frontends/MoneyManager.Backoffice/src/`)

```
app/
  (secure)/         ← route group: protected pages
    layout.tsx      ← server component: sidebar nav + logout button
    page.tsx        ← overview dashboard
    jobs/
    errors-latency/
    financial-maintenance/
    audit/
    documents/
  api/              ← Next.js API routes (proxy + auth endpoints)
  login/
  layout.tsx        ← root layout (minimal: fonts only)
  globals.css
components/
  logout-button.tsx
lib/
  admin-api.ts      ← fetch helpers for the admin API (request, postJson, putJson, deleteJson)
  admin-auth.ts     ← auth token helpers (httpOnly cookie, no JS access)
```

### 2.2 Component Architecture

#### MoneyManager.Web

The split strategy is **feature-driven with explicit `"use client"` when needed**:

- **Server Components** (default): `layout.tsx` at the root level, static pages that do not require interactivity or auth state.
- **Client Components** (`"use client"`): Every page under `(dashboard)/` and nearly all feature components. The dashboard `layout.tsx` is a Client Component because it reads Zustand auth state and performs client-side routing.

The convention is: if a component uses `useState`, `useEffect`, any Zustand store, any TanStack Query hook, or browser-only APIs, add `"use client"` at the top of the file.

#### MoneyManager.Backoffice

All pages are Client Components (`"use client"`) because they use `useEffect` + `useState` for data fetching. The root `layout.tsx` is a Server Component (no client-side imports).

### 2.3 Data Flow

#### MoneyManager.Web

```
API (MoneyManager.Api.Operational)
    ↓
apiClient (lib/api-client.ts)        ← adds Bearer token, handles 401 logout
    ↓
Domain hook (hooks/use-transactions.ts)  ← TanStack Query useQuery/useMutation
    ↓
Page / Component
```

1. The `apiClient` object (`lib/api-client.ts`) wraps `fetch` with auth headers and error normalization.
2. Each domain has a dedicated hook file (`hooks/use-*.ts`) that calls `apiClient` through TanStack Query.
3. Pages and components import only the hook — they never call `apiClient` directly.

#### MoneyManager.Backoffice

```
Admin API (MoneyManager.Api.Backoffice)
    ↓
admin-api.ts helper functions     ← adds Bearer token from cookie (server-side proxy mode)
    ↓
Page component useEffect()
```

1. `lib/admin-api.ts` exports plain async functions (no hooks).
2. Pages call these functions directly inside `useEffect`.

---

## 3. How to Build New Features

### 3.1 Adding a New Page

**MoneyManager.Web — protected page under `(dashboard)/`**:

**Step 1 — Create the route file**:

```
src/app/(dashboard)/my-feature/page.tsx
```

**Step 2 — Implement the page component**:

```tsx
// src/app/(dashboard)/my-feature/page.tsx
"use client";

import { PageHeader } from "@/components/shared/page-header";
import { useMyFeature } from "@/hooks/use-my-feature";

export default function MyFeaturePage() {
  const { data, isLoading } = useMyFeature();

  if (isLoading) {
    return <div className="flex h-32 items-center justify-center">
      <div className="h-6 w-6 animate-spin rounded-full border-4 border-primary border-t-transparent" />
    </div>;
  }

  return (
    <div>
      <PageHeader title="My Feature" />
      {/* content */}
    </div>
  );
}
```

**Step 3 — Add to navigation** in `src/config/navigation.ts` if the page should appear in the sidebar.

**For a page with metadata** (SEO / PWA), export `metadata` from a Server Component wrapper. Since most dashboard pages are Client Components, metadata is typically set only at the layout level — do not add `export const metadata` to a `"use client"` file (it will be silently ignored by Next.js).

### 3.2 Connecting to a BFF Endpoint

**Step 1 — Define the TypeScript types** in `src/types/`:

```typescript
// src/types/my-feature.ts
export interface MyFeatureRequestDto {
  name: string;
  amount: number;
}

export interface MyFeatureResponseDto {
  id: string;
  name: string;
  amount: number;
  createdAt: string;
}
```

**Step 2 — Add the query key** in `src/lib/query-client.ts`:

```typescript
export const queryKeys = {
  // ... existing keys
  myFeature: ["my-feature"] as const,
  myFeatureById: (id: string) => ["my-feature", id] as const,
};
```

**Step 3 — Create the hook file** in `src/hooks/`:

```typescript
// src/hooks/use-my-feature.ts
"use client";

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { getApiErrorMessage } from "@/lib/api-errors";
import { queryKeys } from "@/lib/query-client";
import type { MyFeatureRequestDto, MyFeatureResponseDto } from "@/types/my-feature";
import { toast } from "sonner";

export function useMyFeature() {
  return useQuery({
    queryKey: queryKeys.myFeature,
    queryFn: () => apiClient.get<MyFeatureResponseDto[]>("/api/my-feature"),
  });
}

export function useCreateMyFeature() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: MyFeatureRequestDto) =>
      apiClient.post<MyFeatureResponseDto>("/api/my-feature", data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.myFeature });
      toast.success("Criado com sucesso");
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao criar")),
  });
}
```

**Authentication** is handled automatically by `fetchWithAuth` in `api-client.ts` — the `Bearer` token is read from `useAuthStore.getState().token` and appended to every request. Do not add auth headers manually in hooks or components.

**Step 4 — Use in the page**:

```tsx
const { data, isLoading, error } = useMyFeature();
```

### 3.3 Adding a New Component

**Decision: Server vs Client Component**

- Does it use `useState`, `useEffect`, event handlers, or Zustand/TanStack Query? → `"use client"`
- Is it purely presentational with only props? → Server Component (no directive needed)

**File placement**:
- Reusable across features → `src/components/shared/`
- Feature-specific → `src/components/my-feature/`
- shadcn/ui primitive extension → `src/components/ui/`

**Example — new shared component**:

```tsx
// src/components/shared/amount-badge.tsx
import { cn } from "@/lib/utils";

interface AmountBadgeProps {
  amount: number;
  currency?: string;
  className?: string;
}

export function AmountBadge({ amount, currency = "BRL", className }: AmountBadgeProps) {
  const isPositive = amount >= 0;

  return (
    <span className={cn(
      "inline-flex items-center rounded-md px-2 py-1 text-sm font-medium",
      isPositive ? "bg-green-100 text-green-700" : "bg-red-100 text-red-700",
      className
    )}>
      {new Intl.NumberFormat("pt-BR", { style: "currency", currency }).format(amount)}
    </span>
  );
}
```

Props must always be typed with a `Props` interface (not inline or `React.FC`).

### 3.4 Adding a Form

Forms use **`react-hook-form` + Zod** validation. All schemas live in `src/lib/validators.ts`.

**Step 1 — Add the Zod schema** to `src/lib/validators.ts`:

```typescript
export const myFeatureSchema = z.object({
  name: z.string().min(1, "Nome obrigatório"),
  amount: z.number().positive("Valor deve ser positivo"),
});

export type MyFeatureFormData = z.infer<typeof myFeatureSchema>;
```

**Step 2 — Build the form component**:

```tsx
// src/components/my-feature/my-feature-form.tsx
"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { myFeatureSchema, type MyFeatureFormData } from "@/lib/validators";
import { useCreateMyFeature } from "@/hooks/use-my-feature";
import { Button } from "@/components/ui/button";

interface MyFeatureFormProps {
  onSuccess?: () => void;
}

export function MyFeatureForm({ onSuccess }: MyFeatureFormProps) {
  const createMutation = useCreateMyFeature();

  const {
    register,
    handleSubmit,
    formState: { errors },
  } = useForm<MyFeatureFormData>({
    resolver: zodResolver(myFeatureSchema),
  });

  const onSubmit = (data: MyFeatureFormData) => {
    createMutation.mutate(data, { onSuccess });
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-4">
      <div>
        <label htmlFor="name">Nome</label>
        <input id="name" {...register("name")} />
        {errors.name && <p className="text-sm text-red-500">{errors.name.message}</p>}
      </div>
      <Button type="submit" disabled={createMutation.isPending}>
        {createMutation.isPending ? "Salvando..." : "Salvar"}
      </Button>
    </form>
  );
}
```

### 3.5 Adding a New Route with Parameters

**Dynamic segment** — create the folder with brackets:

```
src/app/(dashboard)/my-feature/[id]/page.tsx
```

**Read the parameter and fetch data**:

```tsx
// src/app/(dashboard)/my-feature/[id]/page.tsx
"use client";

import { useParams } from "next/navigation";
import { useQuery } from "@tanstack/react-query";
import { apiClient } from "@/lib/api-client";
import { queryKeys } from "@/lib/query-client";
import type { MyFeatureResponseDto } from "@/types/my-feature";

export default function MyFeatureDetailPage() {
  const params = useParams<{ id: string }>();

  const { data, isLoading } = useQuery({
    queryKey: queryKeys.myFeatureById(params.id),
    queryFn: () => apiClient.get<MyFeatureResponseDto>(`/api/my-feature/${params.id}`),
    enabled: !!params.id,
  });

  if (isLoading) return <div>Carregando...</div>;
  if (!data) return <div>Não encontrado</div>;

  return <div>{data.name}</div>;
}
```

### 3.6 Global State

The app uses **Zustand** for client-side global state and **TanStack Query** for server state.

- **Server state** (data that lives in the API) → always use TanStack Query hooks. Do not copy API data into Zustand.
- **Client-only state** (auth tokens, UI preferences, breadcrumbs, money privacy toggle) → Zustand stores.

**Adding a new Zustand store** (follow the pattern from `stores/ui-store.ts`):

```typescript
// src/stores/my-store.ts
import { create } from "zustand";

interface MyState {
  isOpen: boolean;
  setIsOpen: (value: boolean) => void;
}

export const useMyStore = create<MyState>((set) => ({
  isOpen: false,
  setIsOpen: (value) => set({ isOpen: value }),
}));
```

Existing stores:
- `auth-store.ts` — user, token, isAuthenticated, isHydrated; `login`, `logout`, `hydrate`
- `breadcrumb-store.ts` — page breadcrumb labels
- `money-privacy-store.ts` — whether monetary values are hidden
- `settings-store.ts` — persisted user display preferences (currency, locale)
- `ui-store.ts` — sidebar open/collapsed state

### 3.7 Adding a Protected Route

There are two layers of route protection:

**Layer 1 — `middleware.ts`** (edge, cookie-based, same-origin only):

```typescript
// middleware.ts already handles cookie-based redirects for same-origin deploys.
// To add a new public path that should NOT redirect to login, add it to PUBLIC_PATHS:
const PUBLIC_PATHS = [
  "/login",
  "/register",
  "/my-new-public-page",   // ← add here
];
```

**Layer 2 — `(dashboard)/layout.tsx`** (client-side, cross-origin fallback):

The layout already guards all routes under `(dashboard)/` via:

```typescript
useEffect(() => {
  if (isHydrated && !isAuthenticated) {
    router.replace(`/login?returnUrl=${encodeURIComponent(pathname)}`);
  }
}, [isHydrated, isAuthenticated, router, pathname]);
```

New protected pages placed inside the `(dashboard)/` route group are automatically protected by this guard. No additional code is needed.

---

## 4. Code Conventions

### 4.1 Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Page files | `page.tsx` | `app/(dashboard)/transactions/page.tsx` |
| Layout files | `layout.tsx` | `app/(dashboard)/layout.tsx` |
| Component files | `kebab-case.tsx` | `transaction-table.tsx`, `confirm-dialog.tsx` |
| Hook files | `use-kebab-case.ts` | `use-transactions.ts` |
| Store files | `kebab-case-store.ts` | `auth-store.ts` |
| Type/interface files | `kebab-case.ts` | `transaction.ts` |
| Library/util files | `kebab-case.ts` | `api-client.ts`, `query-client.ts` |
| Config files | `kebab-case.ts` | `constants.ts`, `navigation.ts` |
| Component functions | PascalCase | `TransactionTable`, `ConfirmDialog` |
| Hook functions | `use` prefix + camelCase | `useTransactions`, `useCreateTransaction` |
| Type/interface names | PascalCase | `TransactionResponseDto`, `TransactionFilters` |
| Props interfaces | component name + `Props` | `TransactionTableProps` |
| Enum members | PascalCase | `TransactionType.Income` |

### 4.2 TypeScript Conventions

- **Strict mode** is enabled (`tsconfig.json`).
- Use `interface` for object shapes that may be extended (component props, API DTOs). Use `type` for unions, computed types, or Zod inferred types.
- API response types mirror the server DTOs. They are stored in `src/types/` and named `*RequestDto` / `*ResponseDto` to match the server naming convention.
- Avoid `any`. Prefer `unknown` when the type is genuinely unknown, then narrow with type guards.
- Zod schemas in `src/lib/validators.ts` are the source of truth for form types — derive the TypeScript type with `z.infer<typeof schema>`.
- The `apiClient` methods are generic: `apiClient.get<T>()`, `apiClient.post<T>()` etc. Always provide the return type parameter.

### 4.3 Styling Conventions

**MoneyManager.Web** uses **Tailwind CSS v4** with the following rules:

- Use the `cn()` utility from `src/lib/utils.ts` (combination of `clsx` + `tailwind-merge`) when class names are conditional:
  ```tsx
  import { cn } from "@/lib/utils";
  <div className={cn("base-classes", condition && "conditional-class", className)} />
  ```
- Prefer Tailwind utilities over custom CSS. Custom CSS is only in `globals.css` for base styles and login page layout.
- Color palette: primary brand color is `#00C896` (defined as `DEFAULT_COLOR` in `config/constants.ts`). Use the Tailwind `primary` token in components.
- Dark mode: managed by `next-themes`. Use `dark:` Tailwind variants for dark mode overrides.
- Responsive: mobile-first. Use `md:`, `lg:` prefixes for larger breakpoints. The sidebar is hidden on mobile; `MobileNav` appears instead.
- Do not use inline `style` attributes for layout; use Tailwind classes.

**MoneyManager.Backoffice** uses plain CSS classes defined in `globals.css`. There is no Tailwind.

### 4.4 Environment Variables

| Variable | Project | Description |
|---|---|---|
| `NEXT_PUBLIC_API_URL` | Web | Base URL of `MoneyManager.Api.Operational` (empty string = same origin) |
| `NEXT_PUBLIC_ADMIN_API_URL` | Backoffice | Base URL of `MoneyManager.Api.Backoffice`; falls back to `/api/proxy` if not set or contains placeholder |

**Rules**:
- Variables read on the client must have the `NEXT_PUBLIC_` prefix.
- Server-only secrets must NOT use the `NEXT_PUBLIC_` prefix.
- Default to empty string `""` or `process.env.NEXT_PUBLIC_API_URL ?? ""` — never hardcode URLs.
- Add new variables to `.env.example` (both projects have one).
- In `api-client.ts`, the base URL is read once at module load: `const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "";`

---

## 5. Good Practices Observed in This Codebase

- **Centralized API client**: All HTTP calls go through `lib/api-client.ts`. This is the single place where auth headers are attached and 401 handling (logout + redirect) is enforced. No component or hook ever calls `fetch` directly with a hardcoded URL.

- **Typed query keys**: `lib/query-client.ts` exports a `queryKeys` registry. All `useQuery` and `invalidateQueries` calls reference these constants, preventing cache key typos.

- **`ApiClientError` for structured error messages**: The custom error class carries `statusCode`, `messages[]`, and `data`. `getApiErrorMessage()` extracts the first useful message from the server's `ApiErrorResponse` structure for display in toasts.

- **TanStack Query for all server state**: Fetched data is never stored in `useState`. The hooks in `src/hooks/` own the data lifecycle — loading, error, stale, and invalidation.

- **Zod schemas as the single source of form types**: The schema in `validators.ts` defines validation rules; `z.infer<typeof schema>` produces the TypeScript type. There is no duplication between the schema and the form type.

- **Toast feedback on mutations**: Every `useMutation` has `onSuccess` and `onError` handlers that call `toast.success()` / `toast.error()`. User feedback is never left to the component that calls the mutation.

- **Middleware + layout guard dual protection**: Same-origin deploys get fast edge-level redirect from `middleware.ts`. Cross-origin deploys (e.g., API on a different Railway domain) fall back gracefully to the client-side layout guard.

- **Auth hydration on app start**: `Providers.tsx` calls `useAuthStore.getState().hydrate()` via `useEffect` once on mount. This hits `/api/auth/me` to verify the cookie and populate the Zustand store. Components wait for `isHydrated` before checking `isAuthenticated`.

- **Money privacy toggle**: `money-privacy-store.ts` + `use-money-privacy.ts` allow users to hide all monetary values across the app. Components read from this store rather than implementing their own hide/show logic.

- **Centralized constants**: `src/config/constants.ts` holds `DEFAULT_PAGE_SIZE`, `DEFAULT_CURRENCY`, color presets, and threshold percentages. No magic numbers are scattered through the codebase.

---

## 6. What NOT to Do

### Making API calls directly in components

```tsx
// WRONG — raw fetch inside a component
export function TransactionList() {
  const [data, setData] = useState([]);
  useEffect(() => {
    fetch("https://api.moneymanager.app/api/transactions")
      .then(r => r.json())
      .then(setData);
  }, []);
}

// CORRECT — use the hook
export function TransactionList() {
  const { data } = useTransactions(filters);
}
```

### Calling `apiClient` directly in a component or page

```tsx
// WRONG — component calls apiClient directly
export function BudgetCard() {
  useEffect(() => {
    apiClient.get<BudgetResponseDto[]>("/api/budgets").then(setData);
  }, []);
}

// CORRECT — call apiClient only inside a hook in src/hooks/
export function useBudgets() {
  return useQuery({
    queryKey: queryKeys.budgets(month),
    queryFn: () => apiClient.get<BudgetResponseDto[]>("/api/budgets"),
  });
}
```

### Unnecessary `"use client"` on server components

```tsx
// WRONG — forces client bundle for a purely static page
"use client";
export default function TermsPage() {
  return <main><h1>Termos de Uso</h1>...</main>;
}

// CORRECT — no directive; it's a Server Component
export default function TermsPage() {
  return <main><h1>Termos de Uso</h1>...</main>;
}
```

### Storing server state in Zustand

```typescript
// WRONG — duplicating TanStack Query state in a Zustand store
const useTransactionStore = create((set) => ({
  transactions: [],
  setTransactions: (txs) => set({ transactions: txs }),
}));

// CORRECT — TanStack Query owns all server data
const { data: transactions } = useTransactions(filters);
```

### Hardcoding the API base URL

```typescript
// WRONG
const res = await fetch("https://api.moneymanager.app/api/transactions");

// CORRECT
const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "";
const res = await fetch(`${API_URL}/api/transactions`);
// Or use apiClient which already reads NEXT_PUBLIC_API_URL
```

### Missing loading and error states

```tsx
// WRONG — no loading or error handling
export default function AccountsPage() {
  const { data } = useAccounts();
  return <ul>{data?.map(a => <li key={a.id}>{a.name}</li>)}</ul>;
}

// CORRECT
export default function AccountsPage() {
  const { data, isLoading, error } = useAccounts();

  if (isLoading) return <TableSkeleton />;
  if (error) return <p>Erro ao carregar contas.</p>;
  if (!data?.length) return <EmptyState message="Nenhuma conta cadastrada" />;

  return <ul>{data.map(a => <li key={a.id}>{a.name}</li>)}</ul>;
}
```

### Accessing `useAuthStore` in Server Components

```typescript
// WRONG — Zustand is client-only; this crashes in a Server Component
import { useAuthStore } from "@/stores/auth-store";
export default function Layout() {
  const { isAuthenticated } = useAuthStore(); // ReferenceError: window is not defined
}

// CORRECT — place any component that uses Zustand inside a Client Component (add "use client")
```

---

## 7. Next.js Version Notes

### MoneyManager.Web — Next.js 15 (15.5.x)

Features in active use:
- **App Router** with route groups `(auth)` and `(dashboard)`
- **`next-intl` v4** for internationalization (configured via `createNextIntlPlugin` in `next.config.ts`)
- **`next/font`** with `Sora` and `DM_Sans` (loaded via `next/font/google` with subset optimization)
- **`next/image`** configured to allow all `https` hostnames (for remote user profile pictures)
- **Turbopack** (`next dev --turbopack`, `next build --turbopack`) — the default dev/build bundler
- **`output: "standalone"`** — builds a self-contained server bundle for Docker/Railway deployment
- **PWA metadata** — `manifest.json`, `appleWebApp`, `viewport.themeColor` in root layout
- **`nuqs`** — URL search param state management (used for filter/pagination state in table pages)

Deprecated patterns to avoid:
- Do not use `getServerSideProps` or `getStaticProps` — these are Pages Router APIs
- Do not use `next/head` — use the `metadata` export from Server Components instead
- Do not use `next/router` — use `next/navigation` (`useRouter`, `usePathname`, `useSearchParams`)

### MoneyManager.Backoffice — Next.js 16 (16.2.x)

This project runs on a newer major version. Features in use:
- **App Router** with route group `(secure)`
- **`output: "standalone"`** for containerized deployment
- **Next.js API routes** in `app/api/` used as a server-side proxy (injects admin auth cookie into requests forwarded to `MoneyManager.Api.Backoffice`)

Deprecated patterns to avoid:
- Same as Web: no Pages Router APIs
- The admin API client (`lib/admin-api.ts`) explicitly checks for a placeholder string in `NEXT_PUBLIC_ADMIN_API_URL` and falls back to `/api/proxy`. Do not remove this check.
