# MoneyManager — Copilot Instructions

> Fonte principal: `AGENTS.md` na raiz do repositório. Este arquivo repete as regras essenciais
> para o GitHub Copilot — mantenha os dois em sincronia.

## Projeto
Sistema de gerenciamento financeiro pessoal (monorepo). Repositório: lgfauth/money-manager.
Documentação de referência em `docs/` (comece por `docs/instructions-for-development.md`).

## Stack
- Backend: .NET 10 / C#, ASP.NET Core Web API (API Operacional, API Backoffice) e Worker
- Banco: MongoDB (driver oficial, sem EF Core)
- Frontends: Next.js 15 (Web) e Next.js 16 (Backoffice), App Router, TypeScript
- Auth: JWT Bearer (cookie httpOnly)
- Validação: FluentValidation (validators invocados nos controllers, nunca nos services)
- Logging: NLog via ILogger<T> + IProcessLogger
- Testes: xUnit + NSubstitute

## ✅ Checklist obrigatório — toda alteração de código
1. **Testes unitários:** rode `dotnet test MoneyManager.sln`; atualize os testes afetados em
   `tests/MoneyManager.Tests` e adicione testes para comportamento novo (caminho feliz + erro).
   Nunca apague ou pule testes para a suíte passar. No frontend, rode `npx tsc --noEmit -p .`.
2. **Documentação:** ajuste em `docs/` tudo que a alteração deixou desatualizado.
3. **Changelog:** registre a mudança visível ao usuário em
   `src/Frontends/MoneyManager.Web/public/changelog.md`, com nova versão no topo
   (patch para correções, minor para funcionalidades; mesmo dia → acrescente na versão do topo).
   Mudanças só internas (refatoração, testes, docs, CI) não entram no changelog.
4. **Validação final:** `dotnet build MoneyManager.sln` sem erros; dados de usuário novos entram no
   fluxo LGPD (`AccountDeletionService`); features removidas saem de todos os pontos.

## Arquitetura — Clean Architecture
- **Domain** (`MoneyManager.Domain`): entidades (classes com propriedades públicas e atributos
  `Bson*`), enums, interfaces de repositório/UoW, exceções, seeds. Depende só de `MongoDB.Bson`
  e `FluentValidation`.
- **Application** (`MoneyManager.Application`): services (interface + implementação no mesmo
  arquivo), DTOs em `DTOs/Request` e `DTOs/Response`, validators. Sem lógica de infraestrutura.
- **Infrastructure** (`MoneyManager.Infrastructure`): MongoContext, migrations, repositórios,
  JWT, criptografia, IA, gateways. Nunca referenciada por Application ou Domain.
- **Presentation** (`MoneyManager.Api.Operational` / `MoneyManager.Api.Backoffice`): controllers
  finos, middlewares (`ExceptionHandlingMiddleware` → `ApiErrorResponse`), composição do DI.
- **Worker** (`MoneyManager.Worker.Operational`): pares `*Worker` + `*Processor`.

## Padrões obrigatórios
- Dependências injetadas via construtor e registradas em `Program.cs` / extensões.
- Erros por exceções específicas (`KeyNotFoundException`, `InvalidOperationException`,
  `ConcurrencyException`…) convertidas pelo middleware — não há Result/Union pattern.
- Toda query passa por repositório (`IUnitOfWork`) e filtra por `UserId`.
- Soft delete (`IsDeleted = true`) no CRUD; delete físico só no fluxo LGPD e na desvinculação bancária.
- Tudo async/await, sem `.Result`/`.Wait()`.
- Nomes em inglês (PascalCase/camelCase); comentários, logs e commits em português.
- DTOs com sufixo `RequestDto` / `ResponseDto`; interfaces com prefixo `I`.
- Frontend Web: dados via hooks TanStack Query em `src/hooks`, estado de UI em Zustand,
  formulários react-hook-form + zod.

## O que NUNCA fazer
- Lógica de negócio em controllers ou queries MongoDB fora dos repositórios
- Validação dentro dos services
- Referenciar Infrastructure em Domain ou Application
- Adicionar pacotes NuGet/npm sem pedido explícito
- Alterar assinatura pública ou renomear membros públicos sem instrução explícita
- Apagar arquivos ou pastas sem instrução explícita
- CQRS, MediatR ou AutoMapper
- Executar commit/push/pull/fetch sem pedido de um humano

## Testes
- xUnit + NSubstitute (`Substitute.For<IInterface>()`), padrão Arrange / Act / Assert.
- Cobrir caminho feliz + ao menos um caso de erro por service.
