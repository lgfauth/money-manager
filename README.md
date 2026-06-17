# MoneyManager

Sistema de gerenciamento financeiro pessoal desenvolvido com **.NET 9** e **Next.js 15**, seguindo os princípios de **Clean Architecture**.

O repositório é composto por múltiplas aplicações independentes: uma API operacional para os usuários finais, uma API administrativa para operações internas, um worker de background para tarefas agendadas, um frontend web para o usuário e um portal de administração.

---

## Sumário

- [Arquitetura geral](#arquitetura-geral)
- [Aplicações](#aplicações)
  - [API Operacional](#api-operacional)
  - [API de Backoffice](#api-de-backoffice)
  - [Frontend Web](#frontend-web)
  - [Portal de Administração](#portal-de-administração)
  - [Worker Operacional](#worker-operacional)
- [Bibliotecas de suporte](#bibliotecas-de-suporte)
- [Testes](#testes)
- [Estrutura de pastas](#estrutura-de-pastas)
- [Como executar](#como-executar)
- [Variáveis de ambiente](#variáveis-de-ambiente)

---

## Arquitetura geral

O projeto segue **Clean Architecture** dividida em quatro camadas:

| Camada | Projeto | Responsabilidade |
|---|---|---|
| Domain | `MoneyManager.Domain` | Entidades, enums e interfaces — zero dependências externas |
| Application | `MoneyManager.Application` | Serviços, DTOs e validadores (FluentValidation) |
| Infrastructure | `MoneyManager.Infrastructure` | MongoDB, repositórios, JWT, IA e fila de controle de workers |
| Presentation | `MoneyManager.Api.Operational` / `MoneyManager.Api.Backoffice` | Controllers REST, middlewares e composição do DI |

Todas as APIs compartilham as mesmas camadas de suporte (Domain, Application, Infrastructure). O Worker opera diretamente sobre Application e Infrastructure, sem passar pelo HTTP.

```
src/
├── APIs/
│   ├── MoneyManager.Api.Operational/   ← API principal (usuários)
│   └── MoneyManager.Api.Backoffice/    ← API administrativa
├── Frontends/
│   ├── MoneyManager.Web/               ← Frontend do usuário (Next.js 15)
│   └── MoneyManager.Backoffice/        ← Portal admin (Next.js 16)
├── Supports/
│   ├── MoneyManager.Domain/
│   ├── MoneyManager.Application/
│   ├── MoneyManager.Infrastructure/
│   └── MoneyManager.Observability/
└── Workers/
    └── MoneyManager.Worker.Operational/
```

---

## Aplicações

### API Operacional

**Projeto:** `src/APIs/MoneyManager.Api.Operational`  
**Runtime:** .NET 9 / ASP.NET Core Web API  
**Porta padrão:** 5000

API REST consumida pelo frontend web. Oferece todas as funcionalidades financeiras para o usuário autenticado.

#### Funcionalidades

| Módulo | Endpoints | Descrição |
|---|---|---|
| Autenticação | `POST /api/auth/register`, `POST /api/auth/login`, `POST /api/auth/logout` | Registro, login com JWT em cookie httpOnly, logout com revogação de token |
| Categorias | `GET/POST /api/categories`, `PUT/DELETE /api/categories/{id}` | Categorias de receita e despesa por usuário |
| Contas | `GET/POST /api/accounts`, `GET/PUT/DELETE /api/accounts/{id}` | Contas bancárias com tipos: Checking, Savings, Cash, CreditCard, Investment |
| Transações | `GET/POST /api/transactions`, `PUT/DELETE /api/transactions/{id}` | Receitas, despesas e transferências com impacto automático no saldo |
| Orçamentos | `GET/POST /api/budgets`, `GET/PUT /api/budgets/{month}` | Limites por categoria para cada mês |
| Cartões de crédito | `GET/POST /api/credit-cards`, `GET/PUT/DELETE /api/credit-cards/{id}` | Cartões com controle de limite e fechamento de fatura |
| Transações de cartão | `GET/POST /api/credit-card-transactions` | Compras no crédito com suporte a parcelamento |
| Recorrências | `GET/POST /api/recurring-transactions`, `PUT/DELETE /api/recurring-transactions/{id}` | Transações recorrentes (diário, semanal, mensal, anual) |
| Relatórios | `GET /api/reports/summary`, `GET /api/reports/by-category`, `GET /api/reports/cashflow` | Resumo do mês, despesas por categoria e fluxo de caixa |
| Onboarding | `GET /api/onboarding/status`, `POST /api/onboarding/complete` | Guia de configuração inicial para novos usuários |
| Perfil | `GET/PUT /api/profile` | Dados pessoais e foto de perfil do usuário |
| Configurações | `GET/PUT /api/settings` | Preferências do usuário (moeda, locale, tema) |
| Exclusão de conta | `POST /api/account-deletion` | Remoção completa de todos os dados (conformidade LGPD) |
| Análise de comprovantes | `POST /api/receipts/analyze` | Extração de dados de imagens de comprovantes via IA (Claude) |
| Push notifications | `POST /api/push/subscribe`, `DELETE /api/push/unsubscribe` | Notificações via Web Push (VAPID) |
| Relatórios de usuário | `GET/POST /api/user-reports` | Envio de feedback e relatos de problemas |
| Documentos legais | `GET /api/documents` | Termos de uso e política de privacidade |

#### Destaques técnicos

- **JWT Bearer** com suporte a cookie `httpOnly` — token não exposto ao JavaScript
- **Token blacklist** em memória para revogação imediata no logout
- **Rate limiting** (10 req/min) nos endpoints de autenticação — proteção contra brute-force
- **FluentValidation** em pipeline, nunca dentro dos services
- **Soft Delete** — exclusões usam `IsDeleted = true`
- **User Isolation** — todas as queries incluem filtro por `UserId`
- **NLog** para logging estruturado com configuração via `nlog.config`
- **IA generativa** integrada via Anthropic Claude para análise de comprovantes fiscais
- **CORS** configurável por variável de ambiente

---

### API de Backoffice

**Projeto:** `src/APIs/MoneyManager.Api.Backoffice`  
**Runtime:** .NET 9 / ASP.NET Core Web API  
**Porta padrão:** 5091

API exclusiva para operadores e administradores do sistema. Não é acessível pelo frontend do usuário final.

#### Funcionalidades

| Módulo | Endpoints | Descrição |
|---|---|---|
| Autenticação admin | `POST /api/admin/auth/login` | Login com credenciais de administrador |
| Status do sistema | `GET /api/admin/system-status` | Saúde da API, MongoDB e Worker |
| Histórico de jobs | `GET /api/admin/jobs/history` | Últimas execuções dos workers com status e duração |
| Controle de jobs | `POST /api/admin/jobs/{jobName}/run-now` | Execução manual de workers com auditoria |
| Observabilidade | `GET /api/admin/process-logs` | Logs estruturados de execução dos processos |
| Documentos legais | `GET/PUT /api/admin/documents` | Edição de termos de uso e política de privacidade |
| Auditoria | Registro automático | Todas as ações de operadores são registradas em audit log |

#### Destaques técnicos

- **RBAC** com três níveis de acesso: `Viewer`, `Operator` e `Admin`
- JWT com secret separado do JWT operacional (`ADMIN_AUTH_SECRET`)
- **Fila de comandos de worker** (`WorkerCommandQueueService`) — permite enfileirar `run-now` para os workers via MongoDB
- **Audit log** para todas as ações sensíveis
- CORS restrito às origens explicitamente configuradas via `ADMIN_PORTAL_ALLOWED_ORIGINS`

---

### Frontend Web

**Projeto:** `src/Frontends/MoneyManager.Web`  
**Stack:** Next.js 15, React 19, TypeScript  
**Porta padrão:** 8000

Interface principal para o usuário final. Consome a API Operacional.

#### Tecnologias

| Biblioteca | Uso |
|---|---|
| Next.js 15 + App Router | Roteamento, SSR e CSR |
| React 19 | UI |
| Tailwind CSS v4 | Estilização |
| shadcn/ui + Radix UI | Componentes de interface |
| TanStack Query v5 | Data fetching, cache e sincronização de estado servidor |
| Zustand | Estado global do cliente |
| React Hook Form + Zod | Formulários e validação |
| next-intl | Internacionalização (i18n) |
| Recharts | Gráficos financeiros (donut, linha, barras) |
| Framer Motion | Animações |
| date-fns | Manipulação de datas |
| Sonner | Toasts e notificações |

#### Páginas e funcionalidades

| Página | Rota | Descrição |
|---|---|---|
| Dashboard | `/` | Resumo financeiro do mês atual |
| Contas | `/accounts` | Listagem e gestão de contas |
| Transações | `/transactions` | Histórico e lançamento de transações |
| Cartões | `/credit-cards` | Cartões de crédito e faturas |
| Recorrências | `/recurring` | Transações recorrentes (agendamentos) |
| Orçamentos | `/budgets` | Orçamentos mensais por categoria |
| Categorias | `/categories` | Gestão de categorias |
| Relatórios | `/reports` | Relatórios com gráficos e análises |
| Perfil | `/profile` | Dados pessoais e exclusão de conta |
| Configurações | `/settings` | Preferências do usuário |
| Onboarding | `/onboarding` | Configuração guiada para novos usuários |
| Termos/Privacidade | `/termos-de-uso`, `/politica-de-privacidade` | Documentos legais públicos |

#### Destaques

- **Modo privacidade** — oculta valores monetários com um clique (`money-privacy-store`)
- **Tema claro/escuro** via `next-themes`
- Middleware de autenticação que protege todas as rotas do dashboard
- Layout responsivo com navegação lateral colapsável

---

### Portal de Administração

**Projeto:** `src/Frontends/MoneyManager.Backoffice`  
**Stack:** Next.js 16, React 19, TypeScript  
**Porta padrão:** 3010

Portal web leve para operadores e administradores. Consome exclusivamente a API de Backoffice.

#### Páginas

| Página | Rota | Descrição |
|---|---|---|
| Overview | `/` | Saúde do sistema (API, MongoDB, Worker) |
| Jobs | `/jobs` | Status e disparo manual de workers |
| Auditoria | `/audit` | Histórico de ações dos operadores |
| Documentos | `/documents` | Edição de termos e políticas legais |
| Erros e latência | `/errors-latency` | Monitoramento de erros e performance |
| Manutenção financeira | `/financial-maintenance` | Operações administrativas sobre dados |

#### Destaques

- Interface minimalista sem dependências de componentes externos
- Autenticação com JWT de sessão curta (15 minutos por padrão)
- Editor Markdown integrado (`@uiw/react-md-editor`) para edição de documentos legais

---

### Worker Operacional

**Projeto:** `src/Workers/MoneyManager.Worker.Operational`  
**Runtime:** .NET 9 Worker Service (`IHostedService`)

Serviço de background responsável por processar tarefas agendadas. Não expõe HTTP — opera diretamente sobre as camadas Application e Infrastructure.

#### Workers

| Worker | Processo | Descrição |
|---|---|---|
| `ScheduledTransactionWorker` | `RecurringTransactions` | Verifica e lança transações recorrentes com data de vencimento atingida |
| `DailyReminderWorker` | `DailyReminder` | Envia push notifications diárias de lembrete para usuários com assinatura ativa |
| `CreditCardInvoiceWorker` | `CreditCardInvoiceStatus` | Fecha faturas de cartão e atualiza o status das invoices |

#### Destaques

- Cada worker tem seu próprio `IHostedService` com schedule configurável via `appsettings.json`
- **Fila de comandos** — o backoffice pode enfileirar `run-now` para execução imediata fora do horário agendado
- Todas as execuções são registradas via `IProcessLogger` e persistidas no MongoDB como `worker_process_logs`
- Suporte a `ITimeProvider` para testes sem depender de `DateTime.Now`

---

## Bibliotecas de suporte

### MoneyManager.Domain

Camada de domínio sem dependências externas.

**Entidades:**

| Entidade | Descrição |
|---|---|
| `User` | Usuário do sistema com hash de senha |
| `Account` | Conta financeira (corrente, poupança, dinheiro, cartão, investimento) |
| `Category` | Categoria de receita ou despesa |
| `Transaction` | Lançamento financeiro (receita, despesa, transferência) |
| `Budget` | Orçamento mensal por categoria |
| `CreditCard` | Cartão de crédito com limite e data de fechamento |
| `CreditCardInvoice` | Fatura mensal de cartão com status (Open, Closed, Paid) |
| `CreditCardTransaction` | Compra no crédito com suporte a parcelamento |
| `RecurringTransaction` | Agendamento de transação recorrente com frequência configurável |
| `PushSubscription` | Assinatura Web Push do usuário |
| `UserReport` | Relato / feedback enviado pelo usuário |
| `UserSettings` | Preferências do usuário (moeda, locale) |

### MoneyManager.Application

Orquestra o domínio. Contém serviços, DTOs e validadores.

- **Services:** `AuthService`, `AccountService`, `CategoryService`, `TransactionService`, `BudgetService`, `ReportService`, `CreditCardService`, `CreditCardInvoiceService`, `CreditCardTransactionService`, `RecurringTransactionService`, `OnboardingService`, `UserProfileService`, `UserSettingsService`, `AccountDeletionService`, `PushService`, `UserReportService`, `TokenBlacklistService`
- **Validators:** FluentValidation — registrados no pipeline da API, nunca chamados diretamente nos services
- **DTOs:** objetos `*Request` e `*Response` separados por feature

### MoneyManager.Infrastructure

Implementações técnicas. Nunca referenciada diretamente por Domain ou Application.

- **MongoDB** — driver oficial, sem EF Core; `MongoContext` gerencia coleções e índices
- **Repositories** — `UserRepository`, `TransactionRepository`, `CreditCardRepository`, `CreditCardInvoiceRepository`, `CreditCardTransactionRepository`, `PushSubscriptionRepository`, `Repository<T>` genérico
- **UnitOfWork** — coordena múltiplas coleções na mesma operação
- **TokenService** — geração e validação de JWT
- **AnthropicReceiptAnalysisService** — integração com Claude para análise de imagens de comprovantes
- **MongoProcessLogStore** — persiste logs de execução dos workers no MongoDB
- **WorkerCommandQueueService** — fila de comandos para controle dos workers pelo backoffice

### MoneyManager.Observability

Biblioteca de observabilidade reutilizada pela API, Worker e Backoffice.

- `IProcessLogger` — abstração para logging estruturado por processo (`Start` → `AddStep` → `AddWarning` → `AddError` → `Finish`)
- `ProcessLogger` — implementação que acumula os steps e persiste o documento completo ao finalizar
- `IProcessLogStore` — persiste o `ProcessLogDocument` no MongoDB
- `IProcessLogHistoryReader` — leitura do histórico de execuções para o backoffice

---

## Testes

**Projeto:** `tests/MoneyManager.Tests`  
**Frameworks:** xUnit + NSubstitute

| Arquivo de teste | Cobertura |
|---|---|
| `AuthServiceTests` | Registro, login, senhas inválidas |
| `AccountServiceTests` | CRUD de contas, saldo inicial |
| `CategoryServiceTests` | CRUD de categorias, isolamento por usuário |
| `TransactionServiceTests` | Lançamentos, impacto no saldo, transferências |
| `BudgetServiceTests` | Criação e atualização de orçamentos |
| `ReportServiceTests` | Resumo, categorias, fluxo de caixa |
| `RecurringTransactionServiceTests` | Criação, processamento de recorrências |
| `UserProfileServiceTests` | Atualização de perfil |
| `UserSettingsServiceTests` | Preferências do usuário |
| `UsersControllerTests` | Camada de apresentação — controller de usuários |
| `Domain/Entities/` | Testes de comportamento das entidades de domínio |

Padrão: **Arrange / Act / Assert** com comentários de seção. Mocks via `Substitute.For<IInterface>()`.

---

## Estrutura de pastas

```
money-manager/
├── src/
│   ├── APIs/
│   │   ├── MoneyManager.Api.Operational/   ← API do usuário (.NET 9)
│   │   └── MoneyManager.Api.Backoffice/    ← API administrativa (.NET 9)
│   ├── Frontends/
│   │   ├── MoneyManager.Web/               ← Frontend do usuário (Next.js 15)
│   │   └── MoneyManager.Backoffice/        ← Portal admin (Next.js 16)
│   ├── Supports/
│   │   ├── MoneyManager.Domain/            ← Entidades e interfaces
│   │   ├── MoneyManager.Application/       ← Serviços, DTOs, validadores
│   │   ├── MoneyManager.Infrastructure/    ← MongoDB, JWT, IA, repositórios
│   │   └── MoneyManager.Observability/     ← Biblioteca de logging estruturado
│   └── Workers/
│       └── MoneyManager.Worker.Operational/ ← Worker de background (.NET 9)
├── tests/
│   └── MoneyManager.Tests/                 ← Testes unitários (xUnit)
├── docker-compose.yml
└── MoneyManager.sln
```

---

## Como executar

### Pré-requisitos

- .NET 9 SDK
- Node.js 20+
- Docker e Docker Compose (opcional)
- MongoDB (local ou via Docker)

### Com Docker Compose

```bash
docker-compose up -d
```

Serviços disponíveis após inicialização:

| Serviço | URL |
|---|---|
| API Operacional | http://localhost:5000 |
| API de Backoffice | http://localhost:5091 |
| Frontend Web | http://localhost:8000 |
| Portal Admin | http://localhost:3010 |
| Mongo Express | http://localhost:8081 |

### Executando individualmente

**API Operacional:**
```bash
cd src/APIs/MoneyManager.Api.Operational
dotnet run
```

**API de Backoffice:**
```bash
cd src/APIs/MoneyManager.Api.Backoffice
dotnet run
```

**Worker Operacional:**
```bash
cd src/Workers/MoneyManager.Worker.Operational
dotnet run
```

**Frontend Web:**
```bash
cd src/Frontends/MoneyManager.Web
npm ci
npm run dev
```

**Portal Admin:**
```bash
cd src/Frontends/MoneyManager.Backoffice
npm ci
npm run dev   # porta 3010
```

### Testes

```bash
dotnet test tests/MoneyManager.Tests/MoneyManager.Tests.csproj
```

---

## Variáveis de ambiente

### API Operacional

| Variável | Descrição | Obrigatória |
|---|---|---|
| `MongoDB__ConnectionString` | String de conexão com o MongoDB | Sim |
| `MongoDB__DatabaseName` | Nome do banco de dados | Sim |
| `Jwt__SecretKey` | Chave secreta para assinatura do JWT (mín. 32 chars) | Sim |
| `Jwt__Issuer` | Issuer do token JWT | Não (padrão: `MoneyManager`) |
| `Jwt__Audience` | Audience do token JWT | Não (padrão: `MoneyManagerUsers`) |
| `Jwt__ExpirationHours` | Tempo de expiração do token em horas | Não (padrão: `1`) |
| `Anthropic__ApiKey` | Chave de API da Anthropic para análise de comprovantes | Não |
| `Anthropic__Model` | Modelo Claude a utilizar | Não (padrão: `claude-haiku-4-5`) |
| `Vapid__PublicKey` | Chave pública VAPID para Web Push | Não |
| `Vapid__PrivateKey` | Chave privada VAPID para Web Push | Não |
| `AllowedOrigins__0` | Origem do frontend autorizada para CORS | Sim |

### API de Backoffice

| Variável | Descrição | Obrigatória |
|---|---|---|
| `MongoDB__ConnectionString` | String de conexão com o MongoDB | Sim |
| `MongoDB__DatabaseName` | Nome do banco de dados | Sim |
| `ADMIN_AUTH_SECRET` | Chave secreta JWT para o admin (mín. 32 chars) | Sim |
| `ADMIN_PORTAL_ALLOWED_ORIGINS` | Origens permitidas para CORS (separadas por vírgula) | Sim |

### Worker Operacional

| Variável | Descrição |
|---|---|
| `MongoDB__ConnectionString` | String de conexão com o MongoDB |
| `MongoDB__DatabaseName` | Nome do banco de dados |
| `WorkerOptions__*` | Configurações de comportamento do worker |
| `ScheduleOptions__*` | Horários de execução do `ScheduledTransactionWorker` |
| `DailyReminderScheduleOptions__*` | Horários do `DailyReminderWorker` |
| `CreditCardInvoiceScheduleOptions__*` | Horários do `CreditCardInvoiceWorker` |
