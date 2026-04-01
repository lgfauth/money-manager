# MoneyManager — Documentação Completa do Frontend

> Documentação detalhada de todas as funcionalidades, regras de negócio, chamadas à API e fluxos de dados do frontend Blazor WebAssembly do MoneyManager.
>
> **Organização:** Fases funcionais, do mais simples ao mais complexo.

---

## Sumário

1. [Visão Geral da Arquitetura](#1-visão-geral-da-arquitetura)
2. [Stack Tecnológico](#2-stack-tecnológico)
3. [Estrutura de Pastas](#3-estrutura-de-pastas)
4. [Configuração e Inicialização (Program.cs)](#4-configuração-e-inicialização-programcs)
5. [Autenticação e Autorização](#5-autenticação-e-autorização)
6. [Componentes Compartilhados (Shared)](#6-componentes-compartilhados-shared)
7. [Componentes Reutilizáveis](#7-componentes-reutilizáveis)
8. [Fase 1 — Onboarding (Boas-vindas)](#8-fase-1--onboarding-boas-vindas)
9. [Fase 2 — Categorias](#9-fase-2--categorias)
10. [Fase 3 — Contas](#10-fase-3--contas)
11. [Fase 4 — Transações](#11-fase-4--transações)
12. [Fase 5 — Transações Recorrentes](#12-fase-5--transações-recorrentes)
13. [Fase 6 — Orçamentos (Budgets)](#13-fase-6--orçamentos-budgets)
14. [Fase 7 — Dashboard](#14-fase-7--dashboard)
15. [Fase 8 — Relatórios](#15-fase-8--relatórios)
16. [Fase 9 — Cartões de Crédito e Faturas](#16-fase-9--cartões-de-crédito-e-faturas)
17. [Fase 10 — Perfil do Usuário](#17-fase-10--perfil-do-usuário)
18. [Fase 11 — Configurações](#18-fase-11--configurações)
19. [Fase 12 — Exclusão de Conta (LGPD)](#19-fase-12--exclusão-de-conta-lgpd)
20. [PWA, Service Worker e Push Notifications](#20-pwa-service-worker-e-push-notifications)
21. [Localização (i18n)](#21-localização-i18n)
22. [Sistema de Temas (Light/Dark)](#22-sistema-de-temas-lightdark)
23. [Mapa Completo de Endpoints da API](#23-mapa-completo-de-endpoints-da-api)

---

## 1. Visão Geral da Arquitetura

O frontend do MoneyManager é uma **Single Page Application (SPA)** construída com **Blazor WebAssembly (.NET 9)**. A aplicação é servida como assets estáticos pelo navegador e se comunica com a API backend via HTTP/REST com autenticação JWT.

### Diagrama de Alto Nível

```
┌─────────────────────────────────┐
│     Navegador (Browser)         │
│  ┌───────────────────────────┐  │
│  │   Blazor WebAssembly App  │  │
│  │  ┌────────┐ ┌──────────┐ │  │
│  │  │ Pages  │ │Components│ │  │
│  │  └───┬────┘ └────┬─────┘ │  │
│  │      │           │        │  │
│  │  ┌───▼───────────▼─────┐ │  │
│  │  │     Services         │ │  │
│  │  │ (HttpClient + JWT)   │ │  │
│  │  └───────────┬──────────┘ │  │
│  └──────────────┼────────────┘  │
│                 │ HTTPS          │
│  ┌──────────────▼──────────────┐│
│  │ sessionStorage (JWT Token)  ││
│  │ localStorage (Preferências) ││
│  └─────────────────────────────┘│
└─────────────────┬───────────────┘
                  │ HTTPS/REST
┌─────────────────▼───────────────┐
│   MoneyManager.Presentation     │
│       (ASP.NET Core API)        │
│           + MongoDB             │
└─────────────────────────────────┘
```

### Padrão de Comunicação

- Todas as chamadas à API passam pelo `HttpClient` configurado com `AuthorizationMessageHandler`.
- O handler injeta automaticamente o header `Authorization: Bearer {token}` em toda requisição.
- Respostas `401 Unauthorized` redirecionam o usuário para `/login`.
- Os Services encapsulam todas as chamadas HTTP e parsing de resposta.

---

## 2. Stack Tecnológico

| Tecnologia | Versão | Uso |
|---|---|---|
| Blazor WebAssembly | .NET 9.0 | Framework SPA |
| Bootstrap | 5.3.2 | Layout e componentes CSS (via CDN) |
| Font Awesome | 6.4.0 | Ícones (via CDN) |
| Chart.js | 4.4.1 | Gráficos interativos (via JS Interop) |
| Blazored.LocalStorage | 4.5.0 | Armazenamento persistente no browser |
| Inter | Google Fonts | Tipografia principal |
| Service Worker | Nativo | PWA offline + Push Notifications |

### Dependências NuGet

- `Microsoft.AspNetCore.Components.WebAssembly` 9.0.0
- `Microsoft.AspNetCore.Components.WebAssembly.Authentication` 9.0.0
- `Microsoft.AspNetCore.Components.Authorization` 9.0.0
- `Microsoft.Extensions.Http` 9.0.0
- `Blazored.LocalStorage` 4.5.0
- `System.IdentityModel.Tokens.Jwt` 8.3.1

---

## 3. Estrutura de Pastas

```
MoneyManager.Web/
├── wwwroot/
│   ├── css/
│   │   ├── app.css                    # Estilos globais da aplicação
│   │   ├── dashboard.css              # Estilos específicos do Dashboard
│   │   ├── credit-card-dashboard.css  # Estilos do dashboard de cartões
│   │   ├── invoice-details.css        # Estilos da página de fatura
│   │   ├── onboarding.css             # Estilos do Onboarding
│   │   └── profile.css                # Estilos do Perfil
│   ├── i18n/
│   │   └── pt-BR.json                 # Arquivo de localização (Português BR)
│   ├── js/
│   │   ├── chartInterop.js            # Funções JS para Chart.js (renderização de gráficos)
│   │   ├── theme-manager.js           # Troca de tema (light/dark/auto)
│   │   ├── navbar.js                  # Manipulação de collapse do navbar
│   │   └── badgeContrast.js           # Cálculo de contraste de cor para badges
│   ├── icon-192.png / icon-512.png    # Ícones do PWA
│   ├── manifest.json                  # Manifesto PWA
│   ├── service-worker.js              # Service Worker base
│   └── service-worker.published.js    # Service Worker para publicação
├── Pages/
│   ├── Index.razor                    # Dashboard principal
│   ├── Login.razor                    # Página de login
│   ├── Register.razor                 # Página de registro
│   ├── Onboarding.razor               # Boas-vindas pós-registro
│   ├── AccountDeleted.razor           # Confirmação de exclusão LGPD
│   ├── Accounts.razor                 # Gestão de contas
│   ├── Transactions.razor             # Gestão de transações
│   ├── Categories.razor               # Gestão de categorias
│   ├── Budgets.razor                  # Gestão de orçamentos
│   ├── Reports.razor                  # Relatórios financeiros
│   ├── RecurringTransactions.razor     # Transações recorrentes
│   ├── Profile.razor                  # Perfil do usuário
│   ├── Settings.razor                 # Configurações
│   ├── CreditCardDashboard.razor      # Dashboard de cartão de crédito
│   ├── InvoiceDetails.razor           # Detalhes de uma fatura
│   └── Test.razor                     # Página de teste (desenvolvimento)
├── Components/
│   ├── TransactionTable.razor         # Tabela de transações reutilizável
│   ├── TransactionFormModal.razor     # Modal de criação/edição de transação
│   ├── TransactionFilters.razor       # Filtros de transação
│   ├── InstallmentConfirmModal.razor  # Modal de confirmação de parcelamento
│   ├── InvoiceCard.razor              # Card de exibição de fatura
│   └── InvoiceStatusBadge.razor       # Badge de status de fatura
├── Shared/
│   ├── MainLayout.razor               # Layout principal (navbar + conteúdo)
│   ├── NavMenu.razor                  # Menu de navegação lateral/superior
│   ├── RedirectToLogin.razor          # Componente de redirecionamento
│   ├── BusyOverlay.razor              # Overlay de carregamento (spinner)
│   ├── ColorPicker.razor              # Seletor de cores
│   ├── MoneyInput.razor               # Input monetário formatado
│   └── LanguageSelector.razor         # Seletor de idioma
├── Services/
│   ├── AuthService.cs                 # Autenticação (login/registro/logout)
│   ├── CustomAuthenticationStateProvider.cs  # Provider de estado de autenticação
│   ├── AuthorizationMessageHandler.cs # Handler HTTP para injeção de JWT
│   ├── AccountService.cs              # CRUD de contas
│   ├── CategoryService.cs             # CRUD de categorias
│   ├── TransactionService.cs          # CRUD de transações
│   ├── RecurringTransactionService.cs # CRUD de transações recorrentes
│   ├── BudgetService.cs               # CRUD de orçamentos
│   ├── DashboardService.cs            # Dados do dashboard
│   ├── ReportService.cs               # Dados de relatórios
│   ├── CreditCardInvoiceService.cs    # Gestão de faturas de cartão
│   ├── UserProfileService.cs          # Perfil do usuário
│   ├── UserSettingsService.cs         # Configurações do usuário
│   ├── OnboardingService.cs           # Fluxo de onboarding
│   ├── AccountDeletionService.cs      # Exclusão de conta (LGPD)
│   ├── PushNotificationService.cs     # Notificações push
│   └── LocalizationService.cs         # Serviço de localização i18n
└── Program.cs                         # Ponto de entrada e configuração DI
```

---

## 4. Configuração e Inicialização (Program.cs)

### Resolução da URL da API

O `Program.cs` define a lógica para descobrir a URL base da API:

1. **Railway (produção):** Se a variável de ambiente `RAILWAY_PUBLIC_DOMAIN` estiver definida, o domínio público é usado com HTTPS.
2. **Variável `API_URL`:** Se definida explicitamente, é usada diretamente.
3. **`appsettings.json`:** Tenta ler `ApiSettings:BaseUrl` da configuração.
4. **Self-hosted:** Substitui a porta do host atual (porta 5001 → porta 5000) como fallback.

```
Prioridade: RAILWAY_PUBLIC_DOMAIN > API_URL > appsettings.json > porta derivada
```

### Registro de Serviços (DI)

Todos os 17 serviços são registrados como `Scoped`:

```csharp
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<CustomAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp =>
    sp.GetRequiredService<CustomAuthenticationStateProvider>());
builder.Services.AddScoped<AuthorizationMessageHandler>();
builder.Services.AddScoped<AccountService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<RecurringTransactionService>();
builder.Services.AddScoped<BudgetService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<CreditCardInvoiceService>();
builder.Services.AddScoped<UserProfileService>();
builder.Services.AddScoped<UserSettingsService>();
builder.Services.AddScoped<OnboardingService>();
builder.Services.AddScoped<AccountDeletionService>();
builder.Services.AddScoped<PushNotificationService>();
builder.Services.AddScoped<LocalizationService>();
```

### HttpClient com Autenticação

```csharp
builder.Services.AddHttpClient("MoneyManagerAPI", client =>
{
    client.BaseAddress = new Uri(apiUrl);
})
.AddHttpMessageHandler<AuthorizationMessageHandler>();
```

O `AuthorizationMessageHandler` intercepta toda requisição e adiciona o header JWT.

### Inicialização Assíncrona

Na inicialização, o `LocalizationService` é carregado para que as traduções estejam disponíveis antes do primeiro render:

```csharp
var localizationService = host.Services.GetRequiredService<LocalizationService>();
await localizationService.LoadTranslationsAsync("pt-BR");
```

---

## 5. Autenticação e Autorização

### 5.1 Páginas de Autenticação

#### Login (`/login`)

| Item | Detalhe |
|---|---|
| **Rota** | `/login` |
| **Atributo** | `[AllowAnonymous]` |
| **Campos** | Email, Senha |
| **API** | `POST /api/auth/login` com body `{ email, password }` |
| **Resposta** | `{ token }` (JWT) |
| **Armazenamento** | Token salvo em `sessionStorage["authToken"]` |
| **Redirecionamento** | Verifica `returnUrl` da query string; caso contrário, vai para `/` |
| **Erro** | Exibe mensagem de "Credenciais inválidas" em alert |

**Regras de Negócio:**
- O campo de senha é do tipo `password`.
- Formulário usa validação HTML5 (`required`).
- Após login, `CustomAuthenticationStateProvider.NotifyUserAuthentication(token)` é chamado para atualizar o estado de autenticação do Blazor.

#### Register (`/register`)

| Item | Detalhe |
|---|---|
| **Rota** | `/register` |
| **Atributo** | `[AllowAnonymous]` |
| **Campos** | Nome, Email, Senha, Confirmar Senha |
| **API** | `POST /api/auth/register` com body `{ name, email, password, confirmPassword }` |
| **Validação** | Senha e confirmação devem ser iguais (validação client-side) |
| **Pós-registro** | Faz login automático com as credenciais → redireciona para `/onboarding` |

**Regras de Negócio:**
- Após registro, o sistema executa automaticamente o fluxo de login.
- O usuário nunca vê a página de login após se registrar.

### 5.2 CustomAuthenticationStateProvider

Gerencia o estado de autenticação no Blazor:

- **GetAuthenticationStateAsync():** Lê o token de `sessionStorage`. Se válido, decodifica os claims (nome, email, userId, role) e cria um `ClaimsPrincipal` autenticado. Se inválido/expirado, retorna anônimo.
- **NotifyUserAuthentication(token):** Salva o token em `sessionStorage` e notifica o framework de autenticação.
- **NotifyUserLogout():** Remove o token do `sessionStorage` e notifica estado anônimo.
- **GetUserIdAsync():** Extrai o claim `nameid` (userId) do token.

**Token JWT:**
- Armazenado em `sessionStorage` (não sobrevive a fechamento de aba).
- Tempo de vida padrão: **10 minutos** (configurado na API).
- Claims contidos: `unique_name`, `email`, `nameid`, `role`, `exp`, `iss`, `aud`.

### 5.3 AuthorizationMessageHandler

Handler HTTP que:
1. Lê o token de `sessionStorage` via JS Interop.
2. Adiciona o header `Authorization: Bearer {token}` a toda requisição.
3. Se a resposta for `401 Unauthorized`:
   - Limpa o token do `sessionStorage`.
   - Notifica `CustomAuthenticationStateProvider` para marcar como deslogado.
   - Redireciona para `/login`.

### 5.4 Fluxo Completo de Autenticação

```
Registro → POST /api/auth/register
         → POST /api/auth/login (auto)
         → Token salvo em sessionStorage
         → NotifyUserAuthentication()
         → Redirecionamento para /onboarding

Login    → POST /api/auth/login
         → Token salvo em sessionStorage
         → NotifyUserAuthentication()
         → Redirecionamento para / (ou returnUrl)

Logout   → Remove token de sessionStorage
         → NotifyUserLogout()
         → Redirecionamento para /login

Expiração→ Qualquer chamada com 401
         → AuthorizationMessageHandler intercepta
         → Limpa token + redireciona para /login
```

---

## 6. Componentes Compartilhados (Shared)

### 6.1 MainLayout (`MainLayout.razor`)

Layout principal da aplicação, usado por todas as páginas autenticadas.

**Estrutura:**
- **Navbar superior:** Logo "MoneyManager", links de navegação (Dashboard, Contas, Transações, Categorias, Orçamentos, Relatórios, Recorrentes).
- **Dropdown do usuário:** Foto de perfil (ou ícone padrão), links para Perfil, Configurações e Botão Sair.
- **Corpo:** Renderiza o `@Body` da página, envolvido pelo `BusyOverlay`.
- O navbar é responsivo com collapse para telas menores.

**API Calls:**
- `GET /api/settings` — Carrega configurações do usuário na inicialização para aplicar tema.

**Comportamento:**
- Ao inicializar, carrega o tema (light/dark) via JS Interop (`themeManager.applyTheme`).
- Na navbar, mostra a foto de perfil do usuário se disponível.
- Logout chama `AuthService.LogoutAsync()`.

### 6.2 NavMenu (`NavMenu.razor`)

Menu de navegação com links para todas as seções:

| Link | Rota | Ícone |
|---|---|---|
| Dashboard | `/` | `fa-tachometer-alt` |
| Contas | `/accounts` | `fa-wallet` |
| Transações | `/transactions` | `fa-exchange-alt` |
| Categorias | `/categories` | `fa-tags` |
| Orçamentos | `/budgets` | `fa-chart-pie` |
| Relatórios | `/reports` | `fa-chart-bar` |
| Recorrentes | `/recurring-transactions` | `fa-redo` |
| Cartões | `/credit-cards/{id}` | `fa-credit-card` |

### 6.3 BusyOverlay (`BusyOverlay.razor`)

Overlay modal de carregamento com spinner.

**Parâmetros:**
- `Visible` (`bool`): Controla a exibição.
- `Text` (`string`): Texto exibido abaixo do spinner (padrão: "Carregando...").

**Uso:** Ativado durante chamadas à API para bloquear interação do usuário e indicar processamento.

### 6.4 ColorPicker (`ColorPicker.razor`)

Seletor de cores com input nativo + paleta de cores predefinidas.

**Parâmetros:**
- `Value` (`string`): Cor selecionada (hex).
- `ValueChanged` (`EventCallback<string>`): Evento de mudança.

**Cores predefinidas:** 16 cores populares (vermelho, rosa, roxo, azul, azul claro, ciano, teal, verde, verde claro, lima, amarelo, âmbar, laranja, laranja escuro, marrom, cinza azulado).

**Uso:** Categorias (cor da categoria), Configurações (cor de destaque da interface).

### 6.5 MoneyInput (`MoneyInput.razor`)

Input monetário com formatação automática.

**Parâmetros:**
- `Value` (`decimal`): Valor numérico.
- `ValueChanged` (`EventCallback<decimal>`): Evento de mudança.
- `CssClass` (`string`): Classe CSS adicional.

**Comportamento:**
- **Exibição (blur):** Mostra o valor formatado como moeda (ex: `R$ 1.500,00`) usando `CultureInfo.CurrentCulture`.
- **Edição (focus):** Mostra o valor "cru" sem formatação (ex: `1500`).
- **Parsing:** Aceita tanto `,` quanto `.` como separador decimal. Detecta o último separador como decimal.
- **Fallback:** Se o parsing falhar, retorna `0m`.

**Regra de parsing:**
1. Remove símbolo monetário e espaços.
2. Filtra apenas dígitos, `-`, `.` e `,`.
3. Identifica o último separador (`.` ou `,`) como decimal.
4. Converte para `decimal` via `InvariantCulture`.

### 6.6 LanguageSelector (`LanguageSelector.razor`)

Dropdown de seleção de idioma.

**Idiomas disponíveis:**
- 🇧🇷 Português (pt-BR)
- 🇺🇸 English (en-US)
- 🇪🇸 Español (es-ES)
- 🇫🇷 Français (fr-FR)

**Estado atual:** Apenas `pt-BR` está implementado. Os demais idiomas estão listados mas sem arquivos de tradução correspondentes.

### 6.7 RedirectToLogin (`RedirectToLogin.razor`)

Componente simples que redireciona para `/login` preservando a URL atual como `returnUrl`. Usado pelo `<NotAuthorized>` no `App.razor` para forçar autenticação.

---

## 7. Componentes Reutilizáveis

### 7.1 TransactionTable (`TransactionTable.razor`)

Tabela de transações reutilizada em múltiplas páginas.

**Parâmetros:**
- `Transactions` (`List<TransactionResponseDto>`): Lista de transações a exibir.
- `OnEdit` (`EventCallback<TransactionResponseDto>`): Callback para edição.
- `OnDelete` (`EventCallback<string>`): Callback para exclusão.
- `ShowActions` (`bool`): Exibir botões de ação (padrão: `true`).

**Colunas exibidas:**
| Coluna | Descrição |
|---|---|
| Data | `dd/MM/yyyy` |
| Descrição | Texto da transação |
| Categoria | Nome da categoria |
| Conta | Nome da conta |
| Tipo | Badge colorido (Receita=verde, Despesa=vermelha, Investimento=azul) |
| Valor | Formatado como moeda (R$) com cor condicional |
| Ações | Botões Editar e Excluir (condicional) |

**Regra de cor do valor:**
- **Receita (Income):** `text-success` (verde)
- **Despesa (Expense):** `text-danger` (vermelho)
- **Investimento (Investment):** `text-primary` (azul)

### 7.2 TransactionFormModal (`TransactionFormModal.razor`)

Modal Bootstrap para criação/edição de transações.

**Parâmetros:**
- `Transaction` (`TransactionRequestDto`): Dados da transação.
- `Accounts` (`List<AccountResponseDto>`): Contas disponíveis.
- `Categories` (`List<CategoryResponseDto>`): Categorias disponíveis.
- `IsEdit` (`bool`): Modo edição.
- `OnSave` (`EventCallback<TransactionRequestDto>`): Callback ao salvar.
- `OnCancel` (`EventCallback`): Callback ao cancelar.

**Campos do formulário:**

| Campo | Tipo | Obrigatório | Observação |
|---|---|---|---|
| Descrição | text | Sim | Texto livre |
| Valor | MoneyInput | Sim | Decimal formatado |
| Data | date | Sim | Default: hoje |
| Tipo | select | Sim | Income/Expense/Investment |
| Conta | select | Sim | Lista de contas do usuário |
| Categoria | select | Sim | Filtrada por tipo (Income/Expense) |
| Observações | textarea | Não | Texto livre |

**Regra de filtragem de categorias:**
- Se tipo = `Income` → mostra categorias do tipo `Income`.
- Se tipo = `Expense` → mostra categorias do tipo `Expense`.
- Se tipo = `Investment` → mostra todas as categorias.

**Funcionalidade de parcelamento:**
- Checkbox "Compra parcelada" (apenas para `Expense`).
- Ao ativar, exibe campo "Número de parcelas" (mínimo 2).
- Checkbox adicional: "Primeira parcela na fatura atual" (apenas se a conta é cartão de crédito).

### 7.3 TransactionFilters (`TransactionFilters.razor`)

Barra de filtros para a listagem de transações.

**Filtros disponíveis:**

| Filtro | Tipo | Opções |
|---|---|---|
| Ordenação | select | Data (mais recente), Data (mais antigo), Maior valor, Menor valor |
| Período | select | Mês atual, Personalizado |
| Tipo | select | Todos, Receitas, Despesas, Investimentos |
| Conta | select | Todas, lista de contas do usuário |
| Data Início | date | Somente quando período = Personalizado |
| Data Fim | date | Somente quando período = Personalizado |

**Eventos:**
- `OnFiltersChanged` (`EventCallback<TransactionFilterModel>`): Disparado ao alterar qualquer filtro.

### 7.4 InstallmentConfirmModal (`InstallmentConfirmModal.razor`)

Modal de confirmação antes de criar compra parcelada.

**Informações exibidas:**
- Valor total
- Número de parcelas
- Valor de cada parcela
- Conta selecionada
- Se a primeira parcela vai para a fatura atual

**Ações:**
- Confirmar → dispara o fluxo de criação de parcelamento.
- Cancelar → fecha o modal.

### 7.5 InvoiceCard (`InvoiceCard.razor`)

Card de exibição de fatura de cartão de crédito.

**Parâmetros:**
- `Invoice` (`CreditCardInvoiceResponseDto`): Dados da fatura.
- `ShowDetailsButton` (`bool`): Exibir botão "Ver Detalhes" (padrão: `true`).
- `ShowPayButton` (`bool`): Exibir botão "Pagar" (padrão: `true`).
- `OnViewDetailsClicked` (`EventCallback<string>`): Callback para detalhes.
- `OnPayClicked` (`EventCallback<string>`): Callback para pagamento.

**Informações exibidas:**
- Mês de referência, período (início/fim), vencimento
- Valor total (vermelho), valor pago (verde), restante (vermelho)
- Status badge (via InvoiceStatusBadge)
- Dias até o vencimento / dias de atraso
- Quantidade de transações
- Botões de ação (Ver Detalhes / Pagar)

**Regra de overdue:**
- `IsOverdue = DueDate < DateTime.Today && Status != Paid`
- Se overdue, card recebe borda vermelha (`border-danger`).

### 7.6 InvoiceStatusBadge (`InvoiceStatusBadge.razor`)

Badge colorido indicando o status de uma fatura.

**Parâmetros:**
- `Status` (`InvoiceStatus`): Status da fatura.
- `IsOverdue` (`bool`): Se está vencida.
- `ShowIcon` (`bool`): Exibir ícone (padrão: `true`).
- `CustomClass` (`string?`): Classe CSS adicional.

**Mapeamento de cores:**

| Status | Classe CSS | Rótulo |
|---|---|---|
| Open | `bg-info` | ABERTA |
| Closed | `bg-warning text-dark` | FECHADA |
| Paid | `bg-success` | PAGA |
| PartiallyPaid | `bg-warning text-dark` | PARCIALMENTE PAGA |
| Overdue | `bg-danger` | VENCIDA |
| (IsOverdue=true) | `bg-danger` | VENCIDA |

---

## 8. Fase 1 — Onboarding (Boas-vindas)

### Página: Onboarding (`/onboarding`)

A página mais simples do sistema. Exibida após o registro de um novo usuário.

**Rota:** `/onboarding`
**Autorização:** Requer autenticação.

#### Layout

Página com fundo gradiente (roxo/azul), card central com:
- Ícone de foguete
- Título: "Bem-vindo ao MoneyManager!"
- Subtítulo: "Sua conta foi criada com sucesso. Vamos começar a organizar suas finanças?"
- Três opções:
  1. **Criar primeira conta** → navega para `/accounts`
  2. **Configurar categorias** → navega para `/categories`
  3. **Ir para o Dashboard** → navega para `/`

#### Chamadas à API

| Endpoint | Método | Quando | Descrição |
|---|---|---|---|
| `POST /api/onboarding/complete` | POST | Ao inicializar | Marca o onboarding como completo |

#### Regras de Negócio

- O `OnboardingService.CompleteOnboardingAsync()` é chamado no `OnInitializedAsync`.
- Mesmo que o usuário navegue embora sem clicar em nada, o onboarding já foi marcado como completo.
- Não há bloqueio — é apenas informativo.

---

## 9. Fase 2 — Categorias

### Página: Categories (`/categories`)

Gestão de categorias de receita e despesa.

**Rota:** `/categories`
**Autorização:** Requer autenticação.

#### Layout

- **Título** com botão "Nova Categoria".
- **Grid de cards** com todas as categorias do usuário.
- **Modal de criação/edição** (Bootstrap modal inline).

#### Modelo de Dados

```
CategoryRequestDto:
  - Name: string (obrigatório)
  - Type: CategoryType (Income | Expense)
  - Color: string (hex color, ex: "#FF5733")
```

```
CategoryResponseDto:
  - Id: string
  - Name: string
  - Type: CategoryType
  - Color: string
```

#### Chamadas à API

| Endpoint | Método | Quando | Descrição |
|---|---|---|---|
| `GET /api/categories` | GET | OnInitializedAsync | Carrega todas as categorias |
| `POST /api/categories` | POST | Ao criar | Cria nova categoria |
| `PUT /api/categories/{id}` | PUT | Ao editar | Atualiza categoria existente |
| `DELETE /api/categories/{id}` | DELETE | Ao excluir | Remove categoria |

#### Fluxo de Dados

```
Inicialização:
  GET /api/categories → Lista de CategoryResponseDto → Renderiza cards

Criação:
  Preencher formulário → POST /api/categories → Recarregar lista

Edição:
  Clicar em "Editar" → Popular formulário → PUT /api/categories/{id} → Recarregar lista

Exclusão:
  Clicar em "Excluir" → Confirmação JS (confirm()) → DELETE /api/categories/{id} → Recarregar lista
```

#### Campos do Formulário

| Campo | Tipo | Obrigatório | Observação |
|---|---|---|---|
| Nome | text | Sim | — |
| Tipo | select | Sim | "Receita" (Income) ou "Despesa" (Expense) |
| Cor | ColorPicker | Sim | Componente compartilhado com paleta |

#### Card de Categoria

Cada card exibe:
- **Badge de cor**: Quadrado colorido com a cor da categoria.
- **Nome da categoria**.
- **Badge de tipo**: Verde para "Receita", Vermelho para "Despesa".
- **Botões**: Editar (azul), Excluir (vermelho).

#### Regras de Negócio

- A exclusão de categoria usa `confirm()` nativo do browser.
- Não há validação se a categoria está em uso por transações (a API pode retornar erro).
- A cor padrão para novas categorias é `#6366f1` (roxo-índigo).
- O tipo da categoria determina quais transações podem usá-la.

---

## 10. Fase 3 — Contas

### Página: Accounts (`/accounts`)

Gestão de contas bancárias, cartões de crédito e investimentos.

**Rota:** `/accounts`
**Autorização:** Requer autenticação.

#### Layout

- **Cabeçalho** com botão "Nova Conta" e controle de ordenação.
- **Grid de cards** com todas as contas, organizados em 3 colunas.
- **Modal de criação/edição** (Bootstrap modal inline).
- **Modal de pagamento de fatura** (para contas de cartão de crédito).

#### Modelo de Dados

```
AccountRequestDto:
  - Name: string (obrigatório)
  - Type: AccountType (Checking | Savings | Cash | CreditCard | Investment)
  - Balance: decimal (saldo inicial — editável apenas na criação)
  - Currency: string (BRL, USD, EUR, GBP, JPY, ARS, CLP, COP, MXN, PEN)
  - Color: string (hex)
  - InvoiceClosingDay: int? (1–28, apenas para CreditCard)
  - InvoiceDueDayOffset: int? (1–30, apenas para CreditCard)
  - CreditLimit: decimal? (apenas para CreditCard)
```

```
AccountResponseDto:
  - Id, Name, Type, Balance, Currency, Color
  - InvoiceClosingDay, InvoiceDueDayOffset, CreditLimit
  - CreatedAt, UpdatedAt
```

#### Chamadas à API

| Endpoint | Método | Quando | Descrição |
|---|---|---|---|
| `GET /api/accounts` | GET | OnInitializedAsync | Carrega todas as contas |
| `POST /api/accounts` | POST | Ao criar | Cria nova conta |
| `PUT /api/accounts/{id}` | PUT | Ao editar | Atualiza conta |
| `DELETE /api/accounts/{id}` | DELETE | Ao excluir | Remove conta |
| `GET /api/credit-card-invoices/pending` | GET | Ao abrir modal de pagamento | Carrega faturas pendentes |
| `POST /api/credit-card-invoices/pay` | POST | Ao pagar fatura integral | Paga fatura completa |
| `POST /api/credit-card-invoices/pay-partial` | POST | Ao pagar fatura parcial | Paga parcialmente |

#### Tipos de Conta e Ícones

| Tipo | Ícone | Label |
|---|---|---|
| Checking | `fa-university` | Conta Corrente |
| Savings | `fa-piggy-bank` | Poupança |
| Cash | `fa-money-bill-wave` | Dinheiro |
| CreditCard | `fa-credit-card` | Cartão de Crédito |
| Investment | `fa-chart-line` | Investimento |

#### Campos Condicionais (Cartão de Crédito)

Quando `Type == CreditCard`, são exibidos campos adicionais:

| Campo | Tipo | Obrigatório | Descrição |
|---|---|---|---|
| Dia do Fechamento | number (1–28) | Sim | Dia do mês que a fatura fecha |
| Offset do Vencimento | number (1–30) | Sim | Dias após o fechamento para vencimento |
| Limite de Crédito | MoneyInput | Sim | Valor do limite do cartão |

#### Sistema de Ordenação

O usuário pode ordenar as contas por:
- **Tipo** (padrão): agrupa por tipo de conta.
- **Nome**: ordem alfabética.
- **Saldo**: maior para menor.

**Persistência:** A preferência de ordenação é salva em `localStorage["accountSortPreference"]` e restaurada na próxima visita.

#### Moedas Suportadas

| Código | Símbolo | País |
|---|---|---|
| BRL | R$ | Brasil |
| USD | $ | EUA |
| EUR | € | Europa |
| GBP | £ | Reino Unido |
| JPY | ¥ | Japão |
| ARS | ARS$ | Argentina |
| CLP | CLP$ | Chile |
| COP | COP$ | Colômbia |
| MXN | MX$ | México |
| PEN | S/ | Peru |

#### Card de Conta

Cada card exibe:
- **Ícone e tipo** no cabeçalho.
- **Nome da conta**.
- **Saldo** formatado com símbolo da moeda.
- **Para cartões de crédito:**
  - Limite de crédito.
  - Progresso de uso (barra de progresso).
  - Cálculo: `usado = |saldo|`, `disponível = limite - usado`, `% = (usado / limite) * 100`.
  - Cores: Verde ≤ 60%, Amarelo ≤ 80%, Vermelho > 80%.
- **Botões:** Editar, Excluir.
- **Para cartões de crédito:** Botão adicional "Pagar Fatura" e link "Ver Dashboard do Cartão".

#### Modal de Pagamento de Fatura

Permite pagar faturas diretamente da página de contas.

**Fluxo:**
1. Ao clicar em "Pagar Fatura", carrega faturas pendentes via `GET /api/credit-card-invoices/pending`.
2. Filtra faturas do cartão selecionado.
3. Exibe dropdown com faturas disponíveis.
4. Usuário seleciona a fatura e a conta de débito (conta de onde sai o dinheiro; filtra cartões de crédito da lista).
5. Campos: Data do pagamento, Valor (preenchido com o restante).
6. Se valor == total restante → `POST /api/credit-card-invoices/pay` (pagamento integral).
7. Se valor < total restante → `POST /api/credit-card-invoices/pay-partial` (pagamento parcial).

#### Regras de Negócio

1. **Saldo inicial bloqueado:** O campo "Saldo Inicial" (`Balance`) só é editável durante a criação. Na edição, ele é ocultado (o backend mantém o saldo baseado em transações).
2. **Campos de cartão condicionais:** Os campos de cartão de crédito (dia de fechamento, offset, limite) só aparecem quando `Type == CreditCard`.
3. **Exclusão com confirmação:** Usa `confirm()` nativo antes de excluir.
4. **Cor padrão:** `#6366f1` para novas contas.
5. **Moeda padrão:** `BRL`.
6. **Conta de débito na fatura:** Na lista de contas para pagamento de fatura, cartões de crédito são excluídos (não faz sentido pagar fatura de cartão com outro cartão).

---

## 11. Fase 4 — Transações

### Página: Transactions (`/transactions`)

Gestão completa de transações financeiras com paginação, filtros e suporte a parcelamento.

**Rota:** `/transactions`
**Autorização:** Requer autenticação.

#### Layout

- **Cabeçalho** com botão "Nova Transação".
- **Barra de filtros** (componente `TransactionFilters`).
- **Tabela de transações** (componente `TransactionTable`).
- **Paginação** inferior.
- **Modal de criação/edição** (componente `TransactionFormModal`).
- **Modal de confirmação de parcelamento** (componente `InstallmentConfirmModal`).

#### Modelo de Dados

```
TransactionRequestDto:
  - Description: string (obrigatório)
  - Amount: decimal (obrigatório)
  - Date: DateTime (obrigatório)
  - Type: TransactionType (Income | Expense | Investment)
  - AccountId: string (obrigatório)
  - CategoryId: string (obrigatório)
  - Notes: string? (opcional)
  - ClientRequestId: string (GUID, para idempotência)
```

```
TransactionResponseDto:
  - Id, Description, Amount, Date, Type
  - AccountId, AccountName
  - CategoryId, CategoryName
  - Notes, CreatedAt, UpdatedAt
```

#### Chamadas à API

| Endpoint | Método | Quando | Descrição |
|---|---|---|---|
| `GET /api/transactions?page=N&pageSize=50&sortBy=X&startDate=Y&endDate=Z&type=W&accountId=A` | GET | Inicialização e filtros | Carrega transações paginadas |
| `POST /api/transactions` | POST | Criar transação simples | Cria transação |
| `POST /api/transactions/installment-purchase` | POST | Criar parcelamento | Cria compra parcelada |
| `PUT /api/transactions/{id}` | PUT | Editar transação | Atualiza transação |
| `DELETE /api/transactions/{id}` | DELETE | Excluir transação | Remove transação |
| `GET /api/accounts` | GET | Inicialização | Carrega contas para o formulário |
| `GET /api/categories` | GET | Inicialização | Carrega categorias para o formulário |

#### Paginação

- **Tamanho da página:** 50 transações.
- **Controles:** Anterior / Próxima / Primeira / Última.
- **Informação exibida:** "Mostrando X–Y de Z transações".
- **Query params:** `page` e `pageSize` enviados ao backend.

#### Sistema de Filtros

| Filtro | Parâmetro API | Valores |
|---|---|---|
| Ordenação | `sortBy` | `date_desc` (padrão), `date_asc`, `amount_desc`, `amount_asc` |
| Período | `startDate`, `endDate` | Mês atual (padrão) ou datas personalizadas |
| Tipo | `type` | `all`, `income`, `expense`, `investment` |
| Conta | `accountId` | Todas (vazio) ou ID específico |

**Comportamento do filtro de período:**
- "Mês atual" (padrão): calcula automaticamente primeiro e último dia do mês corrente.
- "Personalizado": permite selecionar datas de início e fim.
- Ao mudar o filtro, a paginação reseta para a página 1.

#### Fluxo de Criação de Transação Simples

```
1. Clicar "Nova Transação"
2. Preencher formulário no modal
3. Clicar "Salvar"
4. Gerar ClientRequestId (GUID) para idempotência
5. POST /api/transactions com o payload
6. Fechar modal
7. Recarregar lista de transações
```

#### Fluxo de Compra Parcelada (Installment Purchase)

Este é um dos fluxos mais complexos do frontend:

```
1. Clicar "Nova Transação"
2. Selecionar Tipo = "Despesa"
3. Ativar checkbox "Compra parcelada"
4. Preencher:
   - Valor TOTAL da compra
   - Número de parcelas (≥ 2)
   - Se a conta é cartão: checkbox "Primeira parcela na fatura atual"
5. Clicar "Salvar"
6. Modal de confirmação mostra:
   - Valor total: R$ X
   - Parcelas: N vezes
   - Valor de cada parcela: R$ X/N (arredondado para 2 casas)
7. Confirmar
8. POST /api/transactions/installment-purchase com:
   {
     description, totalAmount, installmentCount,
     firstInstallmentInCurrentInvoice, date, type,
     accountId, categoryId, notes, clientRequestId
   }
9. Fechar modal
10. Recarregar lista
```

**Regras do parcelamento:**
- Valor de cada parcela = `totalAmount / installmentCount` (arredondado para 2 casas decimais).
- A API cria a primeira transação imediatamente.
- As parcelas restantes são criadas como uma **RecurringTransaction** com frequência mensal.
- O `ClientRequestId` previne duplicação se o usuário clicar 2 vezes.
- O campo `firstInstallmentInCurrentInvoice` controla:
  - `true`: 2ª parcela começa no mês seguinte.
  - `false`: 2ª parcela começa 2 meses à frente (para faturas de cartão já fechadas).

#### Regras de Negócio

1. **Idempotência:** Cada transação gera um `ClientRequestId` (GUID), evitando duplicatas por reenvio acidental.
2. **Moeda herdada:** A transação herda a moeda da conta selecionada (não é editável).
3. **Filtragem de categorias:** O select de categorias filtra dinamicamente pelo tipo da transação.
4. **Valor sempre positivo:** O frontend não manipula o sinal do valor — o backend interpreta baseado no tipo.
5. **Data padrão:** Hoje (`DateTime.Now`).
6. **Paginação reset:** Qualquer mudança de filtro reseta para a página 1 para evitar páginas vazias.

---

## 12. Fase 5 — Transações Recorrentes

### Página: RecurringTransactions (`/recurring-transactions`)

Gestão de transações automáticas recorrentes.

**Rota:** `/recurring-transactions`
**Autorização:** Requer autenticação.

#### Layout

- **Card resumo** no topo com totais de receitas/despesas/líquido recorrentes.
- **Botão "Nova Transação Recorrente"**.
- **Grid de cards** com todas as transações recorrentes.
- **Modal de criação/edição** (Bootstrap modal inline).

#### Modelo de Dados

```
RecurringTransactionRequestDto:
  - Description: string (obrigatório)
  - Amount: decimal (obrigatório)
  - Type: TransactionType (Income | Expense | Investment)
  - AccountId: string (obrigatório)
  - CategoryId: string (obrigatório)
  - Frequency: RecurrenceFrequency (obrigatório)
  - StartDate: DateTime (obrigatório)
  - EndDate: DateTime? (opcional)
  - IsActive: bool
  - Notes: string?
```

```
RecurringTransactionResponseDto:
  - Id, Description, Amount, Type
  - AccountId, AccountName
  - CategoryId, CategoryName
  - Frequency, StartDate, EndDate, IsActive
  - NextOccurrence, LastProcessedDate
  - Notes, CreatedAt, UpdatedAt
```

#### Chamadas à API

| Endpoint | Método | Quando | Descrição |
|---|---|---|---|
| `GET /api/recurringtransactions` | GET | OnInitializedAsync | Carrega todas as recorrentes |
| `POST /api/recurringtransactions` | POST | Criar | Cria nova recorrente |
| `PUT /api/recurringtransactions/{id}` | PUT | Editar | Atualiza recorrente |
| `DELETE /api/recurringtransactions/{id}` | DELETE | Excluir | Remove recorrente |
| `GET /api/accounts` | GET | OnInitializedAsync | Carrega contas |
| `GET /api/categories` | GET | OnInitializedAsync | Carrega categorias |

#### Frequências Disponíveis

| Valor | Label | Descrição |
|---|---|---|
| Daily | Diária | Todos os dias |
| Weekly | Semanal | Toda semana |
| Biweekly | Quinzenal | A cada 2 semanas |
| Monthly | Mensal | Todo mês |
| Quarterly | Trimestral | A cada 3 meses |
| Semiannual | Semestral | A cada 6 meses |
| Annual | Anual | Uma vez por ano |

#### Card Resumo

Exibido no topo, calculado no frontend a partir das transações ativas:

| Métrica | Cálculo |
|---|---|
| Total Receitas Recorrentes | Soma de `Amount` onde `Type == Income && IsActive` |
| Total Despesas Recorrentes | Soma de `Amount` onde `Type == Expense && IsActive` |
| Líquido Recorrente | Receitas - Despesas |

Cor do líquido: verde se positivo, vermelho se negativo.

#### Card Individual

Cada card exibe:
- Badge de tipo (Receita/Despesa/Investimento) com cor.
- Descrição e valor.
- Conta e categoria associadas.
- Frequência traduzida.
- Data de início e fim (se houver).
- Próxima ocorrência.
- Última data processada.
- Status: "Ativa" (verde) ou "Inativa" (vermelho/cinza).
- Botões: Editar e Excluir.

#### Campos do Formulário

| Campo | Tipo | Obrigatório | Observação |
|---|---|---|---|
| Descrição | text | Sim | — |
| Valor | MoneyInput | Sim | — |
| Tipo | select | Sim | Income / Expense / Investment |
| Conta | select | Sim | Lista de contas |
| Categoria | select | Sim | Filtrada por tipo |
| Frequência | select | Sim | 7 opções |
| Data de Início | date | Sim | Default: hoje |
| Data de Fim | date | Não | Se vazio, recorrência indefinida |
| Ativa | checkbox | — | Default: `true` |
| Observações | textarea | Não | — |

#### Regras de Negócio

1. **Processamento automático:** Transações recorrentes são processadas por um Worker Service (backend), não pelo frontend. O frontend apenas gerencia (CRUD) e visualiza o estado.
2. **Filtragem de categorias por tipo:** Mesmo comportamento da página de Transações.
3. **Exclusão com confirmação:** Usa `confirm()` nativo.
4. **Status ativo/inativo:** Transações inativas não são processadas pelo Worker, mas permanecem visíveis.
5. **Origem por parcelamento:** Transações recorrentes podem ser criadas automaticamente pelo fluxo de parcelamento (Fase 4).

---

## 13. Fase 6 — Orçamentos (Budgets)

### Página: Budgets (`/budgets`)

Gestão de orçamentos mensais por categoria.

**Rota:** `/budgets`
**Autorização:** Requer autenticação.

#### Layout

- **Seletor de mês** (input type month, formato `yyyy-MM`).
- **Botão "Novo Orçamento"** (se não houver orçamento no mês selecionado).
- **Botão "Copiar do Mês Anterior"** (se houver orçamento no mês anterior).
- **Grid de cards** com cada item do orçamento (por categoria).
- **Modal de criação/edição** (fluxo de 3 etapas).

#### Modelo de Dados

```
BudgetRequestDto:
  - Month: string (formato "yyyy-MM")
  - Items: List<BudgetItemDto>

BudgetItemDto:
  - CategoryId: string
  - LimitAmount: decimal

BudgetResponseDto:
  - Id, Month
  - Items: List<BudgetItemResponseDto>

BudgetItemResponseDto:
  - CategoryId, CategoryName, CategoryColor
  - LimitAmount, SpentAmount
```

#### Chamadas à API

| Endpoint | Método | Quando | Descrição |
|---|---|---|---|
| `GET /api/budgets/{month}` | GET | Ao selecionar mês | Carrega orçamento do mês |
| `POST /api/budgets` | POST | Ao criar | Cria novo orçamento |
| `PUT /api/budgets/{id}` | PUT | Ao editar | Atualiza orçamento existente |
| `GET /api/categories` | GET | OnInitializedAsync | Carrega categorias para seleção |

#### Fluxo de Criação de Orçamento (3 Etapas)

**Etapa 1 — Escolher Mês:**
- Input `type=month` com o mês selecionado.

**Etapa 2 — Selecionar Categorias:**
- Grid de categorias (apenas do tipo `Expense`) como "toggle cards".
- O usuário clica nas categorias que deseja incluir no orçamento.
- Cards selecionados recebem visual diferenciado (borda azul, sombra).

**Etapa 3 — Definir Limites:**
- Para cada categoria selecionada, exibe input `MoneyInput` com o limite.
- **Valor padrão:** `R$ 500,00`.
- O usuário ajusta cada limite individualmente.

**Salvamento:**
- Ao clicar "Salvar", envia `POST /api/budgets` ou `PUT /api/budgets/{id}` dependendo se já existe.
- Payload: `{ month: "yyyy-MM", items: [{ categoryId, limitAmount }...] }`.

#### Card de Item do Orçamento

Cada card exibe:
- Nome da categoria com badge de cor.
- Limite definido (ex: `R$ 2.000,00`).
- Valor gasto no mês (ex: `R$ 1.350,00`).
- **Barra de progresso** com porcentagem:
  - `% = (spent / limit) * 100`.
  - **Verde** (`bg-success`): ≤ 75%.
  - **Amarelo** (`bg-warning`): 75% < % ≤ 90%.
  - **Vermelho** (`bg-danger`): > 90%.
- Valor restante.

#### Copiar do Mês Anterior

**Fluxo:**
1. Sistema verifica se existe orçamento no mês anterior ao selecionado.
2. Se existir, exibe botão "Copiar do Mês Anterior".
3. Ao clicar, carrega `GET /api/budgets/{mesAnterior}`.
4. Copia os itens (categorias + limites) para o mês atual.
5. Exibe modal de edição pré-preenchido para ajustes.

#### Regras de Negócio

1. **Um orçamento por mês:** Não é possível ter dois orçamentos para o mesmo mês.
2. **Apenas categorias de despesa:** O orçamento é exclusivamente para categorias do tipo `Expense`.
3. **Limite padrão:** R$ 500,00 ao selecionar uma categoria (valor pode ser alterado).
4. **Gasto calculado no backend:** O campo `SpentAmount` de cada item vem da API, calculado a partir das transações reais do mês.
5. **Limiares visuais de alerta:**
   - ≤ 75%: situação confortável (verde).
   - 75%–90%: atenção (amarelo).
   - > 90%: perigo/estourado (vermelho).

---

## 14. Fase 7 — Dashboard

### Página: Index / Dashboard (`/`)

Visão consolidada de toda a saúde financeira do usuário.

**Rota:** `/` (página inicial)
**Autorização:** Requer autenticação.

#### Layout

- **4 cards de resumo** no topo.
- **4 gráficos** (Chart.js via JS Interop).
- **Tabela de transações recentes**.
- **Cards de limite de cartão de crédito**.

#### Chamadas à API

| Endpoint | Método | Quando | Descrição |
|---|---|---|---|
| `GET /api/accounts` | GET | OnInitializedAsync | Carrega todas as contas |
| `GET /api/transactions?page=1&pageSize=100&startDate=X&endDate=Y` | GET | OnInitializedAsync | Transações do mês atual |
| `GET /api/categories` | GET | OnInitializedAsync | Carrega categorias |
| `GET /api/budgets/{mesAtual}` | GET | OnInitializedAsync | Carrega orçamento do mês |

> **Nota:** O `DashboardService` agrega os dados dessas 4 chamadas no método `LoadDashboardData()`.

#### Cards de Resumo

| Card | Cálculo | Cor |
|---|---|---|
| Saldo Líquido | Soma de `Balance` de contas (excluindo cartões de crédito e investimentos) | Verde se > 0, Vermelho se < 0 |
| Total em Ativos | Soma de `Balance` de todas as contas (excluindo cartões de crédito) | Azul |
| Receitas do Mês | Soma de `Amount` de transações do mês com `Type == Income` | Verde |
| Despesas do Mês | Soma de `Amount` de transações do mês com `Type == Expense` | Vermelho |

Card adicional de uso do orçamento:
- `% = (total gasto / total limite) * 100` das categorias do orçamento do mês.
- Exibe "X% do orçamento utilizado".

#### Gráficos (Chart.js)

Todos os gráficos são renderizados via JS Interop usando o arquivo `chartInterop.js`:

**1. Gráfico de Pizza — Uso do Orçamento:**
- Cada fatia = uma categoria do orçamento.
- Valor de cada fatia = `SpentAmount`.
- Cores vindas das categorias.
- Se não houver orçamento, mostra mensagem "Nenhum orçamento definido".

**2. Gráfico de Pizza — Receitas vs Despesas:**
- 2 fatias: total de receitas (verde) vs total de despesas (vermelho).
- Baseado nas transações do mês atual.

**3. Gráfico de Barras — Contas Líquidas:**
- Uma barra por conta (excluindo cartões de crédito).
- Valor de cada barra = saldo da conta.
- Cores vindas das contas.

**4. Gráfico de Barras — Cartões de Crédito:**
- Uma barra por cartão de crédito.
- Valor = `|Balance|` (valor usado do limite).
- Cor = cor do cartão.

#### Renderização de Gráficos (JS Interop)

```javascript
// chartInterop.js expõe estas funções:
window.chartInterop = {
    createPieChart(canvasId, labels, data, colors, title)
    createBarChart(canvasId, labels, data, colors, title)
    createLineChart(canvasId, labels, data, title)
    destroyChart(canvasId)
}
```

**Fluxo:**
1. `OnAfterRenderAsync(firstRender)` é chamado após o render inicial.
2. Os gráficos são renderizados chamando `IJSRuntime.InvokeVoidAsync("chartInterop.createXChart", ...)`.
3. Antes de re-renderizar, cada gráfico é destruído com `destroyChart(canvasId)` para evitar memory leaks.

#### Transações Recentes

- Exibe as 10 transações mais recentes do mês.
- Usa o componente `TransactionTable` em modo somente-leitura (`ShowActions = false`).

#### Cards de Limite de Cartão de Crédito

Para cada conta do tipo `CreditCard`:
- Nome do cartão.
- Limite total.
- Valor usado (`|Balance|`).
- Valor disponível (`CreditLimit - |Balance|`).
- Barra de progresso: `% = (usado / limite) * 100`.
- Cores: Verde ≤ 60%, Amarelo ≤ 80%, Vermelho > 80%.
- Link "Ver Dashboard do Cartão" → `/credit-cards/{id}`.

#### Regras de Negócio

1. **Saldo líquido exclui cartões de crédito e investimentos:** Representa apenas dinheiro "disponível" (corrente + poupança + dinheiro).
2. **Total em ativos exclui cartões:** Cartões de crédito têm saldo negativo (dívida) e são tratados separadamente.
3. **Cálculos no frontend:** Todos os cálculos de soma, médias e percentuais são feitos no `DashboardService` (client-side), não na API.
4. **Mês atual:** O dashboard sempre mostra dados do mês corrente.

---

## 15. Fase 8 — Relatórios

### Página: Reports (`/reports`)

Análise financeira com gráficos e tabelas.

**Rota:** `/reports`
**Autorização:** Requer autenticação.

#### Layout

- **Seletor de período** com presets de tempo.
- **4 cards de resumo** (receitas, despesas, saldo líquido, taxa de poupança).
- **Gráfico de pizza** — despesas por categoria.
- **Gráfico de linha** — tendências mensais (últimos 6 meses).
- **Tabela** — detalhamento por categoria.

#### Chamadas à API

| Endpoint | Método | Quando | Descrição |
|---|---|---|---|
| `GET /api/transactions?page=1&pageSize=10000&startDate=X&endDate=Y` | GET | Ao selecionar período | Carrega TODAS as transações do período |
| `GET /api/categories` | GET | OnInitializedAsync | Carrega categorias |

> **ATENÇÃO:** O relatório carrega todas as transações (`pageSize=10000`) e faz todos os cálculos no frontend. Em um cenário com grande volume de dados, isso pode causar problemas de performance.

#### Presets de Período

| Preset | `startDate` | `endDate` |
|---|---|---|
| Mês Atual | 1º dia do mês atual | Último dia do mês atual |
| Mês Anterior | 1º dia do mês anterior | Último dia do mês anterior |
| Últimos 3 Meses | 3 meses atrás | Hoje |
| Últimos 6 Meses | 6 meses atrás | Hoje |
| Último Ano | 12 meses atrás | Hoje |
| Personalizado | Data selecionada | Data selecionada |

#### Cards de Resumo

| Card | Cálculo |
|---|---|
| Total Receitas | Soma de Amount onde Type == Income |
| Total Despesas | Soma de Amount onde Type == Expense |
| Saldo Líquido | Receitas − Despesas |
| Taxa de Poupança | `(Receitas − Despesas) / Receitas × 100%` (se Receitas > 0, senão 0%) |

#### Gráficos

**1. Pizza — Despesas por Categoria:**
- Agrupa transações do tipo `Expense` por `CategoryId`.
- Cada fatia = suma dos valores por categoria.
- Cores = cores das categorias.

**2. Linha — Tendências Mensais:**
- Eixo X: últimos 6 meses (formato `MMM/yyyy`).
- Duas linhas: receitas (verde) e despesas (vermelha).
- Valores agrupados por mês (`transação.Date.ToString("yyyy-MM")`).

#### Tabela de Detalhamento por Categoria

| Coluna | Descrição |
|---|---|
| Categoria | Nome + badge de cor |
| Total | Soma das despesas da categoria |
| % do Total | `(total da categoria / total geral de despesas) × 100%` |
| Barra | Barra de progresso proporcional ao % |

#### Regras de Negócio

1. **Cálculo 100% client-side:** Todas as agregações, agrupamentos e percentuais são calculados no frontend via `ReportService`.
2. **Taxa de poupança:** Fórmula padrão de finanças pessoais: `(income - expense) / income`. Se income = 0, a taxa é 0%.
3. **Tendências limitadas a 6 meses:** Mesmo que o período selecionado seja maior, o gráfico de linha mostra apenas os 6 meses mais recentes.
4. **Escalabilidade:** Com `pageSize=10000`, o sistema pode ter problemas com usuários que têm grande volume de transações.

---

## 16. Fase 9 — Cartões de Crédito e Faturas

### 16.1 Página: CreditCardDashboard (`/credit-cards/{AccountId}`)

Dashboard dedicado a um cartão de crédito específico.

**Rota:** `/credit-cards/{AccountId}`
**Autorização:** Requer autenticação.

#### Layout

- **Cabeçalho** com nome do cartão e link "Voltar para Contas".
- **Card de Limite de Crédito** com barra de progresso.
- **Card da Fatura Atual** (aberta).
- **Card da Próxima Fatura a Vencer** (com alerta de atraso).
- **Tabela de Histórico de Faturas**.
- **Modal de Pagamento de Fatura**.

#### Chamadas à API

| Endpoint | Método | Quando | Descrição |
|---|---|---|---|
| `GET /api/accounts` | GET | OnInitializedAsync | Carrega dados da conta |
| `GET /api/credit-card-invoices/accounts/{id}/open` | GET | OnInitializedAsync | Fatura aberta atual |
| `GET /api/credit-card-invoices/overdue` | GET | OnInitializedAsync | Faturas vencidas |
| `POST /api/credit-card-invoices/accounts/{id}/history` | POST | OnInitializedAsync | Histórico de faturas |
| `POST /api/credit-card-invoices/pay` | POST | Ao pagar fatura integral | Paga fatura |
| `POST /api/credit-card-invoices/pay-partial` | POST | Ao pagar parcialmente | Paga parcialmente |

#### Fatura Atual (Open Invoice)

Exibe a fatura aberta (status `Open`):
- Mês de referência.
- Período (início → fim).
- Valor total.
- Dias até o fechamento.

Se não houver fatura aberta, exibe mensagem "Nenhuma fatura aberta".

#### Próxima Fatura a Vencer

Busca a fatura com status `Closed` ou `PartiallyPaid` mais próxima do vencimento:
- Valor total e restante.
- Data de vencimento.
- **Detecção de atraso:** Se `DueDate < Today`, exibe alerta vermelho "FATURA VENCIDA!".
- Botão "Pagar Fatura".

#### Limite de Crédito

| Informação | Cálculo |
|---|---|
| Limite Total | `Account.CreditLimit` |
| Utilizado | `|Account.Balance|` |
| Disponível | `CreditLimit - |Balance|` |
| % Utilização | `(Utilizado / Limite) × 100` |

Barra de progresso:
- Verde: ≤ 60%
- Amarelo: ≤ 80%
- Vermelho: > 80%

#### Histórico de Faturas

Tabela com todas as faturas do cartão:

| Coluna | Descrição |
|---|---|
| Mês | Mês de referência |
| Período | Data início → Data fim |
| Valor Total | Total da fatura |
| Pago | Valor já pago |
| Restante | Valor pendente |
| Status | Badge colorido (InvoiceStatusBadge) |
| Ações | "Ver Detalhes" → `/invoices/{id}` |

#### Modal de Pagamento

Mesma lógica da página Accounts:
1. Selecionar conta de débito (exclui cartões).
2. Data do pagamento.
3. Valor (pré-preenchido com restante).
4. Se valor == restante → pagamento integral (`/pay`).
5. Se valor < restante → pagamento parcial (`/pay-partial`).

### 16.2 Página: InvoiceDetails (`/invoices/{InvoiceId}`)

Detalhes completos de uma fatura específica.

**Rota:** `/invoices/{InvoiceId}`
**Autorização:** Requer autenticação.

#### Chamadas à API

| Endpoint | Método | Quando | Descrição |
|---|---|---|---|
| `GET /api/credit-card-invoices/{id}/summary` | GET | OnInitializedAsync | Resumo da fatura |
| `GET /api/credit-card-invoices/{id}/transactions` | GET | OnInitializedAsync | Transações da fatura |
| `GET /api/categories` | GET | OnInitializedAsync | Categorias para agrupamento |
| `GET /api/accounts` | GET | OnInitializedAsync | Contas para modal de pagamento |
| `POST /api/credit-card-invoices/pay` | POST | Ao pagar | Paga fatura |
| `POST /api/credit-card-invoices/pay-partial` | POST | Ao pagar parcialmente | Paga parcialmente |

#### Layout da Fatura

**Cards de Resumo:**
| Card | Valor |
|---|---|
| Total da Fatura | `Invoice.TotalAmount` |
| Valor Pago | `Invoice.PaidAmount` |
| Restante | `Invoice.RemainingAmount` |
| Vencimento | `Invoice.DueDate` (com alerta se vencida) |

**Informações do Período:**
- Mês de referência.
- Período: data início → data fim.
- Status badge.

**Detalhamento por Categoria:**
- Para cada categoria que tem transações na fatura:
  - Nome da categoria com badge de cor.
  - Total gasto na categoria.
  - Barra de progresso (% do total da fatura).
  - Quantidade de transações.

**Lista de Transações:**
- Tabela com todas as transações da fatura.
- Colunas: Data, Descrição, Categoria, Valor.

**Modal de Pagamento:**
- Mesmo componente/lógica das demais páginas de pagamento.

#### Regras de Negócio (Faturas)

1. **Status da fatura:**
   - `Open`: Fatura ainda recebendo transações.
   - `Closed`: Fatura fechada, aguardando pagamento.
   - `Paid`: Totalmente paga.
   - `PartiallyPaid`: Parte do valor foi paga.
   - `Overdue`: Vencida sem pagamento total.

2. **Overdue override:** Mesmo que o status na API seja `Closed` ou `PartiallyPaid`, se `DueDate < Today`, o frontend trata como "VENCIDA" visualmente.

3. **Pagamento integral vs parcial:**
   - Se o valor pago == restante → API endpoint de pagamento integral.
   - Se o valor pago < restante → API endpoint de pagamento parcial.
   - O frontend não permite pagar mais do que o restante.

4. **Conta de débito:** Cartões de crédito são filtrados da lista de contas de débito.

---

## 17. Fase 10 — Perfil do Usuário

### Página: Profile (`/profile`)

Gerenciamento de informações pessoais do usuário.

**Rota:** `/profile`
**Autorização:** Requer autenticação.

#### Layout

- **Card de informações pessoais** com botão "Editar".
- **Card de alterar senha**.
- **Card de alterar e-mail** (modal).
- **Seção de exclusão de conta** (LGPD — descrita na Fase 12).

#### Modelo de Dados

```
UserProfileDto:
  - Name: string
  - FullName: string?
  - Email: string (somente leitura no card)
  - Phone: string?
  - ProfilePicture: string? (URL da imagem)
```

#### Chamadas à API

| Endpoint | Método | Quando | Descrição |
|---|---|---|---|
| `GET /api/profile` | GET | OnInitializedAsync | Carrega perfil |
| `PUT /api/profile` | PUT | Ao salvar edição | Atualiza informações |
| `POST /api/profile/change-password` | POST | Ao alterar senha | Altera senha |
| `POST /api/profile/update-email` | POST | Ao alterar e-mail | Altera e-mail |

#### Edição de Informações Pessoais

**Campos editáveis:**
| Campo | Tipo | Obrigatório |
|---|---|---|
| Nome | text | Sim |
| Nome Completo | text | Não |
| Telefone | text | Não |
| URL da Foto | text | Não |

**Fluxo:**
1. Clica "Editar" → campos tornam-se editáveis.
2. Preenche/altera → clica "Salvar".
3. `PUT /api/profile` com payload.
4. Atualiza estado local.
5. Botão "Cancelar" descarta alterações e volta ao modo visualização.

#### Alterar Senha

| Campo | Tipo | Obrigatório |
|---|---|---|
| Senha Atual | password | Sim |
| Nova Senha | password | Sim |
| Confirmar Nova Senha | password | Sim |

**Validação client-side:**
- Nova senha e confirmação devem ser iguais.
- Se não iguais: exibe mensagem de erro, não envia à API.

**API:**
```
POST /api/profile/change-password
Body: { currentPassword, newPassword, confirmNewPassword }
```

#### Alterar E-mail

Aberto via modal Bootstrap.

| Campo | Tipo | Obrigatório |
|---|---|---|
| Novo E-mail | email | Sim |
| Senha (confirmação) | password | Sim |

**API:**
```
POST /api/profile/update-email
Body: { newEmail, password }
```

**Regras:**
- A senha é exigida para confirmar a alteração (segurança).
- Após sucesso, o e-mail visível no card é atualizado.

---

## 18. Fase 11 — Configurações

### Página: Settings (`/settings`)

Preferências de uso do aplicativo.

**Rota:** `/settings`
**Autorização:** Requer autenticação.

#### Layout

- **Card de preferências financeiras**.
- **Card de notificações push**.
- **Card de aparência**.

#### Modelo de Dados

```
UserSettingsDto:
  - Currency: string (código da moeda, ex: "BRL")
  - DateFormat: string (ex: "dd/MM/yyyy")
  - MonthClosingDay: int (1–28)
  - DefaultBudget: decimal?
  - PushNotificationsEnabled: bool
  - NotifyRecurringProcessed: bool
  - NotifyDailyReminder: bool
  - Theme: string ("light" | "dark" | "auto")
  - PrimaryColor: string (hex)
```

#### Chamadas à API

| Endpoint | Método | Quando | Descrição |
|---|---|---|---|
| `GET /api/settings` | GET | OnInitializedAsync | Carrega configurações |
| `PUT /api/settings` | PUT | Ao salvar | Salva configurações |
| `GET /api/push/public-key` | GET | Ao habilitar push | Obtém chave VAPID pública |
| `GET /api/push/status` | GET | Ao inicializar push | Verifica status do push |

#### Card de Preferências Financeiras

| Campo | Tipo | Opções |
|---|---|---|
| Moeda | select | BRL, USD, EUR, GBP, JPY |
| Formato de Data | select | dd/MM/yyyy, MM/dd/yyyy, yyyy-MM-dd |
| Dia do Fechamento do Mês | number | 1–28 |
| Orçamento Padrão | MoneyInput | Valor default para novos itens de orçamento |

#### Card de Notificações Push

| Campo | Tipo | Descrição |
|---|---|---|
| Habilitar Notificações | switch | Liga/desliga push notifications |
| Notificar Recorrentes Processadas | switch | Notifica quando worker processar recorrentes |
| Lembrete Diário | switch | Notificação diária de resumo |
| Testar Notificação | button | Envia push de teste |

**Fluxo de habilitação de push:**
1. Clica no switch → `PushNotificationService` solicita permissão ao browser.
2. Se permitido: registra a subscription no Service Worker.
3. Envia a subscription para a API (`POST /api/push/subscribe`).
4. Ativa o switch visualmente.

#### Card de Aparência

| Campo | Tipo | Opções |
|---|---|---|
| Tema | select | Claro (light), Escuro (dark), Automático (auto) |
| Cor de Destaque | ColorPicker | Cor hexadecimal |

**Aplicação do tema:**
- Ao mudar o tema, chama `IJSRuntime.InvokeVoidAsync("themeManager.setTheme", theme)`.
- O `theme-manager.js` aplica `data-theme="dark"` no `<html>` element.
- CSS usa variáveis com `[data-theme="dark"]` para alternar cores.
- A preferência é salva tanto na API (para sincronização entre dispositivos) quanto no `localStorage` (para aplicação imediata antes da API responder).

#### Regras de Negócio

1. **Tema "auto":** Segue a preferência do sistema operacional (`prefers-color-scheme: dark`).
2. **Dia de fechamento limitado a 28:** Para evitar problemas com meses de 28/29/30/31 dias.
3. **Push notifications:** Dependem do suporte do browser e permissão do usuário. Se o browser não suportar, os switches ficam desabilitados.

---

## 19. Fase 12 — Exclusão de Conta (LGPD)

### Localização: Profile (`/profile`) — Seção inferior

Conformidade com a Lei Geral de Proteção de Dados (LGPD).

#### Chamadas à API

| Endpoint | Método | Quando | Descrição |
|---|---|---|---|
| `GET /api/accountdeletion/data-count` | GET | Ao abrir seção | Contagem de dados do usuário |
| `POST /api/accountdeletion/delete-account` | POST | Ao confirmar exclusão | Exclui a conta |

#### Fluxo de Exclusão

```
1. Usuário clica para expandir seção de exclusão
2. GET /api/accountdeletion/data-count retorna:
   {
     totalAccounts: N,
     totalTransactions: N,
     totalCategories: N,
     totalBudgets: N,
     totalRecurringTransactions: N
   }
3. Exibe alertas de aviso:
   - "Esta ação é IRREVERSÍVEL"
   - "Todos os seus dados serão permanentemente excluídos"
   - Contagem detalhada dos dados que serão removidos
4. Três barreiras de confirmação:
   a. Digitar a senha da conta
   b. Digitar exatamente "EXCLUIR MINHA CONTA" (texto literal)
   c. Marcar checkbox de "Compreendo que esta ação é irreversível"
5. Botão "Excluir Minha Conta Permanentemente" fica habilitado apenas quando:
   - Senha preenchida
   - Texto de confirmação exato
   - Checkbox marcado
6. POST /api/accountdeletion/delete-account com:
   { password, confirmationText }
7. Se sucesso:
   - Limpa sessionStorage e localStorage
   - Desloga o usuário
   - Redireciona para /account-deleted
```

#### Página de Confirmação: AccountDeleted (`/account-deleted`)

| Item | Detalhe |
|---|---|
| **Rota** | `/account-deleted` |
| **Autorização** | `[AllowAnonymous]` |
| **Layout** | Card centralizado com fundo gradiente |
| **Conteúdo** | Confirmação LGPD, mensagem de agradecimento, links para criar nova conta ou fazer login |

#### Regras de Negócio (LGPD)

1. **Tripla confirmação:** Senha + texto exato + checkbox — impede exclusão acidental.
2. **Texto exato:** Deve ser exatamente "EXCLUIR MINHA CONTA" (case-sensitive).
3. **Irreversibilidade:** A API exclui permanentemente todos os dados do usuário.
4. **Contagem de dados:** O usuário vê exatamente quanto dado será perdido antes de confirmar.
5. **Limpeza local:** Além da exclusão remota, `sessionStorage` e `localStorage` são limpos.

---

## 20. PWA, Service Worker e Push Notifications

### Progressive Web App (PWA)

O MoneyManager é configurado como PWA, permitindo instalação no dispositivo.

**Manifesto (`manifest.json`):**
```json
{
  "name": "MoneyManager",
  "short_name": "MoneyManager",
  "start_url": "./",
  "display": "standalone",
  "background_color": "#ffffff",
  "theme_color": "#4a90d9",
  "prefer_related_applications": false
}
```

### Service Worker (`service-worker.js`)

- **Modo de desenvolvimento:** Não faz caching (bypass de fetch).
- **Modo de publicação (`service-worker.published.js`):** Implementa caching offline de assets estáticos.

**Estratégia de cache:**
- Assets do Blazor (`.dll`, `.wasm`, `.dat`) são cacheados.
- Requisições para API (`/api/`) **não são cacheadas** — sempre fazem fetch real.
- Se offline e não houver cache, retorna fallback.

### Push Notifications

**Serviço:** `PushNotificationService.cs`

**Funcionalidades:**
- `SubscribeAsync()`: Registra subscription via Web Push API (VAPID).
- `UnsubscribeAsync()`: Remove subscription.
- `SendTestNotificationAsync()`: Envia notificação de teste.
- `GetPublicKeyAsync()`: Obtém chave VAPID pública da API.
- `GetStatusAsync()`: Verifica se o usuário tem subscription ativa.

**Integração com o Worker Service:**
- O Worker Service (backend) envia push notifications quando:
  - Transações recorrentes são processadas.
  - Lembrete diário é agendado.

---

## 21. Localização (i18n)

### Serviço: LocalizationService

**Arquivo de traduções:** `wwwroot/i18n/pt-BR.json`

**Implementação:**
- Carrega o arquivo JSON assincronamente na inicialização.
- Expõe o método `T(key)` para buscar traduções por chave.
- Suporta chaves aninhadas via notação de ponto (ex: `nav.dashboard`).
- Se a chave não for encontrada, retorna a própria chave como fallback.

**Cobertura atual:**
- O `pt-BR.json` contém traduções para navegação, botões e labels comuns.
- **Parcialmente implementado:** Muitas páginas ainda usam textos hardcoded em português, sem passar pelo `LocalizationService`.

**Idiomas planejados:** pt-BR, en-US, es-ES, fr-FR (apenas pt-BR implementado).

---

## 22. Sistema de Temas (Light/Dark)

### JavaScript: theme-manager.js

**Funções:**
- `themeManager.setTheme(theme)`: Aplica tema ("light", "dark" ou "auto").
- `themeManager.getTheme()`: Retorna o tema atual.
- `themeManager.applyTheme()`: Aplica tema do `localStorage`.

**Mecanismo:**
1. Define `data-theme` no `<html>` element.
2. Salva preferência no `localStorage["theme"]`.
3. Para `auto`: usa `window.matchMedia("(prefers-color-scheme: dark)")` para detectar preferência do SO.
4. Aplica listener de mudança para tema `auto` reagir quando o SO mudar.

### CSS

O tema é implementado via CSS Custom Properties:

```css
:root {
  --bg-primary: #ffffff;
  --text-primary: #212529;
  /* ... outros tokens de cor */
}

[data-theme="dark"] {
  --bg-primary: #1a1a2e;
  --text-primary: #e1e1e6;
  /* ... cores escuras */
}
```

Todos os componentes usam `var(--bg-primary)` etc. para renderizar corretamente em ambos os temas.

---

## 23. Mapa Completo de Endpoints da API

### Autenticação

| Método | Endpoint | Descrição |
|---|---|---|
| POST | `/api/auth/login` | Login com email e senha |
| POST | `/api/auth/register` | Registro de novo usuário |

### Contas

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/api/accounts` | Lista todas as contas |
| POST | `/api/accounts` | Cria nova conta |
| PUT | `/api/accounts/{id}` | Atualiza conta |
| DELETE | `/api/accounts/{id}` | Remove conta |

### Categorias

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/api/categories` | Lista todas as categorias |
| POST | `/api/categories` | Cria nova categoria |
| PUT | `/api/categories/{id}` | Atualiza categoria |
| DELETE | `/api/categories/{id}` | Remove categoria |

### Transações

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/api/transactions` | Lista transações (com paginação e filtros) |
| POST | `/api/transactions` | Cria nova transação |
| POST | `/api/transactions/installment-purchase` | Cria compra parcelada |
| PUT | `/api/transactions/{id}` | Atualiza transação |
| DELETE | `/api/transactions/{id}` | Remove transação |

**Query Params (GET):**
- `page` (int): Página (1-based)
- `pageSize` (int): Tamanho da página (padrão: 50)
- `sortBy` (string): `date_desc`, `date_asc`, `amount_desc`, `amount_asc`
- `startDate` (DateTime): Filtro de data início
- `endDate` (DateTime): Filtro de data fim
- `type` (string): `income`, `expense`, `investment`
- `accountId` (string): Filtro por conta

### Transações Recorrentes

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/api/recurringtransactions` | Lista todas as recorrentes |
| POST | `/api/recurringtransactions` | Cria nova recorrente |
| PUT | `/api/recurringtransactions/{id}` | Atualiza recorrente |
| DELETE | `/api/recurringtransactions/{id}` | Remove recorrente |

### Orçamentos

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/api/budgets/{month}` | Carrega orçamento do mês (formato: yyyy-MM) |
| POST | `/api/budgets` | Cria novo orçamento |
| PUT | `/api/budgets/{id}` | Atualiza orçamento |

### Dashboard

| Método | Endpoint | Descrição |
|---|---|---|
| — | — | O Dashboard não possui endpoint próprio. Usa dados de `/api/accounts`, `/api/transactions`, `/api/categories` e `/api/budgets`. |

### Relatórios

| Método | Endpoint | Descrição |
|---|---|---|
| — | — | Os Relatórios não possuem endpoint próprio. Usam dados de `/api/transactions` e `/api/categories`. |

### Faturas de Cartão de Crédito

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/api/credit-card-invoices/{id}` | Detalhes de uma fatura |
| GET | `/api/credit-card-invoices/{id}/summary` | Resumo da fatura |
| GET | `/api/credit-card-invoices/{id}/transactions` | Transações da fatura |
| GET | `/api/credit-card-invoices/accounts/{accountId}` | Faturas de um cartão |
| GET | `/api/credit-card-invoices/accounts/{accountId}/open` | Fatura aberta do cartão |
| POST | `/api/credit-card-invoices/accounts/{accountId}/history` | Histórico de faturas |
| GET | `/api/credit-card-invoices/pending` | Faturas pendentes (global) |
| GET | `/api/credit-card-invoices/overdue` | Faturas vencidas (global) |
| POST | `/api/credit-card-invoices/{id}/close` | Fecha fatura |
| POST | `/api/credit-card-invoices/pay` | Paga fatura integralmente |
| POST | `/api/credit-card-invoices/pay-partial` | Paga fatura parcialmente |
| POST | `/api/credit-card-invoices/{id}/recalculate` | Recalcula fatura |
| GET | `/api/credit-card-invoices/accounts/{accountId}/determine?transactionDate=` | Determina fatura de uma transação |

### Perfil

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/api/profile` | Carrega perfil do usuário |
| PUT | `/api/profile` | Atualiza informações do perfil |
| POST | `/api/profile/change-password` | Altera senha |
| POST | `/api/profile/update-email` | Altera e-mail |

### Configurações

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/api/settings` | Carrega configurações |
| PUT | `/api/settings` | Salva configurações |

### Onboarding

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/api/onboarding/status` | Verifica status do onboarding |
| GET | `/api/onboarding/category-suggestions` | Sugestões de categorias |
| POST | `/api/onboarding/complete` | Marca onboarding como completo |

### Exclusão de Conta (LGPD)

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/api/accountdeletion/data-count` | Contagem de dados do usuário |
| POST | `/api/accountdeletion/delete-account` | Exclui conta permanentemente |

### Push Notifications

| Método | Endpoint | Descrição |
|---|---|---|
| GET | `/api/push/public-key` | Chave VAPID pública |
| GET | `/api/push/status` | Status da subscription |
| POST | `/api/push/subscribe` | Registra subscription |
| POST | `/api/push/unsubscribe` | Remove subscription |
| POST | `/api/push/test` | Envia notificação de teste |

---

> **Última atualização:** Gerado automaticamente a partir da análise do código-fonte do MoneyManager.Web.
