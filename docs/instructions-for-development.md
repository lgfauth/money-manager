# MoneyManager — Instruções para Desenvolvimento

> Documento de referência técnica para desenvolvimento no monorepo **MoneyManager**.
> Reúne visão geral dos projetos, tecnologias, arquitetura, stack, modelo de
> desenvolvimento, boas práticas e a descrição de como cada aplicação funciona e
> foi construída.
>
> Repositório: `lgfauth/money-manager` · Sistema de gerenciamento financeiro pessoal.

---

## Índice rápido

- [Visão geral e arquitetura](#1-visão-geral)
- [Setup e execução local](PROJECT_SETUP.md)
- [Desenvolvimento backend (.NET)](DOTNET-GUIDE.md)
- [Desenvolvimento frontend (Next.js)](NEXTJS-GUIDE.md)
- [Deploy no Railway](RAILWAY_QUICK_START.md)
- [Fluxo SDD e aprovação por tarefa](SPEC-MONEY.md)
- [Comandos Git](GIT_COMMANDS.md) e [regras para agentes](../AGENTS.md)

## Sumário

- [1. Visão geral](#1-visão-geral)
- [2. Stack e tecnologias](#2-stack-e-tecnologias)
- [3. Arquitetura](#3-arquitetura)
- [4. Aplicações do monorepo](#4-aplicações-do-monorepo)
- [5. Bibliotecas de suporte](#5-bibliotecas-de-suporte)
- [6. Modelo de dados (domínio)](#6-modelo-de-dados-domínio)
- [7. Modelo de desenvolvimento e boas práticas](#7-modelo-de-desenvolvimento-e-boas-práticas)
- [8. Testes](#8-testes)
- [9. Build, execução e deploy](#9-build-execução-e-deploy)
- [10. Integrações externas](#10-integrações-externas)
- [11. Estrutura de pastas](#11-estrutura-de-pastas)

---

## 1. Visão geral

O MoneyManager é um sistema de finanças pessoais organizado como **monorepo** com
múltiplas aplicações independentes que compartilham as mesmas camadas de suporte:

| Aplicação | Tipo | Runtime / Stack | Porta padrão |
|---|---|---|---|
| **API Operacional** | Web API REST | .NET 10 / ASP.NET Core | 5000 |
| **API de Backoffice** | Web API REST (interna) | .NET 10 / ASP.NET Core | 5243 |
| **Worker Operacional** | Background Service | .NET 10 Worker Service | — (sem HTTP) |
| **Frontend Web** | SPA/SSR | Next.js 16 · React 19 · TS | 3000 |
| **Portal de Administração** | SPA/SSR | Next.js 16 · React 19 · TS | 3010 |

As três aplicações .NET (2 APIs + Worker) reutilizam as bibliotecas **Domain**,
**Application**, **Infrastructure** e **Observability**. Os dois frontends são
projetos Next.js independentes, cada um consumindo apenas a sua respectiva API.

> **Nota de versão:** os arquivos `*.csproj` têm como alvo **`net10.0`** (fonte
> de verdade). Alguns pacotes `Microsoft.*` ainda estão na linha 9.0.x —
> considere sempre o `TargetFramework` do `.csproj` como oficial.

---

## 2. Stack e tecnologias

### Backend (.NET)

| Item | Escolha |
|---|---|
| Runtime / linguagem | .NET 10 / C# (recursos estáveis mais recentes) |
| Web framework | ASP.NET Core Web API |
| Banco de dados | MongoDB (driver oficial `MongoDB.Driver` 2.25 — **sem EF Core**) |
| Autenticação | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) |
| Hash de senha | `BCrypt.Net-Next` |
| Validação | **FluentValidation** (validators registrados por assembly scan e invocados explicitamente nos controllers; nunca dentro dos services) |
| Logging | NLog via `ILogger<T>` (`NLog.Web.AspNetCore`) |
| Documentação de API | Swashbuckle (Swagger) — habilitado apenas em `Development` |
| Cache | `Microsoft.Extensions.Caching.Memory` |
| Markdown (backoffice) | `Markdig` |
| Imagens (importação de fatura) | `SkiaSharp` + `SkiaSharp.NativeAssets.Linux.NoDependencies` 3.119 (no `MoneyManager.Infrastructure`; a biblioteca nativa de Linux vem no pacote, sem pacotes de sistema no Docker) |
| Nullable / ImplicitUsings | Habilitados em todos os projetos |

### Frontend (Next.js)

**Web (usuário final)** — Next.js 16.4.3, React 19, TypeScript 5:

| Biblioteca | Uso |
|---|---|
| Next.js App Router + Turbopack | Roteamento, SSR/CSR, build |
| `@tanstack/react-query` | Data fetching, cache e sincronização com a API |
| `zustand` | Estado global de UI (stores) |
| `react-hook-form` + `zod` + `@hookform/resolvers` | Formulários e validação |
| `@base-ui/react`, `@radix-ui/react-slot`, `shadcn`, `cmdk`, `vaul` | Componentes de UI |
| `tailwindcss` 4 + `tailwind-merge` + `class-variance-authority` + `clsx` | Estilização |
| `framer-motion` | Animações |
| `recharts` | Gráficos financeiros |
| `next-intl` | Internacionalização — plugin e provider configurados, mas os textos ainda são escritos direto em pt-BR (sem `useTranslations`) |
| `next-themes` | Tema claro/escuro |
| `nuqs` | Instalado, porém não utilizado — filtros na URL usam `useSearchParams` do `next/navigation` |
| `date-fns`, `currency.js` | Datas e valores monetários |
| `lucide-react` | Ícones |
| `sonner` | Toasts/notificações |
| `react-markdown` | Renderização de conteúdo Markdown |

**Backoffice (administração)** — Next.js 16, React 19, TypeScript 5:
propositalmente minimalista, praticamente sem dependências de UI externas, com
`@uiw/react-md-editor` para edição de documentos legais em Markdown.

---

## 3. Arquitetura

O backend segue **Clean Architecture** em quatro camadas, com dependências
apontando sempre para dentro (Presentation → Application → Domain; Infrastructure
implementa contratos do Domain):

| Camada | Projeto | Responsabilidade |
|---|---|---|
| **Domain** | `MoneyManager.Domain` | Entidades, enums, interfaces. **Zero dependências externas** (exceto `MongoDB.Bson` e `FluentValidation` para tipos base). |
| **Application** | `MoneyManager.Application` | Services, DTOs (Request/Response) e Validators. Orquestra o domínio; sem lógica de infraestrutura. |
| **Infrastructure** | `MoneyManager.Infrastructure` | MongoDB (`MongoContext`), repositórios, JWT, criptografia, IA, gateway de pagamento, fila de controle de workers. |
| **Presentation** | `MoneyManager.Api.Operational` / `MoneyManager.Api.Backoffice` | Controllers REST finos, middlewares, composição do DI (`Program.cs`). |

Regras estruturais:

- **Domain** não referencia nenhuma outra camada e não tem NuGet de infraestrutura.
- **Application** referencia apenas Domain e Observability.
- **Infrastructure** referencia Domain + Application (implementa as interfaces).
- **Infrastructure nunca é referenciada por Application ou Domain** — a ligação
  se dá por interfaces do Domain resolvidas via DI na composição root.
- O **Worker** consome diretamente Application + Infrastructure, sem passar por HTTP.
- **Não usamos CQRS nem MediatR** neste projeto.

### Padrões-chave

- **Repository Pattern** — toda query ao MongoDB passa por um `IRepository<T>`.
- **Unit of Work** (`IUnitOfWork`) — centraliza o acesso aos repositórios, inclusive
  em operações com múltiplas coleções. O `SaveChangesAsync` atual é no-op: as
  operações MongoDB são executadas pelos repositórios e não há transação automática
  entre coleções.
- **Erros por exceção** — os services lançam exceções específicas
  (`KeyNotFoundException`, `InvalidOperationException`, `ConcurrencyException`,
  `PremiumRequiredException`, `BankMcpKeyExpiredException`…) que o
  `ExceptionHandlingMiddleware` converte em status HTTP. Não há Result/Union pattern.
- **Soft Delete** — exclusões de dados do usuário marcam `IsDeleted = true`.
  Exceções intencionais com delete físico: o fluxo LGPD (`AccountDeletionService`)
  e a desvinculação de conexão bancária (`BankConnectionService`), além dos
  arquivos temporários de importação de fatura (`CreditCardInvoiceImportFile`),
  apagados ao fim do processamento e expirados por TTL de 24 horas.
- **User Isolation** — consultas de dados do usuário devem filtrar por `UserId`.
  O `Repository<T>.GetByIdAsync` atual filtra apenas por ID; ao usá-lo, o service
  deve verificar se o registro pertence ao usuário antes de utilizá-lo.
- **Injeção por construtor** — dependências nunca instanciadas com `new()`;
  registradas em `Program.cs` ou em métodos de extensão (`ServiceCollectionExtensions`).
- **Async/await em tudo** — sem `.Result`/`.Wait()`; sem fire-and-forget fora de
  background workers explícitos.

---

## 4. Aplicações do monorepo

### 4.1 API Operacional — `src/APIs/MoneyManager.Api.Operational`

API REST principal consumida pelo Frontend Web. Expõe todas as funcionalidades
financeiras do usuário autenticado. Controllers atuais:

`Auth`, `Users`, `Profile`, `Categories`, `Accounts`, `Transactions`, `Budgets`,
`CreditCards`, `CreditCardTransactions`, `CreditCardInvoiceImports`, `RecurringTransactions`, `Reports`, `Dashboard`,
`FinancialHealth`, `BankConnections`, `Subscriptions`, `Receipts`, `Documents`,
`Onboarding`, `Settings`, `UserReports`, `AccountDeletion`, `Webhooks`, `Admin`.

Destaques:

- **JWT em cookie httpOnly**, com logout que revoga o token (`TokenBlacklistService`).
- **Categorias padrão** (`POST /api/categories/defaults`, `CategoryService.CreateDefaultsAsync`):
  cria as categorias da lista `DefaultCategorySeed` (`Domain/Seeds`) que o usuário ainda não
  tem — mesmo nome e tipo, sem diferenciar acentos e maiúsculas —, reativa as padrão excluídas
  e devolve as categorias ativas. Idempotente; acionada pelo botão "Criar categorias padrão"
  da página de categorias e da revisão da importação de fatura.
- **Análise de comprovantes por IA** (`Receipts`) — envia imagem para a API da
  Anthropic (Claude) e extrai descrição, valor, data e categoria.
- **Importação de fatura de cartão por IA** (`CreditCardInvoiceImports` →
  `CreditCardInvoiceImportService`) — upload multipart (`creditCardId` + `files`) de
  **um PDF** (até 20 MB) ou de **1 a 10 imagens** JPEG/PNG/WebP (até 10 MB cada, 40 MB no
  total), sem misturar tipos, validado por `CreateInvoiceImportValidator` (inclui a
  assinatura dos bytes do tipo declarado). Antes de gravar, cada imagem passa por
  `IImageNormalizationService` (`SkiaImageNormalizationService`, SkiaSharp, opções na
  seção `ImageNormalization`): corrige a orientação EXIF, reduz o lado maior para
  `MaxLongSide` (1568 px), aplica fundo branco e recomprime em JPEG começando em
  `InitialQuality` (85) e baixando `QualityStep` (10) até `MinQuality` (40) para caber em
  5 MB. Imagem corrompida, truncada, com tipo falso, acima de `MaxInputPixels` (60 MP)
  ou que não coube em 5 MB → `400` (`ImageNormalizationException`); biblioteca nativa
  ausente → `503` (`ImageProcessingUnavailableException`). PDFs são gravados intactos.
  Também aceita **uma planilha `.xlsx`** (até 5 MB; tipo do Excel ou `application/octet-stream`
  com extensão `.xlsx`; `.xls`, `.xlsm`, `.csv` e planilhas com senha → `400` orientando
  exportar em `.xlsx`). A planilha é convertida em texto por `ISpreadsheetTextExtractor`
  (`XlsxTextExtractor`, leitor nativo com `System.IO.Compression` + `System.Xml`, sem pacote)
  e só o texto (`SpreadsheetText`, `text/plain`: `[Aba: nome]` e uma linha por linha da
  planilha com o número original e as células separadas por TAB) é gravado — o `.xlsx`
  nunca. Regras do leitor (`InvalidSpreadsheetException` → `400`): assinatura ZIP e
  `[Content_Types].xml` com `spreadsheetml.sheet.main+xml` (OLE/CFB e `macroEnabled`
  recusados); até 200 entradas, 20 MB por parte e 50 MB no total contados na leitura,
  taxa de compressão até 100:1 (avaliada a partir de 1 MB lido); XML com
  `DtdProcessing.Prohibit` e sem resolver; lê só workbook, rels, `sharedStrings`, `styles` e
  abas visíveis (macros, links externos, desenhos, comentários e caches nunca são abertos;
  fórmulas só pelo valor calculado); até 10 abas visíveis, 5.000 linhas não vazias e 50
  colunas, células cortadas em 500 caracteres com alerta `CellTruncated`; datas seriais
  (sistemas 1900, com o falso 29/02/1900, e 1904) viram `yyyy-MM-dd` e números saem em
  cultura invariante. No Worker, o texto gravado volta como `SpreadsheetContent` para a
  **conferência anti-alucinação** do `InvoiceImportReviewService`: o valor de cada item
  precisa estar na linha citada (aba + linha) e, se a linha tiver data, a data também;
  senão o item recebe o alerta bloqueante `NotFoundInSpreadsheet` ("não encontrado na
  planilha"), que não se corrige editando — o item precisa ser desmarcado. O cartão precisa ser do usuário (senão `404`),
  não pode ser sincronizado pelo Open Finance nem ter outro job `Queued`/`Processing`
  (`409`, via `InvoiceImportConflictException`). Grava os arquivos e depois o job
  `Queued` e responde `202`; o processamento é do `InvoiceImportWorker` (seção 4.3).
  Também lista os cartões elegíveis, consulta/lista as importações do usuário (as 20
  mais recentes) e descarta (`AwaitingReview`/`Failed` → `Discarded` com soft delete;
  outros estados → `409`).
  **Confirmação** (`POST /{id}/confirm`, `ConfirmInvoiceImportValidator` +
  `ConfirmAsync`): recebe os titulares escolhidos e os itens a cadastrar já editados;
  só aceita job `AwaitingReview` e troca para `Confirming` com versão otimista (duplo
  clique/outra aba → `409`). Antes de gravar, revalida contra o cadastro **atual**:
  cartão sincronizado → `409`; fechamento/vencimento com diferença acima de 3 dias →
  `409` até ajustar o cartão; mais de um titular sem escolha, item/titular/categoria
  inexistente ou item com alerta bloqueante não corrigido → `400`
  (`InvoiceImportValidationException`, com a lista de problemas) sem criar nada. Itens
  de titulares não escolhidos nunca são criados; itens já importados (fingerprint) são
  pulados. Cada item vira transação pelo `CreditCardTransactionService` com origem
  **`invoice_import`**, que aceita faturas `Closed`/`Overdue`/`Paid`, grava na fatura do
  mês importado (`TargetReferenceMonth`; mês já fechado sem fatura nasce `Closed`) e
  começa o parcelamento em X (`FirstInstallmentNumber`: 1/N → parcelamento completo,
  X/N → só X..N com a numeração original, nada em fatura anterior), gravando
  `InvoiceImportId` em todas as parcelas e `ImportFingerprint` só na do mês importado.
  O valor do documento é o da parcela (total da compra = parcela × N). Falha no meio
  devolve o job a `AwaitingReview`; repetir não duplica. No fim, `Confirmed` com
  contadores (`Result`) e `SelectedHolders` (base da pré-seleção da próxima importação).
  A origem `invoice_import` e seus campos são recusados no cadastro manual
  (`CreateCreditCardTransactionValidator`).
- **Open Finance / conexões bancárias** (`BankConnections`) — sincronização de
  transações via MCP bancário (Pluggy). **Invariante importante** (`BankConnectionService.
  SyncConnectionAsync`): o `ClosingDay`/`BillingDueDay` do cartão é corrigido a
  partir dos dados do banco **antes** de importar as transações do período, e a
  fatura "Aberta" (`CreditCardInvoiceService.UpdateOrCreateOpenInvoiceAsync`) é
  sempre resolvida pelo mês de referência (`ReferenceMonth`) implicado pela data
  de fechamento reportada pelo banco — nunca por "a fatura Aberta mais antiga".
  Inverter essa ordem ou voltar a escolher a fatura por ordenação reintroduz o
  bug de faturas exibidas ao usuário sem nenhuma transação vinculada (transações
  associadas a uma fatura-irmã divergente por terem sido calculadas com um
  `ClosingDay` desatualizado). O upsert de transações sincronizadas
  (`UpsertCreditCardTransactionAsync`) também reavalia e realinha o `InvoiceId`
  de transações já existentes (`RepairInvoiceLinkAsync`) sempre que o `ClosingDay`
  atual do cartão implicar um mês de referência diferente do vinculado — exceto
  transações de origem `invoice_import`, que ficam na fatura escolhida na importação.
  **Conciliação com faturas importadas** (cartão vinculado ao Open Finance depois de
  uma importação): antes de criar uma compra `POSTED` que não existe por `ExternalId`,
  o sync procura no mesmo cartão uma transação de origem **`invoice_import`** sem
  `ExternalId`, com o mesmo valor (o da parcela) e data da compra com até 1 dia de
  diferença. Se encontrar, **adota** essa transação: grava `ExternalId` e
  `OpenBankingCategoryId` (a categoria só é preenchida se estiver vazia) e mantém
  descrição, valor e fatura, sem criar outra. A correspondência é um-para-um dentro do
  sync (desempate: data mais próxima, descrição normalizada igual, parcela mais
  antiga); transações já conciliadas, manuais, de comprovante ou do próprio sync nunca
  são adotadas; compras `PENDING` e compras sem correspondência seguem o fluxo normal.
  **API key do Banco MCP**: fica salva no `User` (`BankMcpApiKey`, criptografada) mesmo
  depois de desconectar todos os bancos. Quando o Banco MCP a rejeita (401), o
  `GET /api/bank-connections/available` marca `BankMcpKeyExpiredAt` e responde
  `apiKeyExpired: true`. A `BankMcpKeyExpiredException` vira **422**, nunca 401,
  porque o frontend desloga ao receber 401. Em Bancos e Contas, o aviso de chave
  expirada só aparece para quem tem banco conectado; sem bancos e com a chave válida,
  a página oferece remover a chave (`DELETE /api/bank-connections/api-key`,
  recusado com 400 enquanto houver conexões) ou abrir a vinculação de contas.
- **Assinaturas / pagamento** (`Subscriptions`) — cobrança via gateway EfiBank.
- **Fluxo LGPD** (`AccountDeletion`) — exclusão completa dos dados do usuário.
- **ExceptionHandlingMiddleware** — erros padronizados no formato `ApiErrorResponse`
  (montado por `ApiErrorResponseFactory`).
- **Relatórios** (`Reports`) — `GET /api/reports/overview` consolida contas e
  cartões no período, em regime de competência (parcelas na data de cada parcela)
  ou de caixa (compras da fatura na data do pagamento), excluindo transferências
  entre contas próprias e pagamentos de fatura para não contar em dobro.
- **Dashboard** (`Dashboard`) — `GET /api/dashboard/summary` devolve a posição atual
  (saldo das contas, dívida em faturas não pagas e patrimônio líquido) e os próximos
  vencimentos (faturas abertas/fechadas/vencidas e recorrências dos próximos 15 dias).
  Os números do mês no frontend vêm de `/api/reports/overview` em competência, os
  mesmos da página de Relatórios.

### 4.2 API de Backoffice — `src/APIs/MoneyManager.Api.Backoffice`

API interna, exclusiva de operadores/administradores; não acessível pelo frontend
do usuário. Controllers: `Auth`, `AdminJobs`, `AdminMaintenance`,
`AdminObservability`, `AdminSubscriptions`, `Documents`.

Destaques:

- **RBAC** com três níveis: `Viewer`, `Operator`, `Admin`.
- **JWT com secret separado** do JWT operacional (`ADMIN_AUTH_SECRET`).
- **Fila de comandos de worker** (`WorkerCommandQueueService`) — enfileira
  `run-now`/`pause`/`resume` para os workers via MongoDB (`AdminJobsController`:
  `ScheduledTransactionWorker`, `CreditCardInvoiceWorker`, `BankSyncWorker`,
  `InvoiceImportWorker`).
- **Audit log** de todas as ações sensíveis de operadores.
- **CORS restrito** às origens de `ADMIN_PORTAL_ALLOWED_ORIGINS`.
- Edição de **documentos legais** (termos de uso e política de privacidade) em
  Markdown, renderizados com `Markdig`.

### 4.3 Worker Operacional — `src/Workers/MoneyManager.Worker.Operational`

Serviço de background (`IHostedService`) para tarefas agendadas; opera direto
sobre Application + Infrastructure. Cada worker tem um par
`*Worker` (agendador) + `*Processor` (lógica) e um `*ScheduleOptions`:

| Worker | Processo | Descrição |
|---|---|---|
| `ScheduledTransactionWorker` | `RecurringTransactions` | Lança transações recorrentes vencidas |
| `CreditCardInvoiceWorker` | `CreditCardInvoiceStatus` | Fecha faturas e atualiza status das invoices |
| `FinancialHealthSnapshotWorker` | `FinancialHealthSnapshot` | Gera snapshots mensais dos baldes de patrimônio (reserva/FIRE) |
| `BankSyncWorker` | `BankSync` | Sincroniza transações das conexões bancárias (Open Finance) |
| `SubscriptionExpirationWorker` | `SubscriptionExpiration` | Expira assinaturas vencidas |
| `InvoiceImportWorker` | `InvoiceImport` | Processa a fila de importações de fatura de cartão por IA (`InvoiceImportProcessor` → `InvoiceImportProcessingService`); verifica a fila a cada 10 s e só registra execução quando há trabalho |

Destaques:

- Schedule configurável por `appsettings.json` (`*ScheduleOptions`).
- **Fila de comandos** — o backoffice pode enfileirar `run-now` para execução
  imediata fora do horário agendado.
- Execuções registradas via `IProcessLogger` na coleção `worker_process_logs`.
- **`ITimeProvider`** (`SystemTimeProvider`) para testes sem depender de `DateTime.Now`.

**Importação de fatura de cartão** (`InvoiceImportWorker`, opções na seção `InvoiceImport`):

- Cada ciclo: recupera jobs presos em `Processing` há mais de `StuckAfterMinutes`
  (10; voltam para `Queued` contando tentativa), expira jobs `Queued` há mais de
  `QueuedExpirationHours` (24) com "Os arquivos da fatura expiraram. Envie a fatura
  novamente." e reserva atomicamente até `MaxJobsPerCycle` jobs
  (`ClaimNextQueuedAsync`).
- Por job: confere o cartão (do usuário, não excluído e **não sincronizado pelo Open
  Finance** — `CreditCardImportEligibilityService`), lê os arquivos (ausentes → falha
  de arquivos expirados, sem chamar a IA), chama `IInvoiceDocumentAnalysisService`
  com limite `JobTimeoutMinutes` (8, sempre menor que o tempo de job preso) e monta a
  revisão com `InvoiceImportReviewService`.
- Revisão (`InvoiceImportReviewService` + `InvoiceImportDuplicateDetector`):
  fatura "aberta"/"parcial" ou com fechamento depois de hoje → `Failed`; sem
  fechamento no documento ele é calculado pelo `ClosingDay` (alerta informativo);
  datas sem ano assumem o ano corrente (ano anterior se caírem depois do fechamento);
  a IA devolve datas na ordem brasileira (`DD/MM/AAAA` ou `DD/MM`) e, se o vencimento
  não cair de 1 a 45 dias após o fechamento, a revisão tenta inverter dia e mês de uma
  ou das duas datas e, achando um par coerente, usa-o com alerta informativo
  `DocumentDateCorrected`; categoria de cada item: a IA escolhe entre as categorias de
  despesa ativas do usuário (bloco `<categories>` após o documento; sem nenhuma, a lista
  padrão de despesas do `DefaultCategorySeed`) e a revisão usa primeiro o histórico — a
  categoria da compra de cartão mais recente do usuário com a mesma descrição comparável
  (`NormalizeDescription`) — e, sem histórico, a sugestão da IA casada por nome; itens com data futura, compra à vista/1ª parcela com mais de 60 dias, valor zero ou
  descrição/data vazia ficam bloqueados; fechamento/vencimento divergente do cadastro
  do cartão gera alerta (até 3 dias informativo, acima bloqueante); titulares
  agrupados por nome normalizado e prefixo, com pré-seleção (última importação
  confirmada do cartão → nome do usuário → titular único); prints sobrepostos
  unificados; fingerprint por item (`ImportFingerprint`) para reimportação; duplicados
  com transações existentes por correspondência um-para-um (valor, data ±1 dia,
  descrição como desempate); parcelas 1/N e X/N classificadas como já cadastradas ou
  "parcelamento anterior não cadastrado".
- Sucesso → `AwaitingReview`; falha de regra ou definitiva da IA → `Failed` com
  mensagem amigável; falha temporária (rede, 429, 5xx, tempo esgotado) → volta para
  `Queued` até `MaxAttempts` (3). Os arquivos são apagados ao fim (sucesso ou falha
  definitiva) e mantidos em falha temporária. Desligamento do host devolve o job à
  fila sem contar tentativa.
- O usuário é avisado pelo Web (indicador no header e toast), que acompanha o status
  do job; o Worker não envia notificação. Logs sem conteúdo do documento, nomes de
  titulares ou base64.

### 4.4 Frontend Web — `src/Frontends/MoneyManager.Web`

Interface principal do usuário final (Next.js 16 / React 19). Organização em
`src/app` (App Router, com grupos `(auth)` e `(dashboard)`, além das páginas
legais), `src/components` (por feature: `transactions`, `accounts`, `budgets`,
`credit-cards`, `financial-health`, `bank-connections`, `bancos-e-contas`,
`receipts`, `recurring`, `reports`, `dashboard`, `charts`, `lgpd`, `ui`, `shared`,
`layout`), `src/hooks`,
`src/lib` (`api-client`, `api-errors`, `validators`, utilitários de privacidade
de valores), `src/stores` (Zustand: `auth-store`, `ui-store`, `settings-store`,
`money-privacy-store`, `breadcrumb-store`), `src/i18n`, `src/config`, `src/types`.

Padrões: React Query para dados do servidor, Zustand para estado de UI,
react-hook-form + zod para formulários, Tailwind 4 para estilo.

### 4.5 Portal de Administração — `src/Frontends/MoneyManager.Backoffice`

Portal leve para operadores (Next.js 16 / React 19). Estrutura em `src/app` com
grupo `(secure)`, `login` e rotas `api` (`login`, `logout` e o proxy
`api/proxy/[...path]`). Páginas: Overview (saúde do sistema), Jobs (status e
disparo manual de workers), Auditoria, Documentos (editor Markdown), Erros e
latência, Manutenção financeira e Assinaturas. Autenticação com JWT de sessão curta
(~15 min) e sem dependências de componentes de UI externos.

---

## 5. Bibliotecas de suporte

### `MoneyManager.Domain`
Camada de domínio sem dependências externas de infraestrutura. Contém
`Entities/`, `Enums/`, `Interfaces/` (contratos de repositório e serviços),
`Exceptions/` e `Seeds/`. As entidades são classes de dados com propriedades
públicas (`{ get; set; }`) mapeadas por atributos `Bson*`; as regras de negócio
ficam nos services da Application.

### `MoneyManager.Application`
Services (um por feature, com a interface no mesmo arquivo), DTOs em
`DTOs/Request` e `DTOs/Response` e Validators FluentValidation. Também abriga
serviços sem dependência de infraestrutura, como
`TokenBlacklistService`, e as interfaces `IPaymentGateway`/`IEncryptionService`.
Expõe `InternalsVisibleTo` para o projeto de testes.

### `MoneyManager.Infrastructure`
Implementações concretas: `Data/` (`MongoContext`, `MongoSettings`, `Migrations/`
com `MigrationRunner` e migrations versionadas), `Repositories/`, `Security/`
(`TokenService`), `Services/` (`AesEncryptionService`, `BankMcpClient`,
`EfiBankPaymentGateway`, `AI/AnthropicReceiptAnalysisService`,
`AI/AnthropicInvoiceDocumentAnalysisService`),
`WorkerControl/` (`WorkerCommandQueueService`) e `Observability/`.

### `MoneyManager.Observability`
Logging estruturado de processos: `IProcessLogger`/`ProcessLogger`, `LogStep`,
`ProcessLogDocument`, `IProcessLogStore` e extensões de DI. Usado pelo Worker e
pelas APIs para persistir logs de execução consultáveis pelo backoffice.

---

## 6. Modelo de dados (domínio)

Entidades principais (`src/Supports/MoneyManager.Domain/Entities`):

| Entidade | Descrição |
|---|---|
| `User` | Usuário do sistema (senha com hash BCrypt) |
| `UserSettings` | Preferências (moeda, formato de data, dia de fechamento do mês, orçamento padrão, tema, notificações) |
| `Account` | Conta financeira (corrente, poupança, dinheiro, investimento) — cartões de crédito são a entidade `CreditCard` |
| `Category` | Categoria de receita ou despesa |
| `Transaction` | Lançamento em conta (receita, despesa, transferência e tipos de investimento) com impacto no saldo |
| `Budget` | Orçamento mensal por categoria |
| `CreditCard` | Cartão de crédito com limite e fechamento |
| `CreditCardInvoice` | Fatura mensal (Pending, Open, Closed, Paid, Overdue) |
| `CreditCardTransaction` | Compra no crédito com parcelamento (campos opcionais `InvoiceImportId` e `ImportFingerprint` quando vem de importação de fatura) |
| `CreditCardInvoiceImport` | Job de importação de fatura fechada de cartão por IA (status, origem, dados da fatura, titulares, alertas e itens extraídos para revisão) — coleção `credit_card_invoice_imports` |
| `CreditCardInvoiceImportFile` | Arquivo normalizado de uma importação (JPEG, PDF ou texto da planilha), apagado ao fim do processamento e com índice TTL de 24 h — coleção `credit_card_invoice_import_files` |
| `RecurringTransaction` | Transação recorrente (diária, semanal, quinzenal, mensal, trimestral, semestral, anual) |
| `Subscription` | Assinatura do usuário (plano/pagamento) |
| `BankConnection` | Conexão Open Finance com instituição bancária |
| `FinancialHealthSettings` | Metas do usuário (perfil, percentuais de aporte, reserva, FIRE) |
| `PatrimonyBucket` | Balde de patrimônio (reserva de emergência ou FIRE) |
| `MonthlySnapshot` | Snapshot mensal do patrimônio |
| `UserReport` | Relato/feedback do usuário |

Enums: `AccountType`, `CategoryType`, `TransactionType`, `TransactionStatus`,
`RecurrenceFrequency`, `InvoiceStatus`, `CreditCardTransactionType`,
`BankConnectionStatus`, `SubscriptionStatus`, `PlanType`, `UserStatus`,
`OnboardingStrategy`, `InvoiceImportStatus`, `InvoiceImportSourceType`,
`InvoiceImportHolderType`, `InvoiceImportCardType`, `InvoiceImportWarningCode`.

---

## 7. Modelo de desenvolvimento e boas práticas

### Nomenclatura
- Classes, métodos, propriedades: **PascalCase** (inglês).
- Variáveis locais e parâmetros: **camelCase** (inglês).
- Comentários e mensagens de log: **português** (apenas onde a lógica é não-óbvia).
- DTOs de request com sufixo `RequestDto` (ex.: `CreateTransactionRequestDto`).
- DTOs de response com sufixo `ResponseDto` (ex.: `TransactionResponseDto`).
- Interfaces com prefixo `I` (ex.: `ITransactionRepository`).

### O que **nunca** fazer
- Lógica de negócio em Controllers.
- Queries diretas ao MongoDB fora dos Repositories.
- Validação dentro dos Services (vai nos Validators).
- Referenciar Infrastructure a partir de Domain ou Application.
- Adicionar pacotes NuGet sem pedido explícito.
- Alterar assinatura pública ou renomear membros públicos sem instrução explícita.
- Deletar arquivos/pastas sem instrução explícita.
- Introduzir CQRS ou MediatR.

### Checklist obrigatório de toda alteração
Definido em **`AGENTS.md`** (raiz), que vale para pessoas e para qualquer IA
(`CLAUDE.md` importa o mesmo arquivo; `.github/copilot-instructions.md` repete o resumo):
1. Rodar e atualizar os testes unitários (`dotnet test MoneyManager.sln`).
2. Atualizar a documentação em `docs/` afetada pela mudança.
3. Registrar a mudança no changelog do frontend
   (`src/Frontends/MoneyManager.Web/public/changelog.md`) com nova versão —
   patch para correções, minor para funcionalidades.
4. Build sem erros, fluxo LGPD e remoção completa de features.

O hook `.githooks/commit-msg` e o workflow `change-guard.yml` verificam o item 3
automaticamente (veja a seção 9).

### Convenções de entrega de código
- Ordem de entrega das camadas: **Domain → Application → Infrastructure → Presentation**.
- Cada arquivo em bloco separado com o caminho completo no topo.
- Garantir que o código compila e passa nos testes antes de entregar.
- Ao alterar uma feature, atualizar os testes existentes e adicionar novos quando necessário.
- **LGPD:** ao adicionar uma feature que persiste dados de usuário no banco,
  incluí-la no fluxo de deleção de dados (`AccountDeletionService`).

> Referência complementar: `AGENTS.md` (fonte destas convenções),
> `.github/copilot-instructions.md` e os guias em `docs/DOTNET-GUIDE.md` e
> `docs/NEXTJS-GUIDE.md`.

---

## 8. Testes

- **Framework:** xUnit · **Mocks:** NSubstitute (`Substitute.For<IInterface>()`).
- Projeto: `tests/MoneyManager.Tests` com subpastas `Domain/`, `Application/`,
  `Presentation/` espelhando as camadas.
- Padrão **Arrange / Act / Assert** com comentários de seção.
- Um `Assert` por teste sempre que possível.
- Cobertura mínima: **caminho feliz + ao menos um caso de erro** por service.
- `InternalsVisibleTo` habilitado em Application para testar membros internos.

Executar: `dotnet test MoneyManager.sln`. O workflow `.github/workflows/dotnet-tests.yml`
roda build e testes (.NET 10) em todo push/PR para `main` e `develop`.

O frontend não tem suíte de testes; a verificação é `npx tsc --noEmit -p .` (feita no CI
pelo `change-guard.yml`) e `npx eslint` nos arquivos alterados.

---

## 9. Build, execução e deploy

### Pré-requisitos
- .NET 10 SDK, Node.js + npm, MongoDB acessível em `mongodb://localhost:27017`
  (ex.: `docker run -d --name mongodb -p 27017:27017 mongo:7`).
- `npm ci` já executado em `src/Frontends/MoneyManager.Web` e `.../MoneyManager.Backoffice`.

### Desenvolvimento local (`dev.sh`)
```bash
./dev.sh operational   # API Operacional + Frontend Web
./dev.sh admin         # API Backoffice + Frontend Backoffice
./dev.sh all           # as 4 aplicações
```

### Docker
Não há `docker-compose.yml` no repositório. Existem Dockerfiles dedicados na raiz
(`Dockerfile.api`, `Dockerfile.admin-api`, `Dockerfile.worker`) e um Dockerfile em
cada frontend (`src/Frontends/MoneyManager.Web/Dockerfile`,
`src/Frontends/MoneyManager.Backoffice/Dockerfile`).

### Verificações automáticas
- **Hook local** — ative uma vez por clone com `git config core.hooksPath .githooks`.
  O `commit-msg` bloqueia commits com código em `src/` sem atualização do changelog
  (exceto com `[skip changelog]` na mensagem) e avisa quando código .NET muda sem
  alteração em `tests/`.
- **CI (GitHub Actions)** — em push/PR para `main`/`develop`:
  `dotnet-tests.yml` (build + testes) e `change-guard.yml` (changelog obrigatório,
  aviso de testes não alterados e type-check do frontend Web).

### Deploy (Railway)
Configurado via arquivos `railway.*.toml` (`railway.api.toml`,
`railway.admin-api.toml`, `railway.worker.toml`, `railway.web.toml`,
`railway.admin.toml`) e scripts `deploy-railway.sh` / `deploy-railway.bat` /
`deploy.ps1` / `deploy.sh`. Ver `RAILWAY.md` e `docs/RAILWAY_QUICK_START.md`.

### Variáveis de ambiente relevantes
- **MongoDB:** `MongoDB__ConnectionString`, `MongoDB__DatabaseName`.
- **JWT operacional:** `Jwt__SecretKey`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__ExpirationHours`.
- **CORS (API Operacional):** `AllowedOrigins` ou `ALLOWED_ORIGIN` (obrigatório;
  aceita várias origens separadas por vírgula).
- **Criptografia:** `Encryption__Key` — idêntica na API e no Worker.
- **Admin:** `ADMIN_AUTH_SECRET`, `ADMIN_USERNAME`, `ADMIN_PASSWORD`, `ADMIN_ROLE`,
  `ADMIN_PORTAL_ALLOWED_ORIGINS` (têm precedência sobre `AdminAuth__*` e
  `AdminCors__AllowedOrigins__*`).
- **Frontends:** `NEXT_PUBLIC_API_URL` (web, embutida no build) /
  `ADMIN_API_URL` e `NEXT_PUBLIC_ADMIN_API_URL` (backoffice).
- **Integrações:** `Anthropic__ApiKey`/`Anthropic__Model` (IA — compartilhadas entre API e Worker), `Efi__*`
  (EfiBank). A API key do Banco MCP (Pluggy) é informada
  por cada usuário e salva criptografada.

---

## 10. Integrações externas

| Integração | Componente | Uso |
|---|---|---|
| **Anthropic (Claude)** | `AnthropicReceiptAnalysisService` | Análise de comprovantes por imagem → JSON estruturado (descrição, valor, data, categoria) |
| **Anthropic (Claude)** | `AnthropicInvoiceDocumentAnalysisService` / `IInvoiceDocumentAnalysisService` | Leitura de fatura de cartão (PDF como bloco `document`, imagens como blocos `image` em ordem, planilha como texto) com resposta por streaming → dados da fatura, lançamentos (data, descrição, valor, compra/estorno, parcela, titular, cartão, origem, categoria, confiança) e pendências. Falhas lançam `InvoiceDocumentAnalysisException` com `IsTransient` (rede, 429, 5xx/529) ou definitiva (chave ausente, recusa, `max_tokens`, JSON inválido). Registrado no Worker |
| **Open Finance (Pluggy via MCP)** | `BankMcpClient` / `IBankMcpClient` | Sincronização de contas e transações bancárias |
| **EfiBank** | `EfiBankPaymentGateway` / `IPaymentGateway` | Gateway de pagamento para assinaturas |
| **AES** | `AesEncryptionService` / `IEncryptionService` | Criptografia de dados sensíveis (ex.: credenciais de conexão bancária) |

---

## 11. Estrutura de pastas

```
src/
├── APIs/
│   ├── MoneyManager.Api.Operational/   ← API principal (usuários)  · :5000
│   └── MoneyManager.Api.Backoffice/    ← API administrativa        · :5243
├── Frontends/
│   ├── MoneyManager.Web/               ← Frontend do usuário (Next.js 16) · :3000
│   └── MoneyManager.Backoffice/        ← Portal admin (Next.js 16)        · :3010
├── Supports/
│   ├── MoneyManager.Domain/            ← Entidades, enums, interfaces
│   ├── MoneyManager.Application/       ← Services, DTOs, validators
│   ├── MoneyManager.Infrastructure/    ← MongoDB, repos, JWT, IA, gateways
│   └── MoneyManager.Observability/     ← Logging estruturado de processos
└── Workers/
    └── MoneyManager.Worker.Operational/ ← Jobs agendados (IHostedService)

tests/
└── MoneyManager.Tests/                 ← xUnit + NSubstitute (Domain/Application/Presentation)

docs/                                   ← Guias (.NET, Next.js, Railway, setup) e este documento
```

---

*Documento gerado como referência de desenvolvimento. Mantenha-o atualizado ao
introduzir novas aplicações, integrações ou mudanças arquiteturais relevantes.*
