# MoneyManager — Copilot Instructions

> Fonte principal: [AGENTS.md](../AGENTS.md). O resumo abaixo preserva as regras essenciais
> para clientes que não carregam esse arquivo; mantenha ambos em sincronia.

## Projeto
Sistema de gerenciamento financeiro pessoal em monorepo. Consulte o
[índice da documentação](../docs/instructions-for-development.md#índice-rápido) e os
manifests dos projetos para versões e detalhes atuais.
Quando o humano invocar `/spec-money`, siga a skill e aguarde aprovação explícita
entre tarefas; veja o [guia SDD](../docs/SPEC-MONEY.md).

## Stack
- Backend: .NET 10 / C#, ASP.NET Core Web API (API Operacional, API Backoffice) e Worker
- Banco: MongoDB (driver oficial, sem EF Core)
- Frontends: Next.js 16 (Web e Backoffice), App Router, TypeScript
- Auth: JWT; o Web usa cookie httpOnly e token de sessão para compatibilidade cross-origin
- Validação: FluentValidation (validators invocados nos controllers, nunca nos services)
- Logging: NLog via ILogger<T> + IProcessLogger
- Testes: xUnit + NSubstitute

## ✅ Checklist obrigatório — toda alteração de código
1. **Testes unitários:** rode `dotnet test MoneyManager.sln`; atualize os testes afetados em
   `tests/MoneyManager.Tests` e adicione testes para comportamento novo (caminho feliz + erro).
   Nunca apague ou pule testes para a suíte passar. No frontend alterado, rode
   `npx tsc --noEmit -p .` e `npx eslint <arquivos alterados>`.
2. **Documentação:** ajuste em `docs/` tudo que a alteração deixou desatualizado.
3. **Changelog:** registre a mudança visível ao usuário em
   `src/Frontends/MoneyManager.Web/public/changelog.md`, com nova versão no topo
   (patch para correções, minor para funcionalidades; mesmo dia → acrescente na versão do topo).
   Mudanças só internas (refatoração, testes, docs, CI) não entram no changelog.
4. **Validação final:** `dotnet build MoneyManager.sln` sem erros; dados de usuário novos entram no
   fluxo LGPD (`AccountDeletionService`); features removidas saem de todos os pontos.

## Arquitetura — Clean Architecture
- Domain contém entidades e contratos; Application contém services, DTOs e validators;
  Infrastructure implementa persistência e integrações; APIs e Worker compõem o DI.
- Infrastructure não é referenciada por Application ou Domain; workers usam pares
  `*Worker` + `*Processor`. Detalhes no [guia .NET](../docs/DOTNET-GUIDE.md).

## Padrões obrigatórios
- Dependências injetadas via construtor e registradas em `Program.cs` / extensões.
- Erros por exceções específicas (`KeyNotFoundException`, `InvalidOperationException`,
  `ConcurrencyException`…) convertidas pelo middleware — não há Result/Union pattern.
- Acesso a dados usa repositórios (`IUnitOfWork`); garanta isolamento por `UserId`.
  Ao buscar por ID, confira a propriedade antes de usar o resultado.
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
