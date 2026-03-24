# MoneyManager — Guia de Arquitetura e Desenvolvimento

> Documento central de referência para o desenvolvimento futuro da solução.
> Mantenha este arquivo atualizado sempre que houver mudanças estruturais.

---

## Sumário

1. [Visão Geral da Solução](#1-visão-geral-da-solução)
2. [Diagrama de Camadas](#2-diagrama-de-camadas)
3. [Projetos da Solução](#3-projetos-da-solução)
4. [Modelo de Dados](#4-modelo-de-dados)
5. [Fluxo de Autenticação](#5-fluxo-de-autenticação)
6. [Persistência e Banco de Dados](#6-persistência-e-banco-de-dados)
7. [Comunicação entre Camadas](#7-comunicação-entre-camadas)
8. [Localização e Internacionalização](#8-localização-e-internacionalização)
9. [Infraestrutura e Deploy](#9-infraestrutura-e-deploy)
10. [Padrões e Convenções](#10-padrões-e-convenções)

---

## 1. Visão Geral da Solução

**MoneyManager** é uma aplicação de gerenciamento financeiro pessoal.
Permite ao usuário controlar contas, transações, cartões de crédito, orçamentos, categorias e transações recorrentes.

### Stack Principal

| Camada | Tecnologia |
|---|---|
| Frontend | Blazor WebAssembly (.NET 9) |
| Backend API | ASP.NET Core (.NET 9) |
| Background Jobs | .NET Worker Service (.NET 9) |
| Banco de Dados | MongoDB |
| Autenticação | JWT (Bearer) |
| Validação | FluentValidation |
| Testes | xUnit + NSubstitute |
| Logging | NLog (API), ILogger (Worker) |
| Deploy | Railway (API + Worker), Railway ou CDN (Web) |

---

## 2. Diagrama de Camadas

```
???????????????????????????????????????????????????????????????
?                     MoneyManager.Web                         ?
?            (Blazor WebAssembly — SPA Frontend)               ?
?   Pages/ ? Services/ ? Shared/ ? wwwroot/i18n/              ?
???????????????????????????????????????????????????????????????
                         ? HTTP (REST + JWT)
???????????????????????????????????????????????????????????????
?                  MoneyManager.Presentation                   ?
?           (ASP.NET Core Web API — Host da API)               ?
?   Controllers/ ? Middlewares/ ? Program.cs                   ?
???????????????????????????????????????????????????????????????
                         ? interfaces
???????????????????????????????????????????????????????????????
?                  MoneyManager.Application                    ?
?         (Lógica de Negócio — Orquestração de Serviços)       ?
?   Services/ ? DTOs/ ? Validators/                            ?
???????????????????????????????????????????????????????????????
             ?????????????????????????
             ?                       ?
???????????????????????   ???????????????????????????????????
?  MoneyManager.Domain ?   ?  MoneyManager.Infrastructure     ?
?  (Contratos e        ?   ?  (Repositórios, MongoDB,         ?
?   Entidades)         ?   ?   TokenService)                  ?
???????????????????????   ????????????????????????????????????

???????????????????????????????????????????????????????????????
?                    MoneyManager.Worker                       ?
?         (.NET Worker Service — Jobs em Background)           ?
?   WorkerHost/Services/ ? WorkerHost/Options/                 ?
?   (Referencia Application + Infrastructure)                  ?
???????????????????????????????????????????????????????????????

???????????????????????????????????????????????????????????????
?                   MoneyManager.Web.Host                      ?
?     (Host estático para o Blazor WASM em produção)           ?
?   Serve wwwroot/ do MoneyManager.Web                         ?
???????????????????????????????????????????????????????????????
```

### Dependências entre projetos

```
Web          ? (nenhuma referência ao backend — comunicação só via HTTP)
Presentation ? Application, Infrastructure, Domain
Application  ? Domain
Infrastructure ? Domain
Worker       ? Application, Infrastructure, Domain
Web.Host     ? (nenhuma referência C# — serve arquivos estáticos)
Tests        ? Application, Domain
```

---

## 3. Projetos da Solução

### 3.1 MoneyManager.Domain

**Tipo:** Class Library  
**Responsabilidade:** Coração do domínio — entidades, enums, interfaces de repositório.

#### Estrutura de Pastas

```
src/MoneyManager.Domain/
??? Entities/          # Entidades do domínio
?   ??? Account.cs
?   ??? Budget.cs
?   ??? Category.cs
?   ??? CreditCardInvoice.cs
?   ??? RecurringTransaction.cs
?   ??? Transaction.cs
?   ??? User.cs
?   ??? UserSettings.cs
??? Enums/             # Enumerações do domínio
?   ??? AccountType.cs
?   ??? CategoryType.cs
?   ??? InvoiceStatus.cs
?   ??? RecurrenceFrequency.cs
?   ??? TransactionStatus.cs
?   ??? TransactionType.cs
?   ??? UserStatus.cs
??? Interfaces/        # Contratos de repositório
    ??? ICreditCardInvoiceRepository.cs
    ??? IRepository.cs      ? repositório genérico
    ??? IUnitOfWork.cs      ? agregador de repositórios
    ??? IUserRepository.cs
```

#### Regras desta camada

- Nenhuma dependência de infraestrutura ou UI.
- Os atributos MongoDB (`[BsonElement]`, etc.) vivem aqui por decisão pragmática; não introduzir dependências de outras tecnologias além do driver MongoDB.
- Toda entidade usa `IsDeleted` para soft delete.
- Datas sempre em `DateTime.UtcNow`.

---

### 3.2 MoneyManager.Application

**Tipo:** Class Library  
**Responsabilidade:** Orquestração da lógica de negócio — serviços, DTOs, validadores.

#### Estrutura de Pastas

```
src/MoneyManager.Application/
??? DTOs/
?   ??? Request/       # Objetos de entrada
?   ??? Response/      # Objetos de saída
??? Services/          # Implementações de serviços + interfaces (mesmo arquivo)
?   ??? AccountDeletionService.cs
?   ??? AccountService.cs
?   ??? AuthService.cs
?   ??? BudgetService.cs
?   ??? CategoryService.cs
?   ??? CreditCardInvoiceService.cs
?   ??? OnboardingService.cs
?   ??? RecurringTransactionService.cs
?   ??? ReportService.cs
?   ??? TransactionService.cs
?   ??? UserProfileService.cs
?   ??? UserSettingsService.cs
??? Validators/        # FluentValidation
    ??? CreateAccountValidator.cs
    ??? CreateCategoryValidator.cs
    ??? CreateTransactionValidator.cs
    ??? LoginRequestValidator.cs
    ??? RegisterRequestValidator.cs
```

#### Convenção de Interface + Implementação

As interfaces de serviço são declaradas **no mesmo arquivo** que a implementação:

```csharp
// AuthService.cs
public interface IAuthService { ... }
public class AuthService : IAuthService { ... }
```

#### Regras desta camada

- Nenhum acesso direto a HTTP, banco de dados ou UI.
- Serviços recebem `IUnitOfWork` via injeção de dependência.
- Todos os serviços são `Scoped` no container.
- Logging com `ILogger<T>` seguindo o padrão structured logging.
- Exceções usadas como sinalizadores de negócio: `KeyNotFoundException` = 404, `InvalidOperationException` = 400.

---

### 3.3 MoneyManager.Infrastructure

**Tipo:** Class Library  
**Responsabilidade:** Persistência (MongoDB), segurança (JWT), Unit of Work.

#### Estrutura de Pastas

```
src/MoneyManager.Infrastructure/
??? Data/
?   ??? MongoContext.cs       # Conexão, criação de coleções, índices
?   ??? MongoSettings.cs      # Configuração de conexão
??? Repositories/
?   ??? CreditCardInvoiceRepository.cs   # Repositório especializado
?   ??? Repository.cs                    # Repositório genérico
?   ??? UnitOfWork.cs                    # Instanciação lazy dos repositórios
?   ??? UserRepository.cs               # Repositório especializado
??? Security/
    ??? TokenService.cs       # Geração de JWT
```

#### Mapeamento MongoDB

| Entidade | Collection |
|---|---|
| `User` | `users` |
| `Category` | `categories` |
| `Account` | `accounts` |
| `Transaction` | `transactions` |
| `Budget` | `budgets` |
| `RecurringTransaction` | `recurring_transactions` |
| `UserSettings` | `user_settings` |
| `CreditCardInvoice` | `credit_card_invoices` |

#### Índices Criados (startup)

- `users`: `email` (para lookup por email)
- `categories`: `userId + createdAt`
- `transactions`: `userId + date`
- `accounts`: `userId`
- `budgets`: `userId + month`

#### Regras desta camada

- O `UnitOfWork` instancia repositórios lazily.
- O `Repository<T>` genérico serve todas as coleções sem necessidades especiais.
- Repositórios especializados (`UserRepository`, `CreditCardInvoiceRepository`) herdam ou implementam contratos próprios do domínio.
- `SaveChangesAsync()` é no-op (MongoDB é document-oriented; não há transação explícita na maioria dos fluxos).

---

### 3.4 MoneyManager.Presentation

**Tipo:** ASP.NET Core Web API  
**Responsabilidade:** Exposição HTTP — controllers, middlewares, composição do host.

#### Estrutura de Pastas

```
src/MoneyManager.Presentation/
??? Controllers/
?   ??? AccountDeletionController.cs
?   ??? AccountsController.cs
?   ??? AdminController.cs
?   ??? AuthController.cs
?   ??? BudgetsController.cs
?   ??? CategoriesController.cs
?   ??? CreditCardInvoicesController.cs
?   ??? OnboardingController.cs
?   ??? ProfileController.cs
?   ??? RecurringTransactionsController.cs
?   ??? ReportsController.cs
?   ??? SettingsController.cs
?   ??? TransactionsController.cs
??? Extensions/
?   ??? HttpContextExtensions.cs     # Extrai userId do JWT
??? Middlewares/
?   ??? ExceptionHandlingMiddleware.cs
??? Program.cs
```

#### Tabela de Endpoints Principais

| Controller | Rota base | Auth? |
|---|---|---|
| `AuthController` | `/api/auth` | Não |
| `TransactionsController` | `/api/transactions` | Sim |
| `AccountsController` | `/api/accounts` | Sim |
| `CategoriesController` | `/api/categories` | Sim |
| `BudgetsController` | `/api/budgets` | Sim |
| `RecurringTransactionsController` | `/api/recurring-transactions` | Sim |
| `CreditCardInvoicesController` | `/api/credit-card-invoices` | Sim |
| `ReportsController` | `/api/reports` | Sim |
| `ProfileController` | `/api/profile` | Sim |
| `SettingsController` | `/api/settings` | Sim |
| `OnboardingController` | `/api/onboarding` | Sim |
| `AccountDeletionController` | `/api/account` | Sim |
| `AdminController` | `/api/admin` | Sim |

#### Middleware Pipeline (ordem)

1. `ForwardedHeaders` — suporte Railway/proxy
2. CORS manual inline — headers permissivos + preflight 204
3. `ExceptionHandlingMiddleware` — trata exceções não tratadas
4. `UseAuthentication` / `UseAuthorization`
5. Controllers

#### Regras desta camada

- Controllers são thin: recebem a request, validam com FluentValidation, delegam ao serviço e retornam o resultado.
- `GetUserId()` extrai o `NameIdentifier` do JWT.
- Swagger habilitado em todos os ambientes (necessário para Railway).
- NLog configurado via `builder.Host.UseNLog()`.

---

### 3.5 MoneyManager.Web

**Tipo:** Blazor WebAssembly (.NET 9)  
**Responsabilidade:** SPA frontend — páginas, componentes, serviços HTTP, localização.

#### Estrutura de Pastas

```
src/MoneyManager.Web/
??? Pages/             # Páginas Blazor (roteadas)
?   ??? Login.razor
?   ??? Register.razor
?   ??? Index.razor          # Dashboard principal
?   ??? Transactions.razor
?   ??? Accounts.razor
?   ??? Categories.razor
?   ??? Budgets.razor
?   ??? Reports.razor
?   ??? RecurringTransactions.razor
?   ??? CreditCardDashboard.razor
?   ??? InvoiceDetails.razor
?   ??? Profile.razor
?   ??? Settings.razor
?   ??? Onboarding.razor
?   ??? AccountDeleted.razor
??? Shared/            # Componentes reutilizáveis
?   ??? MainLayout.razor
?   ??? NavMenu.razor
?   ??? BusyOverlay.razor
?   ??? ColorPicker.razor
?   ??? LanguageSelector.razor
?   ??? MoneyInput.razor
?   ??? RedirectToLogin.razor
??? Components/        # Componentes especializados
?   ??? InvoiceCard.razor
?   ??? InvoiceStatusBadge.razor
??? Services/          # Serviços de integração com a API
?   ??? AuthService.cs
?   ??? AccountService.cs
?   ??? TransactionService.cs
?   ??? BudgetService.cs
?   ??? CategoryService.cs
?   ??? CreditCardInvoiceService.cs
?   ??? DashboardService.cs
?   ??? RecurringTransactionService.cs
?   ??? ReportService.cs
?   ??? UserProfileService.cs
?   ??? UserSettingsService.cs
?   ??? OnboardingService.cs
?   ??? AccountDeletionService.cs
?   ??? AuthenticationStateProvider.cs
?   ??? AuthorizationMessageHandler.cs
?   ??? ApiConfigService.cs
?   ??? Localization/
?       ??? ILocalizationService.cs
?       ??? LocalizationService.cs
??? wwwroot/
    ??? i18n/          # Arquivos de localização JSON
    ?   ??? pt-BR.json
    ??? js/            # JavaScript interop
    ?   ??? investmentCharts.js
    ??? index.html
```

#### Regras desta camada

- Toda chamada HTTP é feita através dos serviços em `Services/` — nunca `HttpClient` direto em `.razor`.
- `AuthorizationMessageHandler` injeta automaticamente o token JWT nas requisições.
- `CustomAuthenticationStateProvider` gerencia o estado de autenticação no cliente via `Blazored.LocalStorage`.
- Localização carregada de `wwwroot/i18n/pt-BR.json`; chaves seguem padrão `Secao.SubSecao.Chave`.
- URL da API está hardcoded em `Program.cs` — ver risco na seção 9.

---

### 3.6 MoneyManager.Web.Host

**Tipo:** ASP.NET Core (servidor de arquivos estáticos)  
**Responsabilidade:** Hospedar o build publicado do Blazor WASM em produção.

- Não possui controllers ou lógica de negócio.
- Configura MIME types para `.wasm`, `.blat`, `.dat` etc.
- Em desenvolvimento aponta para o `wwwroot/` do `MoneyManager.Web`.
- Em produção serve o `wwwroot/` local copiado pelo publish.
- Rota fallback para `index.html` (padrão SPA).

---

### 3.7 MoneyManager.Worker

**Tipo:** .NET Worker Service (BackgroundService)  
**Responsabilidade:** Processamento em background — transações recorrentes e fechamento de faturas.

#### Estrutura de Pastas

```
src/MoneyManager.Worker/
??? WorkerHost/
?   ??? DependencyInjection/
?   ?   ??? ApplicationServicesExtensions.cs   # Registra serviços Application + Infrastructure
?   ?   ??? ServiceCollectionExtensions.cs     # Ponto de entrada do DI do Worker
?   ??? Options/
?   ?   ??? InvoiceClosureScheduleOptions.cs
?   ?   ??? ScheduleOptions.cs
?   ?   ??? WorkerOptions.cs
?   ??? Services/
?       ??? ITimeProvider.cs
?       ??? SystemTimeProvider.cs
?       ??? ITransactionScheduleProcessor.cs
?       ??? RecurringTransactionsProcessor.cs    # Lógica de processar recorrências
?       ??? InvoiceClosureProcessor.cs           # Lógica de fechamento de faturas
?       ??? ScheduledTransactionWorker.cs        # Hosted service — recorrências
?       ??? InvoiceClosureWorker.cs              # Hosted service — faturas
??? Program.cs
```

#### Workers Ativos

| Worker | Schedule | Função |
|---|---|---|
| `ScheduledTransactionWorker` | 08:00 diário + startup | Criar transações a partir de `RecurringTransaction` vencidas |
| `InvoiceClosureWorker` | 00:01 diário | Fechar faturas de cartão de crédito abertas |

#### Regras desta camada

- Hosted services apenas orquestram (agendam, trigam, logam resultado).
- A lógica de negócio fica em `*Processor` — testável isoladamente.
- Todos os processos devem ser **idempotentes**.
- Timeout configurável via `WorkerOptions.ExecutionTimeoutMinutes`.
- `ITimeProvider` abstrai `DateTimeOffset.UtcNow` para testabilidade.

---

### 3.8 MoneyManager.Tests

**Tipo:** xUnit Test Project  
**Responsabilidade:** Testes unitários da camada Application.

#### Estrutura de Pastas

```
tests/MoneyManager.Tests/
??? Application/
?   ??? Services/
?       ??? AccountServiceTests.cs
?       ??? AuthServiceTests.cs
?       ??? BudgetServiceTests.cs
?       ??? CategoryServiceTests.cs
?       ??? CreditCardInvoiceServiceTests.cs
?       ??? RecurringTransactionServiceTests.cs
?       ??? ReportServiceTests.cs
?       ??? TransactionServiceTests.cs
?       ??? UserProfileServiceTests.cs
?       ??? UserSettingsServiceTests.cs
??? Domain/
    ??? Entities/
        ??? RecurringTransactionBsonTests.cs
```

#### Stack de Testes

- **xUnit** — framework de testes
- **NSubstitute** — mocking
- **Padrão AAA** — Arrange / Act / Assert
- **Nomenclatura:** `MethodName_Scenario_ExpectedResult`

---

## 4. Modelo de Dados

### Entidades Principais e Relacionamentos

```
User (1)
 ??? Account (N)          ? tem tipo: Checking, Savings, CreditCard, Investment
 ?    ??? CreditCardInvoice (N)   ? referenciada por transações
 ??? Category (N)
 ??? Transaction (N)      ? vinculada a Account, Category, CreditCardInvoice
 ??? RecurringTransaction (N)
 ??? Budget (N)           ? agrupa BudgetItems por Category/mês
 ??? UserSettings (1)
```

### Enums Relevantes

| Enum | Valores |
|---|---|
| `TransactionType` | `Income=0`, `Expense=1`, `Transfer=2` |
| `AccountType` | `Checking`, `Savings`, `CreditCard`, `Investment` |
| `TransactionStatus` | `Pending`, `Completed`, `Cancelled` |
| `InvoiceStatus` | `Open`, `Closed`, `Paid` |
| `RecurrenceFrequency` | `Daily`, `Weekly`, `Monthly`, `Yearly` |
| `CategoryType` | `Income`, `Expense` |
| `UserStatus` | `Active`, `Inactive` |

### Convenções de Entidade

- `Id`: `ObjectId` do MongoDB como `string` (`[BsonId]`, `[BsonRepresentation(BsonType.ObjectId)]`)
- `UserId`: `string` — chave de isolamento entre usuários (toda query filtra por `UserId`)
- `IsDeleted`: `bool` — soft delete; queries devem filtrar `!IsDeleted`
- `CreatedAt` / `UpdatedAt`: `DateTime.UtcNow`

---

## 5. Fluxo de Autenticação

```
1. Cliente (Web) envia POST /api/auth/login com { email, password }
2. AuthController ? AuthService.LoginAsync()
3. AuthService consulta MongoDB (UserRepository.GetByEmailAsync)
4. BCrypt.Verify() valida a senha
5. TokenService.GenerateToken() emite JWT (HS256, 24h)
6. Resposta: { id, name, email, token }
7. Cliente armazena token em LocalStorage via CustomAuthenticationStateProvider
8. Cada chamada subsequente: AuthorizationMessageHandler injeta "Authorization: Bearer {token}"
9. API valida JWT em cada request protegida
```

### Claims no JWT

| Claim | Valor |
|---|---|
| `ClaimTypes.NameIdentifier` | `user.Id` (ObjectId) |
| `ClaimTypes.Email` | `user.Email` |

---

## 6. Persistência e Banco de Dados

### Estratégia

- **MongoDB** como único banco de dados.
- Cada entidade tem sua própria collection.
- Sem joins — dados são desnormalizados quando necessário.
- `IUnitOfWork` agrega todos os repositórios; serviços da Application recebem apenas `IUnitOfWork`.

### Unit of Work

O `UnitOfWork` instancia repositórios de forma lazy:

```csharp
public IRepository<Transaction> Transactions =>
    _transactionRepository ??= new Repository<Transaction>(_context, "transactions");
```

`SaveChangesAsync()` é no-op — MongoDB confirma automaticamente cada operação.

### Soft Delete

Todos os registros usam `IsDeleted = true` para exclusão lógica. A consulta ao banco traz todos, e a filtragem ocorre na camada de serviço:

```csharp
transactions.Where(t => t.UserId == userId && !t.IsDeleted)
```

> **Atenção:** Em coleções grandes isso pode ser ineficiente. Filtros no MongoDB seriam mais performáticos.

---

## 7. Comunicação entre Camadas

### Web ? API (HTTP)

- `HttpClient` configurado com `BaseAddress` da API Railway.
- `AuthorizationMessageHandler` adiciona o token JWT automaticamente.
- Cada serviço do Web chama endpoints REST específicos.

### API ? Application

- Controllers instanciam serviços via DI (`ITransactionService`, etc.).
- DTOs são usados como contratos de entrada e saída.
- Exceções do Application são capturadas pelo `ExceptionHandlingMiddleware`.

### Application ? Infrastructure

- Serviços recebem `IUnitOfWork` via DI.
- Nunca acessam `MongoContext` diretamente.

### Worker ? Application + Infrastructure

- Workers usam os mesmos serviços da Application.
- Registrados em `ApplicationServicesExtensions.cs`.

---

## 8. Localização e Internacionalização

- Arquivos JSON em `src/MoneyManager.Web/wwwroot/i18n/`.
- `LocalizationService` carrega o JSON e expõe `Get("Secao.Chave")`.
- Idioma atual: **pt-BR** (fixo — campo `FixedCulture`).
- A infraestrutura suporta múltiplos idiomas (design preparado), mas a UI atualmente serve apenas `pt-BR`.
- O `User.PreferredLanguage` é armazenado no banco para uso futuro.

### Padrão de chave

```
"Login.Title"        ? seção Login, chave Title
"Transactions.Table.Date"  ? seção aninhada
```

---

## 9. Infraestrutura e Deploy

### Railway

| Serviço | Projeto | Porta |
|---|---|---|
| API | `MoneyManager.Presentation` | `8080` |
| Worker | `MoneyManager.Worker` | — (sem HTTP) |
| Web | `MoneyManager.Web.Host` | (estático) |

### Riscos Conhecidos

| # | Risco | Impacto |
|---|---|---|
| 1 | URL da API hardcoded em `MoneyManager.Web/Program.cs` | Requer rebuild para mudar ambiente |
| 2 | CORS permissivo (qualquer origin) | Risco de segurança em produção |
| 3 | `SaveChangesAsync()` é no-op | Sem suporte a transações MongoDB multi-documento |
| 4 | Filtro `!IsDeleted` no serviço (não no banco) | Performance degradada em coleções grandes |
| 5 | JWT secret pode cair no appsettings | Gerenciar via variáveis de ambiente em produção |

---

## 10. Padrões e Convenções

### Nomenclatura

| Elemento | Convenção |
|---|---|
| Serviço + Interface | `IFooService` / `FooService` (mesmo arquivo) |
| DTO de entrada | `CreateFooRequestDto` ou `FooRequestDto` |
| DTO de saída | `FooResponseDto` |
| Validador | `CreateFooValidator` |
| Repositório especializado | `FooRepository` |
| Controller | `FoosController` |
| Página Blazor | `Foo.razor` em `Pages/` |
| Componente reutilizável | `FooBar.razor` em `Shared/` ou `Components/` |

### Estrutura de Testes

```
MethodName_Scenario_ExpectedResult

Ex: CreateAsync_WithExpense_ShouldDecreaseBalance
```

### Logging Estruturado

```csharp
_logger.LogInformation("Transaction {TransactionId} created for user {UserId}",
    transaction.Id, userId);
```

- Nunca interpolar strings no log — usar placeholders.
- Níveis: `Debug` para diagnóstico, `Information` para eventos de negócio, `Warning` para situações anômalas recuperáveis, `Error` para falhas.

---

*Última atualização: consulte o histórico Git para a data exata.*
