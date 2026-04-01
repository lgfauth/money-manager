# Plano de Correção — Money Manager

> **Objetivo:** Corrigir progressivamente os problemas identificados no projeto sem alterar a arquitetura existente, sem criar novos arquivos fora do padrão atual e sem quebrar funcionalidades que já funcionam.
>
> **Regras do plano:**
> - Cada fase é autônoma e pode ser executada e validada individualmente antes de avançar para a próxima.
> - Nenhuma fase altera a arquitetura de camadas existente (Domain → Application → Infrastructure → Presentation → Web).
> - Nenhum arquivo novo será criado fora do padrão já existente no projeto.
> - Cada problema tem testes de regressão indicados para validar que nada foi quebrado.
> - As fases seguem ordem de risco: segurança → integridade de dados → performance → qualidade → observabilidade → testes → infra.

---

## Índice de Problemas

| # | Problema | Fase | Severidade | Status |
|---|---|---|---|---|
| 01 | Transação distribuída na transferência | 2 | Crítico | ✅ Concluído |
| 02 | Credenciais VAPID no repositório | 1 | Crítico | ✅ Concluído |
| 03 | CORS completamente aberto | 1 | Alto | ✅ Concluído |
| 04 | N+1 no BudgetService | 3 | Alto | ✅ Concluído |
| 05 | Matemática inversa no saldo de cartão | 2 | Alto | ✅ Concluído |
| 06 | Invoice linking não-atômico | 2 | Alto | ✅ Concluído |
| 07 | Sem idempotência na criação de transações | 4 | Alto | |
| 08 | Sem paginação nos endpoints | 3 | Médio | ✅ Concluído |
| 09 | Índices insuficientes no MongoDB | 3 | Médio | ✅ Concluído |
| 10 | Task.Delay(100) como workaround | 3 | Médio | ✅ Concluído |
| 11 | Componentes Razor gigantes | 4 | Médio | |
| 12 | GetUserId() duplicado em todos os controllers | 4 | Médio | |
| 13 | URL da API hardcoded no cliente Blazor | 1 | Médio | ✅ Concluído |
| 14 | Soft delete inconsistente (Budget sem IsDeleted) | 2 | Médio | ✅ Concluído |
| 15 | NLog básico e sem estrutura | 5 | Médio | |
| 16 | Dados sensíveis sendo logados | 5 | Médio | |
| 17 | Verificações de autorização espalhadas | 4 | Médio | |
| 18 | Cobertura de testes insuficiente | 6 | Médio | |
| 19 | Docker usando tags `latest` | 7 | Baixo | |
| 20 | Sem healthcheck no Docker Compose | 7 | Baixo | |
| 21 | Sem estratégia de migração de schema | 7 | Baixo | |
| 22 | Sem suporte a multi-moeda | 8 | Baixo | |
| 23 | Sem regras de cascade delete | 2 | Médio | ✅ Concluído |
| 24 | Sem locking otimista para concorrência | 8 | Baixo | |

---

## Fase 1 — Segurança Imediata

> **Meta:** Eliminar vulnerabilidades que expõem o sistema a ataques externos sem exigir mudança de lógica de negócio.
> **Risco de regressão:** Baixo — apenas configurações, sem alteração de lógica.
> **Validação:** Testes manuais de login, notificações push e requests cross-origin após cada item.

---

### Problema 02 — Credenciais VAPID no Repositório

**Arquivo:** `src/MoneyManager.Presentation/appsettings.json` e `src/MoneyManager.Web/wwwroot/appsettings.json`

**Situação atual:**
As chaves VAPID (pública e privada) estão escritas diretamente nos arquivos de configuração que são versionados no git:

```json
"Vapid": {
    "Subject": "mailto:admin@moneymanager.app",
    "PublicKey": "BNVGoGuOJuMS_V51w8Dw7zvpm77p0D1UgOVXT1-0guIwIuMrQS...",
    "PrivateKey": "U2zgMXu8w9fzPLcp_CAHz388Ek0uYUcKmhoagyMiPZQ"
}
```

**Risco:** Qualquer pessoa com acesso ao repositório possui as chaves privadas de push notification. As chaves podem ser usadas para enviar notificações push em nome da aplicação.

**Correção:**
1. Remover os valores reais das chaves de `appsettings.json` — manter apenas a estrutura com valores vazios ou placeholder.
2. Mover os valores reais para variáveis de ambiente no Railway (já em uso no projeto para outras configurações).
3. Garantir que `appsettings.Production.json` não contenha os valores.
4. Adicionar `appsettings.*.json` ao `.gitignore` se necessário, ou manter arquivos com valores placeholder.

**Padrão já existente no projeto:** O projeto já usa variáveis de ambiente no Railway (`railway.api.toml`). Seguir o mesmo padrão para VAPID.

**Validação pós-correção:**
- Deploy no Railway com as variáveis configuradas → push notification funciona.
- Sem as variáveis → aplicação deve falhar com erro claro ao iniciar (não silenciosamente).

---

### Problema 03 — CORS Completamente Aberto

**Arquivo:** `src/MoneyManager.Presentation/Program.cs` linhas 119–169

**Situação atual:**

```csharp
policy.SetIsOriginAllowed(origin => true)
      .AllowAnyMethod()
      .AllowAnyHeader()
      .AllowCredentials();
```

Há também um middleware manual de CORS nas linhas 139–169 que duplica o comportamento e loga cada requisição:

```csharp
Console.WriteLine($"[CORS] Request: {method} {path} from Origin: {origin ?? "NO ORIGIN"}");
```

**Risco:** Qualquer domínio pode fazer requisições autenticadas para a API. Ataques CSRF e exfiltração de dados ficam triviais.

**Correção:**
1. Substituir `SetIsOriginAllowed(origin => true)` por uma lista explícita de origens permitidas (domínio do Blazor no Railway + `localhost` para desenvolvimento).
2. Remover o middleware manual de CORS das linhas 139–169 — ele é redundante com o middleware nativo do ASP.NET Core.
3. Usar `appsettings.json` para configurar as origens permitidas por ambiente.

**Padrão já existente:** O projeto já usa `appsettings.json` e `appsettings.Production.json` para configurações por ambiente. Adicionar uma chave `AllowedOrigins` nesse arquivo.

**Validação pós-correção:**
- Request do domínio da aplicação → funciona normalmente.
- Request de domínio não autorizado → recebe 403.
- Verificar que os logs de CORS sumam do console.

---

### Problema 13 — URL da API Hardcoded no Cliente Blazor

**Arquivo:** `src/MoneyManager.Web/Program.cs` linha 12

**Situação atual:**

```csharp
var apiUrl = "https://money-manager-api.up.railway.app";
```

**Risco:** Qualquer mudança de URL exige recompilação. Não há separação entre ambientes (dev/prod). Em desenvolvimento local a URL apontará para produção.

**Correção:**
1. Mover a URL para `src/MoneyManager.Web/wwwroot/appsettings.json` (arquivo já existente no projeto).
2. Ler o valor no `Program.cs` via `IConfiguration` da forma como já é feito em outros pontos do projeto.
3. Manter `appsettings.Development.json` com URL local e `appsettings.json` com URL de produção.

**Padrão já existente:** O projeto já tem `wwwroot/appsettings.json` no Web. Adicionar `ApiUrl` nesse arquivo.

**Validação pós-correção:**
- Rodar localmente → aponta para `localhost`.
- Build de produção → aponta para Railway.
- Todas as chamadas de API continuam funcionando.

---

## Fase 2 — Integridade de Dados

> **Meta:** Corrigir problemas que causam corrupção silenciosa de dados financeiros — saldos errados, transações órfãs, estados inconsistentes.
> **Risco de regressão:** Alto — alterar lógica de negócio central. Cada item deve ser coberto por testes antes de ser aplicado.
> **Validação:** Criar casos de teste específicos para cada cenário de falha antes de fazer a correção.

---

### Problema 01 — Transação Distribuída na Transferência

**Arquivo:** `src/MoneyManager.Application/Services/TransactionService.cs` linhas 57–73

**Situação atual:**

```csharp
await _accountService.UpdateBalanceAsync(userId, request.AccountId, -request.Amount);
// Se a linha abaixo falhar, o dinheiro desaparece da conta origem
await _accountService.UpdateBalanceAsync(userId, request.ToAccountId, toImpact);
await _unitOfWork.Transactions.AddAsync(transaction);
await _unitOfWork.SaveChangesAsync();
```

Cada `UpdateBalanceAsync` chama seu próprio `SaveChangesAsync` internamente. Se a segunda chamada falhar, a primeira já foi persistida. O saldo da conta origem diminui e o da destino nunca aumenta.

**Risco:** Corrupção silenciosa de saldo. O usuário perde dinheiro "no ar" sem erro visível.

**Correção:**
1. Retirar as chamadas diretas a `UpdateBalanceAsync` no fluxo de transferência.
2. Calcular os novos saldos das duas contas dentro do próprio `TransactionService`.
3. Usar os repositórios diretamente via `_unitOfWork` para atualizar ambas as contas.
4. Chamar `SaveChangesAsync` uma única vez ao final, após todas as operações.
5. Encapsular em try/catch com rollback explícito caso o save falhe.

**Padrão já existente:** O `UnitOfWork` já centraliza os repositórios. O padrão correto é: todas as operações → um único `SaveChangesAsync`.

**Validação pós-correção:**
- Transferência bem-sucedida: saldo origem diminui, saldo destino aumenta, transação criada.
- Simulação de falha no segundo update: nenhum saldo muda, nenhuma transação criada.
- Teste de integração cobrindo o fluxo completo.

---

### Problema 05 — Matemática Inversa no Saldo de Cartão

**Arquivo:** `src/MoneyManager.Application/Services/TransactionService.cs` linhas 80–110 (aplicar impacto) vs. linhas 370–380 (reverter impacto)

**Situação atual:**
A lógica de aplicar o impacto ao saldo usa uma direção de sinal e a lógica de reverter usa a direção oposta de forma inconsistente para contas do tipo `CreditCard`. Com uso contínuo (criação, edição, exclusão de transações), o saldo do cartão diverge acumulativamente do valor real.

**Risco:** Saldo do cartão de crédito fica incorreto de forma progressiva e silenciosa.

**Correção:**
1. Isolar a lógica de cálculo de impacto em um método privado único: `CalculateBalanceImpact(AccountType, TransactionType, decimal amount)`.
2. Tanto `ApplyTransactionImpact` quanto `RevertTransactionImpact` devem chamar esse método — o revert simplesmente passa o sinal invertido.
3. Nunca duplicar a lógica de cálculo de sinal em dois lugares.

**Padrão já existente:** O projeto já usa serviços privados de apoio dentro de `TransactionService`. Adicionar o método privado ali mesmo.

**Validação pós-correção:**
- Criar transação de despesa no cartão → saldo aumenta (saldo devedor).
- Excluir mesma transação → saldo volta ao original.
- Criar, editar e excluir transações variadas em cartão → saldo permanece consistente com soma das transações.

---

### Problema 06 — Invoice Linking Não-Atômico

**Arquivo:** `src/MoneyManager.Application/Services/TransactionService.cs` linhas 127–151

**Situação atual:**

```csharp
try
{
    var invoice = await _invoiceService.DetermineInvoiceForTransactionAsync(...);
    transaction.InvoiceId = invoice.Id;
    invoice.TotalAmount += request.Amount;
    await _unitOfWork.CreditCardInvoices.UpdateAsync(invoice);
    await _unitOfWork.SaveChangesAsync();
}
catch (Exception ex)
{
    _logger.LogError(...);
    // Continua sem re-throw — transação é criada mesmo com invoice inconsistente
}
```

Se a atualização da fatura falhar, a transação é criada sem `InvoiceId` e a fatura fica com total incorreto. O usuário não recebe erro — a tela mostra sucesso.

**Risco:** Transações de cartão sem fatura associada, totais de fatura incorretos, dados financeiros inconsistentes.

**Correção:**
1. Remover o `try/catch` que engole o erro silenciosamente.
2. Mover a criação da transação e o update da fatura para o mesmo bloco de save (mesma unidade de trabalho).
3. Se a linkagem com fatura falhar, a transação inteira não deve ser criada — o erro deve subir para o controller e retornar 400/500 apropriado.
4. O `ExceptionHandlingMiddleware` existente já captura e formata erros — deixar ele trabalhar.

**Validação pós-correção:**
- Transação em cartão criada com sucesso → fatura tem `InvoiceId` correto e `TotalAmount` atualizado.
- Simulação de falha na fatura → transação não é criada, usuário recebe erro.
- Nenhuma transação órfã no banco (sem `InvoiceId` em transações de cartão).

---

### Problema 14 — Soft Delete Inconsistente

**Arquivo:** `src/MoneyManager.Domain/Entities/Budget.cs`

**Situação atual:**
`Transaction` e `Account` possuem campo `IsDeleted` para exclusão lógica. `Budget` não possui — quando um budget é excluído, é removido permanentemente do banco.

**Risco:** Inconsistência histórica — transações e contas ficam no histórico mas budgets somem, dificultando relatórios retroativos.

**Correção:**
1. Adicionar campo `IsDeleted` e `DeletedAt` na entidade `Budget` em `MoneyManager.Domain`.
2. Atualizar `BudgetService.DeleteAsync` para setar `IsDeleted = true` em vez de remover.
3. Atualizar todos os métodos `GetAll` e `GetById` de budget para filtrar `!b.IsDeleted`.
4. Nenhum arquivo novo — apenas mudança na entidade e no serviço existentes.

**Validação pós-correção:**
- Excluir budget → não aparece mais na listagem mas ainda existe no banco.
- Relatórios históricos do mês em que o budget existia continuam funcionando.
- Budgets novos criados após a exclusão não conflitam com os excluídos.

---

### Problema 23 — Sem Regras de Cascade Delete

**Serviços:** `AccountService`, `CategoryService`

**Situação atual:**
Ao excluir uma conta (soft delete), as transações, faturas e budgets associados a ela ficam no banco referenciando uma conta "deletada". Não há documentado nem implementado qual deve ser o comportamento.

**Risco:** Dados órfãos, relatórios exibindo transações de contas deletadas, inconsistência na UI.

**Correção:**
1. Definir e implementar a política de cada entidade ao excluir uma conta:
   - **Transações:** soft delete em cascata (marcar `IsDeleted = true`).
   - **Faturas de cartão:** soft delete em cascata.
   - **Budgets:** manter — são por categoria, não por conta.
2. Implementar dentro de `AccountService.DeleteAsync` no projeto `MoneyManager.Application`.
3. A lógica de cascade deve acontecer no `Application`, não no banco — padrão já usado no projeto.

**Validação pós-correção:**
- Excluir conta → transações da conta não aparecem mais na listagem.
- Relatórios que filtram por período ainda funcionam (transações soft-deleted são ignoradas).
- Criar nova conta com mesmo nome → funciona normalmente.

---

## Fase 3 — Performance

> **Meta:** Eliminar queries que não escalam e comportamentos de UI que dependem de timers arbitrários.
> **Risco de regressão:** Médio — mudanças em repositórios e componentes. Validar com dados de volume antes de promover.
> **Validação:** Testar com volume simulado (500+ transações, 12+ budgets).

---

### Problema 04 — N+1 no BudgetService

**Arquivo:** `src/MoneyManager.Application/Services/BudgetService.cs` linhas 78–91

**Situação atual:**

```csharp
var transactions = await _unitOfWork.Transactions.GetAllAsync(); // carrega TODAS as transações de TODOS os usuários
var monthTransactions = transactions
    .Where(t => t.UserId == budget.UserId && t.Month == month) // filtra em memória
    .ToList();
```

Esse método é chamado 3 vezes por operação de budget (create, update, get). Com usuários acumulando transações, essa query carrega o banco inteiro na memória.

**Correção:**
1. Adicionar método `GetByUserAndMonthAsync(string userId, string month)` no repositório `IRepository<Transaction>` existente.
2. Implementar o método em `Repository<T>` ou no repositório específico de transações, usando filtro direto no MongoDB.
3. Atualizar `BudgetService.UpdateSpentAmountsAsync` para chamar o novo método filtrado.
4. Nenhum arquivo novo — apenas extensão da interface e implementação existentes.

**Validação pós-correção:**
- Abrir budget → query no MongoDB usa filtro por `userId` e `month` (verificar nos logs).
- Comportamento visual idêntico ao anterior.
- Tempo de resposta não cresce com volume de transações de outros usuários.

---

### Problema 08 — Sem Paginação nos Endpoints

**Arquivos:** `src/MoneyManager.Presentation/Controllers/TransactionsController.cs` e `src/MoneyManager.Web/Pages/Transactions.razor`

**Situação atual:**
`GET /api/transactions` retorna todas as transações do usuário em uma única resposta. Com 1000+ transações, a resposta fica lenta e a UI trava.

**Correção:**
1. Adicionar parâmetros `page` e `pageSize` (com default de 50) no endpoint `GetAll` de `TransactionsController`.
2. Criar DTO de resposta paginada `PagedResultDto<T>` — padrão DTO já existe no `Application`.
3. Atualizar `TransactionService.GetAllAsync` para receber `page` e `pageSize` e passar para o repositório.
4. Implementar `GetPagedAsync` no repositório com `Skip` e `Limit` do MongoDB driver.
5. Atualizar o componente `Transactions.razor` para fazer requisições paginadas e renderizar controles de navegação.
6. **Priorizar transações** — outros endpoints podem aguardar fase subsequente.

**Validação pós-correção:**
- Primeira página carrega rapidamente com 50 transações.
- Navegar para próxima página retorna as 50 seguintes.
- Filtros e ordenação continuam funcionando dentro da paginação.

---

### Problema 09 — Índices Insuficientes no MongoDB

**Arquivo:** `src/MoneyManager.Infrastructure/Data/MongoContext.cs` método `CreateIndexesAsync`

**Situação atual:**
Existe índice em `(UserId, Date)` para transações, mas faltam índices em campos muito consultados:
- `CategoryId` — usado em relatórios e cálculos de budget
- `AccountId` — usado em extratos e exclusão em cascata
- `IsDeleted` — todas as queries filtram por ele mas sem índice

**Correção:**
1. Adicionar os índices ausentes dentro de `CreateIndexesAsync` no `MongoContext.cs` existente.
2. Para `IsDeleted`: criar índice parcial (`{ IsDeleted: false }`) para cobrir apenas documentos ativos.
3. Para `CategoryId`: índice composto `(UserId, CategoryId)`.
4. Para `AccountId`: índice composto `(UserId, AccountId)`.
5. Nenhum arquivo novo — apenas adição de chamadas de criação de índice no método já existente.

**Validação pós-correção:**
- Usar `explain()` no MongoDB para confirmar que as queries de relatório usam os novos índices.
- Nenhuma mudança de comportamento visível — apenas performance.

---

### Problema 10 — Task.Delay(100) Como Workaround

**Arquivos:** `src/MoneyManager.Web/Pages/Index.razor` linha 333, `src/MoneyManager.Web/Pages/Reports.razor` linha 332

**Situação atual:**

```csharp
await Task.Delay(100);
// Renderiza gráficos aqui
```

O delay foi adicionado para esperar o DOM estar pronto antes de renderizar gráficos via JavaScript Interop. É uma solução frágil — pode falhar em máquinas lentas ou não ser suficiente em máquinas rápidas.

**Correção:**
1. Substituir o `Task.Delay` pela sobrescrita do lifecycle `OnAfterRenderAsync(bool firstRender)` do Blazor.
2. Mover a lógica de renderização de gráficos para dentro do bloco `if (firstRender)` do `OnAfterRenderAsync`.
3. Esse lifecycle garante que o DOM está pronto antes de executar o JS Interop — sem timers.
4. Padrão já usado em outros componentes Blazor do projeto.

**Validação pós-correção:**
- Dashboard e Reports carregam gráficos corretamente.
- Não há delay artificial perceptível.
- Funciona em conexões lentas sem quebrar.

---

## Fase 4 — Qualidade de Código

> **Meta:** Reduzir duplicação, aumentar manutenibilidade e corrigir comportamentos inesperados sem alterar funcionalidades.
> **Risco de regressão:** Baixo a médio — refatorações internas. Validar que comportamento externo é idêntico.

---

### Problema 12 — GetUserId() Duplicado em Todos os Controllers

**Arquivos:** `src/MoneyManager.Presentation/Controllers/TransactionsController.cs`, `ReportsController.cs`, `AccountsController.cs`, e demais controllers.

**Situação atual:**
Cada controller implementa o mesmo método privado:

```csharp
private string GetUserId()
{
    return User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
}
```

Se a lógica de extração do userId mudar (ex: troca de claim type), todos os controllers precisam ser atualizados manualmente.

**Correção:**
1. Criar classe `BaseController : ControllerBase` dentro de `src/MoneyManager.Presentation/Controllers/`.
2. Mover `GetUserId()` para o `BaseController`.
3. Fazer todos os controllers existentes herdar de `BaseController` em vez de `ControllerBase`.
4. Remover o método duplicado de cada controller.
5. **Não criar arquivo novo fora do padrão** — `BaseController` fica no mesmo diretório `Controllers/` já existente.

**Validação pós-correção:**
- Todos os endpoints continuam retornando dados do usuário correto.
- Nenhuma mudança de comportamento visível.

---

### Problema 17 — Verificações de Autorização Espalhadas

**Arquivos:** Todos os serviços em `src/MoneyManager.Application/Services/`

**Situação atual:**
Cada método de serviço repete manualmente:

```csharp
if (account == null || account.UserId != userId || account.IsDeleted)
    throw new KeyNotFoundException("Account not found");
```

Se uma verificação for esquecida em um método, é uma falha de segurança (acesso a dados de outro usuário).

**Correção:**
1. Criar método de extensão `EnsureOwnership(string resourceUserId, string requestingUserId, string resourceType)` dentro de `src/MoneyManager.Application/` — em arquivo de extensão já existente ou em `ApplicationExtensions`.
2. Substituir os blocos if/throw espalhados pela chamada ao método de extensão.
3. O método lança `KeyNotFoundException` com mensagem padronizada — comportamento idêntico ao atual.
4. **Não mudar nenhuma regra de negócio** — apenas centralizar o throw.

**Validação pós-correção:**
- Tentativa de acessar recurso de outro usuário → 404 (igual ao atual).
- Nenhum endpoint passa a permitir acesso indevido.
- Nenhum endpoint para a funcionar corretamente.

---

### Problema 07 — Sem Idempotência na Criação de Transações

**Arquivo:** `src/MoneyManager.Application/Services/TransactionService.cs` método `CreateAsync`

**Situação atual:**
Não há verificação de duplicatas. Duplo clique em "Salvar" ou retry automático do browser cria duas transações idênticas.

**Correção:**
1. Adicionar campo `ClientRequestId` (string, opcional) no `CreateTransactionRequestDto`.
2. No `TransactionService.CreateAsync`, se `ClientRequestId` for fornecido, verificar se já existe transação com esse id para o usuário antes de criar.
3. Se já existir, retornar a transação existente (idempotência real) sem criar duplicata.
4. No frontend (`Transactions.razor`), gerar um `Guid` ao abrir o modal de criação e incluí-lo no request.
5. Padrão DTO já existe — apenas adicionar campo opcional.

**Validação pós-correção:**
- Duplo submit com mesmo `ClientRequestId` → retorna a mesma transação, sem duplicata no banco.
- Submit sem `ClientRequestId` → comportamento anterior (compatibilidade retroativa).

---

### Problema 11 — Componentes Razor Gigantes

**Arquivo:** `src/MoneyManager.Web/Pages/Transactions.razor` (873 linhas)

**Situação atual:**
Um único arquivo concentra: listagem, criação, edição, exclusão, lógica de parcelamento, filtros e múltiplos modais.

**Correção:**
Decompor em sub-componentes dentro de `src/MoneyManager.Web/Pages/` ou criar `src/MoneyManager.Web/Components/Transactions/`:

1. Extrair modal de criação/edição → `TransactionFormModal.razor`
2. Extrair modal de confirmação de parcelas → `InstallmentModal.razor`
3. Extrair filtros de listagem → `TransactionFilters.razor`
4. `Transactions.razor` passa a ser apenas o orchestrador dos sub-componentes

**Regra:** Seguir o padrão já existente de `Components/` no projeto Web (`InvoiceCard.razor`, `InvoiceStatusBadge.razor`).

**Validação pós-correção:**
- Comportamento visual idêntico ao anterior.
- Criar, editar, excluir, filtrar transações — tudo funciona igual.
- Lógica de parcelas mantida.

---

## Fase 5 — Logging e Observabilidade

> **Meta:** Adicionar logging estruturado com rastreabilidade de requests sem alterar fluxos de negócio.
> **Risco de regressão:** Baixo — apenas adição de instrumentação.

---

### Problema 16 — Dados Sensíveis Sendo Logados

**Arquivo:** `src/MoneyManager.Presentation/Program.cs` linhas 139–169

**Situação atual:**

```csharp
Console.WriteLine($"[CORS] Request: {method} {path} from Origin: {origin ?? "NO ORIGIN"}");
```

Além de revelar informações de request (potencialmente com tokens em query strings), esse middleware de CORS manual será removido na Fase 1 (Problema 03). Esta correção deve ser feita junto ou logo após.

**Correção:**
1. Remover todos os `Console.WriteLine` do middleware CORS manual ao removê-lo (Fase 1).
2. Auditar os demais `_logger.LogInformation` nos serviços para garantir que não logam valores de `Amount`, dados pessoais ou tokens.
3. Logs de request devem logar apenas: método HTTP, path, status code, userId (não o valor completo do token).

**Validação pós-correção:**
- Nenhum log contém valores de transação, tokens ou dados pessoais.
- Logs de erro continuam existindo com informação suficiente para diagnóstico.

---

### Problema 15 — NLog Básico e Sem Estrutura

**Arquivo:** `src/MoneyManager.Presentation/nlog.config`

**Situação atual:**
- Todos os logs vão para um único arquivo sem separação por nível.
- Formato texto puro — dificulta parsing por ferramentas de observabilidade.
- Sem Correlation ID — impossível rastrear uma requisição do início ao fim.
- Sem rotação de logs configurada.

**Correção:**
1. Adicionar `CorrelationId` como campo nos logs — gerado no `ExceptionHandlingMiddleware` ou em novo middleware de request tracking (seguindo o padrão do middleware existente).
2. Atualizar o layout do NLog para formato JSON estruturado no arquivo de log (manter texto no console para desenvolvimento).
3. Configurar rotação de arquivo por tamanho (ex: `maxArchiveFiles=30`, `archiveAboveSize=10485760`).
4. Separar arquivo de log de erros (`Error` e `Fatal`) do arquivo de log geral.
5. Todas as mudanças dentro do `nlog.config` existente — nenhum arquivo novo.

**Validação pós-correção:**
- Fazer uma requisição → log contém `CorrelationId` consistente em todas as linhas relacionadas.
- Arquivo de log em formato JSON válido.
- Arquivo de log de erros separado e funcional.

---

## Fase 6 — Cobertura de Testes

> **Meta:** Adicionar testes que cubram os cenários de falha dos problemas corrigidos nas fases anteriores e os fluxos críticos não cobertos.
> **Risco de regressão:** Nenhum — apenas adição de testes.

---

### Problema 18 — Cobertura de Testes Insuficiente

**Arquivo:** `tests/MoneyManager.Tests/`

**Situação atual:**
Testes cobrem apenas happy paths de `CreateAsync` (receita, despesa, transferência). Faltam testes para:
- Transferência com falha no segundo update de saldo
- Criação de transação com invoice que falha
- Atualização de transação que muda de fatura (mês diferente)
- Exclusão de transação que reverte saldo
- Budget com cálculo de `SpentAmount` após transações variadas
- Cenários de idempotência (ClientRequestId duplicado)
- Cascade delete ao excluir conta

**Correção:**
Seguir o padrão de testes já existente em `tests/MoneyManager.Tests/Application/Services/`:

1. `TransactionServiceTests.cs` — adicionar cenários de falha nas transferências, reversão de saldo, idempotência.
2. `BudgetServiceTests.cs` — adicionar cenários de cálculo de `SpentAmount` com volume.
3. `AccountServiceTests.cs` — adicionar cenários de cascade delete.
4. `CreditCardInvoiceServiceTests.cs` — adicionar cenários de invoice linking e falha.

**Prioridade de cobertura (por ordem):**
1. Transferências (Problema 01 desta lista)
2. Reversão de saldo de cartão (Problema 05)
3. Invoice linking com falha (Problema 06)
4. Cascade delete de conta (Problema 23)
5. Idempotência de transação (Problema 07)

**Validação pós-correção:**
- `dotnet test` passa 100%.
- Cobertura das classes de serviço críticas acima de 80%.

---

## Fase 7 — Infraestrutura e Deploy

> **Meta:** Estabilizar o ambiente de containers e adicionar mecanismos de saúde dos serviços.
> **Risco de regressão:** Baixo — apenas configurações de infraestrutura.

---

### Problema 19 — Docker Usando Tags `latest`

**Arquivo:** `docker-compose.yml` linhas 5 e 17

**Situação atual:**

```yaml
image: mongo:latest
image: mongo-express:latest
```

Tags `latest` são atualizadas automaticamente pelo Docker Hub. Uma atualização pode introduzir breaking changes sem aviso.

**Correção:**
1. Verificar a versão atual do MongoDB em uso no Railway ou ambiente de produção.
2. Fixar `mongo:7.0` (ou a versão em uso) no `docker-compose.yml`.
3. Fixar `mongo-express:1.0` (ou versão compatível).
4. Documentar no `docker-compose.yml` como atualizar as versões intencionalmente.

**Validação pós-correção:**
- `docker-compose up` sobe corretamente com versões fixas.
- Banco de dados funciona igual ao anterior.

---

### Problema 20 — Sem Healthcheck no Docker Compose

**Arquivo:** `docker-compose.yml`

**Situação atual:**
Nenhum serviço tem `healthcheck` definido. O Docker considera o serviço "pronto" assim que o processo inicia, mas o MongoDB pode ainda estar inicializando quando a API tenta conectar.

**Correção:**
1. Adicionar `healthcheck` no serviço `mongodb`:
   ```yaml
   healthcheck:
     test: ["CMD", "mongosh", "--eval", "db.adminCommand('ping')"]
     interval: 10s
     timeout: 5s
     retries: 5
   ```
2. Adicionar `depends_on: { mongodb: { condition: service_healthy } }` no serviço da API.
3. Nenhum arquivo novo — apenas adição no `docker-compose.yml` existente.

**Validação pós-correção:**
- `docker-compose up` aguarda MongoDB estar pronto antes de iniciar a API.
- Não há erros de conexão nos primeiros segundos.

---

### Problema 21 — Sem Estratégia de Migração de Schema

**Arquivo:** `src/MoneyManager.Infrastructure/Data/MongoContext.cs`

**Situação atual:**
Mudanças de schema (novo campo, índice, rename de coleção) são aplicadas manualmente sem controle de versão. Não é possível saber quais mudanças foram aplicadas em qual ambiente.

**Correção:**
1. Adicionar um método `RunMigrationsAsync` no `MongoContext.cs` existente.
2. Usar uma collection `_migrations` no próprio MongoDB para registrar quais migrações já foram aplicadas.
3. Cada migração é uma classe dentro de `src/MoneyManager.Infrastructure/Data/Migrations/` (diretório a ser criado apenas se necessário — avaliar se cabe dentro de `Data/` com arquivo único).
4. `RunMigrationsAsync` é chamado no startup da aplicação antes de `CreateIndexesAsync`.
5. **Escopo inicial:** apenas a infraestrutura — nenhuma migração de dados ainda nesta fase.

**Validação pós-correção:**
- Primeira execução: collection `_migrations` criada com migração inicial registrada.
- Segunda execução: migração não é re-executada.
- Adicionar nova migração → executa apenas no primeiro deploy após a mudança.

---

## Fase 8 — Evoluções de Negócio

> **Meta:** Funcionalidades ausentes que limitam o uso real do produto. Executar somente após as fases anteriores estarem completas e estáveis.
> **Risco de regressão:** Alto — adições significativas ao modelo de dados.

---

### Problema 22 — Sem Suporte a Multi-Moeda

**Situação atual:**
Não há campo `Currency` nas entidades `Account` e `Transaction`. Relatórios somam valores de todas as contas sem considerar moeda. Um usuário com conta em USD e BRL terá relatórios incorretos.

**Correção:**
1. Adicionar campo `Currency` (string, ex: "BRL", "USD") na entidade `Account` em `MoneyManager.Domain`.
2. Adicionar campo `Currency` na entidade `Transaction`.
3. Atualizar os DTOs de criação de conta e transação para incluir `Currency` (com default "BRL" para retrocompatibilidade).
4. Atualizar `ReportService` para separar totais por moeda ou converter usando taxa de câmbio (fase inicial: apenas separar por moeda, sem conversão).
5. Atualizar a UI para exibir a moeda junto ao valor.

**Sequência de campos a adicionar:**
- `Account.Currency` (default "BRL")
- `Transaction.Currency` (herdado da conta na criação)
- `ReportService`: agrupar por moeda

**Validação pós-correção:**
- Contas existentes sem `Currency` assumem "BRL" — nenhum dado perdido.
- Relatórios exibem totais separados por moeda.
- UI mostra símbolo da moeda junto ao valor.

---

### Problema 24 — Sem Locking Otimista para Concorrência

**Situação atual:**
Duas requisições simultâneas atualizando o saldo de uma mesma conta geram race condition — a segunda sobrescreve a primeira.

**Correção:**
1. Adicionar campo `Version` (int ou timestamp) nas entidades `Account` e `Transaction` em `MoneyManager.Domain`.
2. Atualizar `Repository<T>.UpdateAsync` para incluir o `Version` atual no filtro da query MongoDB.
3. Se o documento foi modificado por outro processo (versão não bate), lançar `ConcurrencyException`.
4. O `ExceptionHandlingMiddleware` existente captura e retorna 409 Conflict.
5. O frontend recarrega os dados e apresenta mensagem ao usuário.

**Validação pós-correção:**
- Requisição com versão correta → atualiza normalmente.
- Requisição com versão desatualizada → recebe 409 Conflict.
- Nenhum dado é sobrescrito silenciosamente.

---

## Checklist de Execução por Fase

```
Fase 1 — Segurança Imediata
[X] #02 — Credenciais VAPID removidas do repositório
[X] #03 — CORS restrito às origens permitidas
[X] #13 — URL da API movida para appsettings

Fase 2 — Integridade de Dados
[X] #01 — Transferência atômica com SaveChangesAsync único
[X] #05 — Matemática de saldo de cartão unificada
[X] #06 — Invoice linking atômico com erro propagado
[X] #14 — Budget com IsDeleted implementado
[X] #23 — Cascade delete em Account implementado

Fase 3 — Performance
[X] #04 — BudgetService com query filtrada no repositório
[X] #08 — Paginação em TransactionsController
[X] #09 — Índices adicionais no MongoContext
[X] #10 — Task.Delay substituído por OnAfterRenderAsync

Fase 4 — Qualidade de Código
[X] #12 — BaseController com GetUserId()
[X] #17 — EnsureOwnership centralizado
[X] #07 — ClientRequestId para idempotência
[X] #11 — Transactions.razor decomposto em sub-componentes

Fase 5 — Logging e Observabilidade
[X] #16 — Console.WriteLines e logs sensíveis removidos
[X] #15 — Logging estruturado com ProcessLogger (JSON por request/processo)

Fase 6 — Testes
[X] #18 — Testes para transferências, invoice, cascade e idempotência

Fase 7 — Infraestrutura
[X] #19 — Docker com versões fixas
[X] #20 — Healthcheck no docker-compose
[X] #21 — Estratégia de migração no MongoContext

Fase 8 — Evoluções de Negócio
[X] #22 — Suporte a multi-moeda
[X] #24 — Locking otimista com campo Version
```

---

> **Última atualização:** 2026-06-13
> **Gerado por:** Análise de código — Money Manager v1.x
> **Status:** Todas as 8 fases concluídas
