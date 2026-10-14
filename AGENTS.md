# MoneyManager — Instruções para agentes de IA

Este arquivo vale para **qualquer IA** que edite este repositório (Claude Code, Codex, Cursor,
GitHub Copilot, Gemini etc.). Leia-o inteiro antes de alterar código.

> Ao mudar estas regras, atualize também `.github/copilot-instructions.md`, que repete o
> checklist para o GitHub Copilot.

## Projeto

Sistema de gerenciamento financeiro pessoal em monorepo: APIs e Worker em **.NET 10 + MongoDB**,
frontends em **Next.js** (Web e Backoffice). Documentação de referência em `docs/`:

| Documento | Conteúdo |
|---|---|
| `docs/instructions-for-development.md` | Visão geral, arquitetura, aplicações, domínio, integrações, variáveis de ambiente |
| `docs/DOTNET-GUIDE.md` | Como construir features no backend (.NET) e convenções |
| `docs/NEXTJS-GUIDE.md` | Como construir features nos frontends (Next.js) e convenções |
| `docs/PROJECT_SETUP.md` | Como rodar localmente, configuração e endpoints principais |
| `docs/RAILWAY_QUICK_START.md` | Deploy no Railway e variáveis por serviço |
| `docs/GIT_COMMANDS.md` | Regras e comandos de git |

## ✅ Checklist obrigatório — toda alteração de código

Uma alteração só está concluída quando os quatro itens abaixo foram feitos. Ao final, informe ao
usuário o resultado de cada item (ou por que ele não se aplica).

### 1. Testes unitários

- Rode `dotnet test MoneyManager.sln` **depois** das alterações e garanta que tudo passa.
- Para cada comportamento alterado, **atualize os testes existentes** em `tests/MoneyManager.Tests`
  (espelha as camadas: `Domain/`, `Application/`, `Presentation/`).
- Para comportamento novo, **adicione testes**: caminho feliz + ao menos um caso de erro/borda.
- Feature removida → remova os testes correspondentes.
- Nunca apague, comente ou pule (`Skip`) um teste só para a suíte passar. Se um teste quebrou
  porque a regra mudou de propósito, ajuste o teste e explique a mudança.
- Frontend (não há suíte de testes): rode `npx tsc --noEmit -p .` e `npx eslint <arquivos alterados>`
  no projeto Next.js alterado.

### 2. Documentação

Revise `docs/` e ajuste o que a alteração tornou desatualizado. Referência rápida:

| Se a alteração… | Revise |
|---|---|
| Cria/remove controller, endpoint, service, worker ou integração | `docs/instructions-for-development.md` e `docs/PROJECT_SETUP.md` |
| Muda padrão de código, camada, convenção ou exemplo do backend | `docs/DOTNET-GUIDE.md` |
| Muda estrutura de pastas, padrão, lib ou variável do frontend | `docs/NEXTJS-GUIDE.md` |
| Adiciona/renomeia variável de ambiente, Dockerfile ou `railway.*.toml` | `docs/RAILWAY_QUICK_START.md` e `docs/instructions-for-development.md` |
| Cria/remove entidade ou enum | `docs/instructions-for-development.md` (seção de domínio) |

Documentação descreve o que **existe** no código — não deixe exemplos ou nomes que não existem.

### 3. Changelog do frontend (com nova versão)

Arquivo: `src/Frontends/MoneyManager.Web/public/changelog.md` (exibido no modal "O que há de novo").

- **Versão:** o cabeçalho mais recente (`# Versão X.Y.Z-beta`) é a fonte da versão do produto.
  - Correção ou ajuste → incrementa o **patch** (`1.1.3-beta` → `1.1.4-beta`).
  - Funcionalidade nova → incrementa o **minor** e zera o patch (`1.1.4-beta` → `1.2.0-beta`).
  - Se a versão do topo já tem a **data de hoje**, acrescente os itens nela em vez de criar outra
    (suba de patch para minor se agora houver funcionalidade nova).
- **Formato:** siga as versões existentes — nova versão no **topo**, linha
  `*Lançamento: DD de <mês por extenso> de AAAA*`, seções `## ✨ Novidades` e/ou
  `## 🐛 Correções e melhorias`, cada item com `### <emoji> Título` e texto curto, separadores `---`.
- **Linguagem:** português, voltado ao usuário final — explique o efeito, não o código.
- Mudanças puramente internas (refatoração, testes, docs, CI, ferramentas) **não** entram no
  changelog; nesse caso informe isso ao usuário.

### 4. Validação final

- `dotnet build MoneyManager.sln` sem erros.
- Se a feature persiste dados de usuário, inclua-a no fluxo LGPD (`AccountDeletionService`).
- Ao remover uma feature, remova-a em todos os pontos: frontend, backend, worker, testes e docs.

## Convenções essenciais

- **Idioma:** nomes de classes, métodos e variáveis em inglês; comentários, mensagens de log,
  textos de interface e mensagens de commit em português do Brasil.
- **Backend (Clean Architecture):** Domain ← Application ← Infrastructure; APIs e Worker compõem o DI.
  - Controllers finos; regra de negócio nos services (`MoneyManager.Application/Services`,
    interface e implementação no mesmo arquivo).
  - Validação com FluentValidation nos `Validators`, invocada explicitamente no controller —
    nunca dentro dos services.
  - Toda query passa por repositório via `IUnitOfWork`; toda query de dados de usuário filtra por `UserId`.
  - Erros por exceções específicas tratadas no `ExceptionHandlingMiddleware` (não há Result pattern).
  - Soft delete (`IsDeleted = true`) no CRUD; delete físico só no fluxo LGPD e na desvinculação bancária.
  - DTOs com sufixo `RequestDto` / `ResponseDto`; tudo `async/await`, sem `.Result`/`.Wait()`.
  - Sem CQRS, MediatR ou AutoMapper; não adicione pacotes NuGet/npm sem pedido explícito.
- **Frontend Web:** dados do servidor via hooks TanStack Query em `src/hooks` (componentes não
  chamam `apiClient` direto), estado de UI em Zustand, formulários com react-hook-form + zod,
  Tailwind 4, tipos espelhando os DTOs em `src/types`.
- **Testes:** xUnit + NSubstitute, padrão Arrange / Act / Assert.

## O que nunca fazer

- Executar `git commit`, `push`, `pull` ou `fetch` sem pedido explícito de um humano.
- Alterar assinatura pública ou renomear membros públicos sem instrução explícita.
- Apagar arquivos ou pastas sem instrução explícita.
- Encerrar uma tarefa com testes falhando sem avisar claramente o usuário.
