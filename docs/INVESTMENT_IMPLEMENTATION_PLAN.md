# 📊 Plano de Implementação - Sistema de Investimentos

## Visão Geral

Este documento descreve o plano completo de implementação do módulo de **Gestão de Investimentos** no sistema MoneyManager. O objetivo é permitir que usuários registrem, acompanhem e gerenciem seus investimentos, incluindo rendimentos, perdas e ajustes de valor de mercado.

---

## 🎯 Objetivos do Projeto

- ✅ Gerenciar ativos de investimento (ações, FIIs, renda fixa, criptomoedas, etc.)
- ✅ Registrar operações de compra e venda
- ✅ Lançar rendimentos (dividendos, juros, aluguéis)
- ✅ Ajustar valores de mercado
- ✅ Calcular lucro/prejuízo automaticamente
- ✅ Gerar relatórios para análise e declaração de IR
- ✅ Integrar com APIs externas para cotações (opcional)
- ✅ Automatizar lançamentos de rendimentos recorrentes

---

## 📋 Estrutura de Fases

### **🔷 FASE 1: Fundação - Enums e Modelos Base**

**Objetivo:** Criar as estruturas de dados fundamentais para o sistema de investimentos.

#### Arquivos a Criar

1. **`src/MoneyManager.Domain/Enums/InvestmentAssetType.cs`**
   ```csharp
   - Stock (Ações)
   - FixedIncome (Renda Fixa)
   - RealEstate (Fundos Imobiliários)
   - Crypto (Criptomoedas)
   - Fund (Fundos de Investimento)
   - ETF (ETFs)
   - Other (Outros)
   ```

2. **`src/MoneyManager.Domain/Enums/InvestmentTransactionType.cs`**
   ```csharp
   - Buy (Compra)
   - Sell (Venda)
   - Dividend (Dividendo)
   - Interest (Juros)
   - YieldPayment (Rendimento)
   - MarketAdjustment (Ajuste de Mercado)
   - Fee (Taxa)
   ```

#### Arquivos a Modificar

3. **`src/MoneyManager.Domain/Enums/TransactionType.cs`**
   - Adicionar: `InvestmentYield = 3` (Rendimento de investimento)
   - Adicionar: `InvestmentLoss = 4` (Perda de investimento)
   - Adicionar: `InvestmentBuy = 5` (Compra de ativo)
   - Adicionar: `InvestmentSell = 6` (Venda de ativo)

#### Entregáveis
- ✅ Enums criados e documentados
- ✅ TransactionType estendido com tipos de investimento
- ✅ Código compilando sem erros

#### Critérios de Aceitação
- [ ] Todos os enums estão documentados com XML comments
- [ ] Valores numéricos não conflitam com enums existentes
- [ ] Build do projeto Domain bem-sucedido

---

### **🔷 FASE 2: Entidades de Domínio**

**Objetivo:** Criar as entidades principais que representarão os dados de investimentos.

#### Arquivos a Criar

1. **`src/MoneyManager.Domain/Entities/InvestmentAsset.cs`**
   - Propriedades principais:
     - `Id`, `UserId`, `AccountId`
     - `AssetType`, `Name`, `Ticker`
     - `Quantity`, `AveragePurchasePrice`, `CurrentPrice`
     - `TotalInvested`, `CurrentValue`, `ProfitLoss`
     - `ProfitLossPercentage`
     - `LastPriceUpdate`, `Notes`
     - Timestamps e soft delete
   - Métodos auxiliares:
     - `CalculateCurrentValue()`
     - `CalculateProfitLoss()`
     - `UpdateAveragePriceOnBuy()`
     - `UpdateAveragePriceOnSell()`

2. **`src/MoneyManager.Domain/Entities/InvestmentTransaction.cs`**
   - Propriedades principais:
     - `Id`, `UserId`, `AssetId`, `AccountId`
     - `TransactionType`, `Quantity`, `Price`
     - `TotalAmount`, `Fees`
     - `Date`, `Description`
     - Timestamps

#### Entregáveis
- ✅ Entidade InvestmentAsset com cálculos automáticos
- ✅ Entidade InvestmentTransaction para histórico
- ✅ Atributos MongoDB configurados
- ✅ Validações básicas

#### Critérios de Aceitação
- [ ] Entidades com atributos `[BsonElement]` corretos
- [ ] Soft delete implementado
- [ ] Relacionamentos com Account e User definidos
- [ ] Build do projeto Domain bem-sucedido

---

### **🔷 FASE 3: Camada de Dados - Repositórios**

**Objetivo:** Implementar acesso aos dados via MongoDB.

#### Arquivos a Criar

1. **`src/MoneyManager.Domain/Interfaces/IInvestmentAssetRepository.cs`**
   - Métodos:
     - `GetByUserIdAsync(string userId)`
     - `GetByAccountIdAsync(string accountId)`
     - `GetByTickerAsync(string userId, string ticker)`
     - CRUD básico

2. **`src/MoneyManager.Domain/Interfaces/IInvestmentTransactionRepository.cs`**
   - Métodos:
     - `GetByAssetIdAsync(string assetId)`
     - `GetByUserIdAsync(string userId, DateTime startDate, DateTime endDate)`
     - CRUD básico

3. **`src/MoneyManager.Infrastructure/Repositories/InvestmentAssetRepository.cs`**
   - Implementação concreta usando MongoDB

4. **`src/MoneyManager.Infrastructure/Repositories/InvestmentTransactionRepository.cs`**
   - Implementação concreta usando MongoDB

#### Arquivos a Modificar

5. **`src/MoneyManager.Domain/Interfaces/IUnitOfWork.cs`**
   - Adicionar:
     ```csharp
     IInvestmentAssetRepository InvestmentAssets { get; }
     IInvestmentTransactionRepository InvestmentTransactions { get; }
     ```

6. **`src/MoneyManager.Infrastructure/Data/UnitOfWork.cs`**
   - Implementar as novas propriedades

#### Entregáveis
- ✅ Interfaces de repositório definidas
- ✅ Implementação MongoDB concreta
- ✅ UnitOfWork atualizado
- ✅ Queries otimizadas com índices

#### Critérios de Aceitação
- [ ] Repositórios implementam interface base
- [ ] Queries assíncronas
- [ ] Soft delete respeitado
- [ ] Build dos projetos Domain e Infrastructure bem-sucedido

---

### **🔷 FASE 4: DTOs e Contratos**

**Objetivo:** Criar contratos para comunicação entre camadas.

#### Arquivos a Criar

**Request DTOs:**

1. **`src/MoneyManager.Application/DTOs/Request/CreateInvestmentAssetRequestDto.cs`**
   - Campos: AccountId, AssetType, Name, Ticker, InitialQuantity, InitialPrice, Notes

2. **`src/MoneyManager.Application/DTOs/Request/UpdateInvestmentAssetRequestDto.cs`**
   - Campos: Name, Notes

3. **`src/MoneyManager.Application/DTOs/Request/BuyAssetRequestDto.cs`**
   - Campos: Quantity, Price, Date, Fees, Description

4. **`src/MoneyManager.Application/DTOs/Request/SellAssetRequestDto.cs`**
   - Campos: Quantity, Price, Date, Fees, Description

5. **`src/MoneyManager.Application/DTOs/Request/RecordYieldRequestDto.cs`**
   - Campos: AssetId, Amount, YieldType, Date, Description

6. **`src/MoneyManager.Application/DTOs/Request/AdjustPriceRequestDto.cs`**
   - Campos: NewPrice, Date

**Response DTOs:**

7. **`src/MoneyManager.Application/DTOs/Response/InvestmentAssetResponseDto.cs`**
   - Todos os campos da entidade + dados calculados

8. **`src/MoneyManager.Application/DTOs/Response/InvestmentTransactionResponseDto.cs`**
   - Campos da transação + nome do ativo

9. **`src/MoneyManager.Application/DTOs/Response/InvestmentSummaryResponseDto.cs`**
   - TotalInvested, CurrentValue, TotalProfitLoss, TotalProfitLossPercentage
   - AssetsByType (agrupado)
   - TopPerformers, WorstPerformers

#### Entregáveis
- ✅ DTOs de Request validados
- ✅ DTOs de Response completos
- ✅ DTO de Summary para dashboard
- ✅ Mapeamentos documentados

#### Critérios de Aceitação
- [ ] DTOs com DataAnnotations para validação
- [ ] Nomenclatura consistente
- [ ] Build do projeto Application bem-sucedido

---

### **🔷 FASE 5: Serviços de Aplicação**

**Objetivo:** Implementar lógica de negócio central.

#### Arquivos a Criar

1. **`src/MoneyManager.Application/Services/IInvestmentAssetService.cs`**
   - Métodos:
     - `CreateAsync(userId, request)`
     - `GetAllAsync(userId)`
     - `GetByIdAsync(userId, assetId)`
     - `UpdateAsync(userId, assetId, request)`
     - `DeleteAsync(userId, assetId)`
     - `BuyAsync(userId, assetId, request)` ⭐
     - `SellAsync(userId, assetId, request)` ⭐
     - `AdjustPriceAsync(userId, assetId, request)` ⭐
     - `GetSummaryAsync(userId)` ⭐

2. **`src/MoneyManager.Application/Services/InvestmentAssetService.cs`**
   - Implementação completa
   - Cálculos de preço médio ponderado
   - Validações de negócio
   - Integração com TransactionService para criar transações regulares

3. **`src/MoneyManager.Application/Services/IInvestmentTransactionService.cs`**
   - Métodos:
     - `GetByAssetIdAsync(assetId)`
     - `GetByUserIdAsync(userId, filters)`
     - `RecordYieldAsync(userId, request)` ⭐

4. **`src/MoneyManager.Application/Services/InvestmentTransactionService.cs`**
   - Implementação completa
   - Registro de transações
   - Atualização de saldos de conta

#### Funcionalidades Críticas

**Lógica de Compra (BuyAsync):**
```
1. Validar quantidade e preço
2. Calcular novo preço médio ponderado:
   NovoPreçoMédio = (ValorTotal + (Quantidade * Preço)) / (QtdTotal + Quantidade)
3. Atualizar quantidade total
4. Criar InvestmentTransaction (tipo Buy)
5. Criar Transaction regular (tipo Expense) na conta de investimento
6. Atualizar saldo da conta
```

**Lógica de Venda (SellAsync):**
```
1. Validar se há quantidade suficiente
2. Reduzir quantidade do ativo
3. Calcular lucro/prejuízo da operação:
   Resultado = (PreçoVenda - PreçoMédio) * Quantidade
4. Criar InvestmentTransaction (tipo Sell)
5. Criar Transaction regular (tipo Income) na conta de investimento
6. Atualizar saldo da conta
```

**Lógica de Rendimento (RecordYieldAsync):**
```
1. Validar ativo e valor
2. Criar InvestmentTransaction (tipo Dividend/Interest/YieldPayment)
3. Criar Transaction regular (tipo InvestmentYield) na conta de investimento
4. Atualizar saldo da conta
```

#### Entregáveis
- ✅ Interfaces de serviço definidas
- ✅ Implementação completa com lógica de negócio
- ✅ Cálculos financeiros testados
- ✅ Integração com sistema de transações existente

#### Critérios de Aceitação
- [ ] Todas as operações são transacionais (UnitOfWork)
- [ ] Validações de negócio implementadas
- [ ] Exceções customizadas para erros de negócio
- [ ] Logs adequados
- [ ] Build do projeto Application bem-sucedido

---

### **🔷 FASE 6: API Controllers**

**Objetivo:** Expor endpoints REST para o frontend.

#### Arquivos a Criar

1. **`src/MoneyManager.API/Controllers/InvestmentAssetsController.cs`**

**Endpoints:**

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/investment-assets` | Listar todos os ativos do usuário |
| GET | `/api/investment-assets/{id}` | Detalhes de um ativo específico |
| POST | `/api/investment-assets` | Criar novo ativo |
| PUT | `/api/investment-assets/{id}` | Atualizar informações do ativo |
| DELETE | `/api/investment-assets/{id}` | Deletar ativo (soft delete) |
| POST | `/api/investment-assets/{id}/buy` | Registrar compra de mais unidades |
| POST | `/api/investment-assets/{id}/sell` | Registrar venda de unidades |
| POST | `/api/investment-assets/{id}/adjust-price` | Ajustar preço de mercado |
| GET | `/api/investment-assets/summary` | Resumo consolidado de investimentos |

2. **`src/MoneyManager.API/Controllers/InvestmentTransactionsController.cs`**

**Endpoints:**

| Método | Rota | Descrição |
|--------|------|-----------|
| GET | `/api/investment-transactions` | Histórico de transações (com filtros) |
| GET | `/api/investment-transactions/asset/{assetId}` | Transações de um ativo específico |
| POST | `/api/investment-transactions/yield` | Registrar rendimento |

#### Entregáveis
- ✅ Controllers com autorização `[Authorize]`
- ✅ Validação de entrada com ModelState
- ✅ Tratamento de erros consistente
- ✅ Documentação Swagger/OpenAPI

#### Critérios de Aceitação
- [ ] UserId extraído do token JWT
- [ ] Todos os endpoints retornam status codes adequados
- [ ] Validações de entrada funcionando
- [ ] Swagger documentado com exemplos
- [ ] Build do projeto API bem-sucedido

---

### **🔷 FASE 7: Serviços Web (Blazor Client)**

**Objetivo:** Criar camada de comunicação HTTP no frontend.

#### Arquivos a Criar

1. **`src/MoneyManager.Web/Services/IInvestmentAssetService.cs`**
   - Interface espelhando operações da API

2. **`src/MoneyManager.Web/Services/InvestmentAssetService.cs`**
   ```csharp
   - GetAllAsync()
   - GetByIdAsync(id)
   - CreateAsync(request)
   - UpdateAsync(id, request)
   - DeleteAsync(id)
   - BuyAsync(id, request)
   - SellAsync(id, request)
   - AdjustPriceAsync(id, request)
   - GetSummaryAsync()
   ```

3. **`src/MoneyManager.Web/Services/IInvestmentTransactionService.cs`**
   - Interface para transações de investimento

4. **`src/MoneyManager.Web/Services/InvestmentTransactionService.cs`**
   ```csharp
   - GetAllAsync(filters)
   - GetByAssetIdAsync(assetId)
   - RecordYieldAsync(request)
   ```

#### Arquivos a Modificar

5. **`src/MoneyManager.Web/Program.cs`**
   - Registrar serviços de investimento no DI:
   ```csharp
   builder.Services.AddScoped<IInvestmentAssetService, InvestmentAssetService>();
   builder.Services.AddScoped<IInvestmentTransactionService, InvestmentTransactionService>();
   ```

#### Entregáveis
- ✅ HttpClient configurado com base URL
- ✅ Métodos assíncronos para todas as operações
- ✅ Tratamento de erros HTTP
- ✅ Serialização/deserialização JSON

#### Critérios de Aceitação
- [ ] Serviços registrados no DI
- [ ] Tratamento de exceções HTTP
- [ ] Timeout configurado
- [ ] Build do projeto Web bem-sucedido

---

### **🔷 FASE 8: Componentes Blazor Reutilizáveis**

**Objetivo:** Criar componentes UI compartilhados.

#### Arquivos a Criar

1. **`src/MoneyManager.Web/Components/Investment/InvestmentAssetCard.razor`**
   - Parâmetros: `Asset`, `OnEdit`, `OnDelete`, `OnBuy`, `OnSell`
   - Exibe: Nome, Tipo, Quantidade, Valor Atual, Lucro/Prejuízo
   - Ícones dinâmicos por tipo de ativo
   - Indicadores visuais de performance (verde/vermelho)

2. **`src/MoneyManager.Web/Components/Investment/InvestmentSummaryCard.razor`**
   - Parâmetros: `Summary`
   - Cards: Total Investido, Valor Atual, Lucro/Prejuízo
   - Indicador de performance geral

3. **`src/MoneyManager.Web/Components/Investment/InvestmentAssetSelector.razor`**
   - Componente de seleção de ativo (dropdown)
   - Parâmetros: `Assets`, `SelectedAssetId`, `OnAssetSelected`
   - Filtro por tipo de ativo

4. **`src/MoneyManager.Web/Components/Investment/AssetTypeIcon.razor`**
   - Parâmetro: `AssetType`
   - Retorna ícone FontAwesome apropriado:
     - Stock: `fa-chart-line`
     - FixedIncome: `fa-piggy-bank`
     - RealEstate: `fa-building`
     - Crypto: `fa-bitcoin`
     - Fund: `fa-briefcase`
     - ETF: `fa-chart-bar`

5. **`src/MoneyManager.Web/Components/Investment/ProfitLossIndicator.razor`**
   - Parâmetros: `Value`, `Percentage`
   - Exibe valor e percentual com cores apropriadas
   - Ícone de seta para cima/baixo

#### Entregáveis
- ✅ Componentes parametrizados e reutilizáveis
- ✅ Responsivos (Bootstrap 5)
- ✅ Acessíveis (ARIA labels)
- ✅ Eventos customizados (EventCallback)

#### Critérios de Aceitação
- [ ] Componentes funcionam isoladamente
- [ ] CSS/classes Bootstrap aplicadas
- [ ] Sem warnings de compilação
- [ ] Build do projeto Web bem-sucedido

---

### **🔷 FASE 9: Página Principal de Investimentos**

**Objetivo:** Criar interface principal de gerenciamento.

#### Arquivos a Criar

1. **`src/MoneyManager.Web/Pages/Investments.razor`**

**Estrutura da Página:**

```
┌─────────────────────────────────────────────────────┐
│  🎯 Meus Investimentos        [+ Adicionar Ativo]   │
├─────────────────────────────────────────────────────┤
│  📊 Cards de Resumo (3 colunas)                     │
│  - Total Investido                                  │
│  - Valor Atual                                      │
│  - Lucro/Prejuízo                                   │
├─────────────────────────────────────────────────────┤
│  🔧 Botões de Ação Rápida                           │
│  [Rendimento] [Ajustar Preço] [Comprar] [Vender]   │
├─────────────────────────────────────────────────────┤
│  📋 Filtros                                         │
│  - Tipo de Ativo: [Todos ▼]                        │
│  - Ordenar: [Lucro/Prejuízo ▼]                     │
│  - Pesquisar: [          ]                         │
├─────────────────────────────────────────────────────┤
│  📁 Lista de Ativos (Grid Cards)                    │
│  ┌─────────┐ ┌─────────┐ ┌─────────┐              │
│  │ Ativo 1 │ │ Ativo 2 │ │ Ativo 3 │              │
│  └─────────┘ └─────────┘ └─────────┘              │
└─────────────────────────────────────────────────────┘
```

**Funcionalidades:**

- ✅ Carregar lista de ativos ao inicializar
- ✅ Exibir cards de resumo com dados agregados
- ✅ Filtrar por tipo de ativo
- ✅ Ordenar por: Nome, Tipo, Valor, Lucro/Prejuízo
- ✅ Pesquisar por nome/ticker
- ✅ Grid responsivo (3 colunas desktop, 1 coluna mobile)
- ✅ Botões de ação rápida abrindo modais
- ✅ Loading states e mensagens de erro
- ✅ Empty state quando não há ativos

**Modais a incluir:**

- ✅ Modal de Adicionar Ativo
- ✅ Modal de Editar Ativo
- ✅ Confirmação de Exclusão

#### Entregáveis
- ✅ Página completa e funcional
- ✅ Integração com serviços
- ✅ UX/UI consistente com resto do app
- ✅ Responsiva

#### Critérios de Aceitação
- [ ] Dados carregam corretamente
- [ ] Filtros funcionam
- [ ] Modais abrem/fecham corretamente
- [ ] Ações (criar/editar/deletar) funcionam
- [ ] Loading states exibidos
- [ ] Erros tratados com mensagens ao usuário

---

### **🔷 FASE 10: Modais de Operações**

**Objetivo:** Criar interfaces para operações específicas de investimento.

#### Arquivos a Criar

1. **`src/MoneyManager.Web/Components/Investment/Modals/BuyAssetModal.razor`**

**Campos:**
- Ativo: [Dropdown de ativos]
- Quantidade: [Input numérico]
- Preço Unitário: [Input monetário]
- Taxa/Corretagem: [Input monetário]
- Data: [Date picker]
- Descrição: [Textarea]

**Cálculos em tempo real:**
- Valor Total = (Quantidade × Preço) + Taxa
- Novo Preço Médio (estimativa)
- Novo Total Investido

2. **`src/MoneyManager.Web/Components/Investment/Modals/SellAssetModal.razor`**

**Campos:**
- Ativo: [Dropdown de ativos]
- Quantidade: [Input numérico] (validar disponível)
- Preço Unitário: [Input monetário]
- Taxa/Corretagem: [Input monetário]
- Data: [Date picker]
- Descrição: [Textarea]

**Cálculos em tempo real:**
- Valor Bruto = Quantidade × Preço
- Valor Líquido = Valor Bruto - Taxa
- Lucro/Prejuízo da Operação = (Preço Venda - Preço Médio) × Quantidade

3. **`src/MoneyManager.Web/Components/Investment/Modals/RecordYieldModal.razor`**

**Campos:**
- Ativo: [Dropdown de ativos]
- Tipo de Rendimento: [Dividendo / Juros / Aluguel]
- Valor Líquido: [Input monetário]
- Data: [Date picker]
- Descrição: [Textarea]

**Informações exibidas:**
- Conta de destino
- Rendimento acumulado no ano

4. **`src/MoneyManager.Web/Components/Investment/Modals/AdjustPriceModal.razor`**

**Campos:**
- Ativo: [Dropdown de ativos] (read-only se passado por parâmetro)
- Preço Atual: [Display do preço antigo]
- Novo Preço: [Input monetário]
- Data de Referência: [Date picker]

**Cálculos em tempo real:**
- Variação Absoluta = Novo Preço - Preço Atual
- Variação Percentual = (Variação / Preço Atual) × 100
- Novo Valor Total do Ativo
- Novo Lucro/Prejuízo

#### Entregáveis
- ✅ 4 modais completos e funcionais
- ✅ Validações de formulário
- ✅ Cálculos automáticos
- ✅ Feedback visual (success/error)
- ✅ Loading durante submissão

#### Critérios de Aceitação
- [ ] Todos os campos validados
- [ ] Cálculos corretos
- [ ] Modais fecham após sucesso
- [ ] Erros de API exibidos ao usuário
- [ ] UX consistente entre modais

---

### **🔷 FASE 11: Dashboard de Investimentos**

**Objetivo:** Criar página analítica com visualizações.

#### Arquivos a Criar

1. **`src/MoneyManager.Web/Pages/InvestmentsDashboard.razor`**

**Seções da Página:**

**A. Cards de Métricas (Topo)**
- Total Investido
- Valor Atual
- Lucro/Prejuízo Total
- Rendimentos no Mês
- Rentabilidade (%)

**B. Gráficos (Grid 2 colunas)**

1. **Gráfico de Pizza - Diversificação por Tipo**
   - Percentual de cada tipo de ativo
   - Cores distintas por tipo
   - Legenda interativa

2. **Gráfico de Barras - Diversificação por Ativo**
   - Top 10 ativos por valor
   - Ordenado do maior para o menor

3. **Gráfico de Linha - Evolução Patrimonial**
   - Valor total ao longo do tempo
   - Filtro por período (1M, 3M, 6M, 1A, Tudo)
   - Linha de tendência

4. **Gráfico de Barras Horizontal - Rendimentos Mensais**
   - Últimos 12 meses
   - Total de rendimentos por mês

**C. Tabelas de Análise**

1. **Top 5 Melhores Performers**
   - Nome, Tipo, Rentabilidade %
   - Badge verde

2. **Top 5 Piores Performers**
   - Nome, Tipo, Rentabilidade %
   - Badge vermelho

3. **Histórico de Transações Recentes**
   - Data, Ativo, Tipo, Quantidade, Valor
   - Últimas 20 transações
   - Link para ver todas

**D. Filtros Globais**
- Período: [Este Mês ▼]
- Conta: [Todas ▼]
- Tipo de Ativo: [Todos ▼]

#### Arquivos a Criar (JavaScript)

2. **`src/MoneyManager.Web/wwwroot/js/investmentCharts.js`**
   - Funções para renderizar gráficos com Chart.js
   - `renderDiversificationByType()`
   - `renderDiversificationByAsset()`
   - `renderEvolutionChart()`
   - `renderMonthlyYields()`

#### Entregáveis
- ✅ Dashboard completo com 4 gráficos
- ✅ Integração com Chart.js
- ✅ Tabelas de análise
- ✅ Filtros funcionais
- ✅ Responsivo

#### Critérios de Aceitação
- [ ] Todos os gráficos renderizam corretamente
- [ ] Dados atualizados em tempo real
- [ ] Filtros aplicados corretamente
- [ ] Performance adequada (< 2s para carregar)
- [ ] Exportação de dados (opcional)

---

### **🔷 FASE 12: Integração com Sistema Existente**

**Objetivo:** Conectar investimentos ao fluxo de transações e contas.

#### Arquivos a Modificar

1. **`src/MoneyManager.Web/Pages/Transactions.razor`**

**Modificações:**
- Adicionar filtro para tipos de transação de investimento
- Ícones específicos para transações de investimento:
  - `InvestmentYield`: 💰 (verde)
  - `InvestmentBuy`: 🛒 (azul)
  - `InvestmentSell`: 💵 (laranja)
- Exibir nome do ativo quando aplicável
- Link para detalhes do ativo

2. **`src/MoneyManager.Web/Pages/Accounts.razor`**

**Modificações:**
- Destacar visualmente contas de investimento
- Card especial com ícone 📊
- Botão "Ver Investimentos" para contas tipo Investment
- Exibir resumo rápido: Qtd de ativos, Lucro/Prejuízo

3. **`src/MoneyManager.Web/Pages/Index.razor` (Dashboard Principal)**

**Modificações:**
- Nova seção "💼 Meus Investimentos"
- Card de resumo com:
  - Total Investido
  - Valor Atual
  - Rentabilidade %
- Botão "Ver Detalhes" → redireciona para /investments
- Gráfico mini de diversificação (opcional)

4. **`src/MoneyManager.Web/Shared/NavMenu.razor`**

**Modificações:**
- Adicionar item de menu "Investimentos" com ícone 📊
- Submenu (opcional):
  - Meus Ativos
  - Dashboard
  - Transações

#### Entregáveis
- ✅ Transações de investimento visíveis em Transactions
- ✅ Contas de investimento destacadas em Accounts
- ✅ Seção de investimentos no dashboard principal
- ✅ Navegação atualizada

#### Critérios de Aceitação
- [ ] Sem quebra de funcionalidades existentes
- [ ] UX consistente
- [ ] Transações de investimento filtráveis
- [ ] Links de navegação funcionam

---

### **🔷 FASE 13: Automação de Rendimentos Recorrentes**

**Objetivo:** Automatizar lançamentos periódicos de rendimentos.

#### Arquivos a Criar

1. **`src/MoneyManager.Worker/Jobs/InvestmentYieldProcessorJob.cs`**

**Funcionalidades:**
- Executar diariamente
- Buscar `RecurringTransactions` do tipo Investment vinculadas a ativos
- Processar rendimentos na data configurada
- Criar `InvestmentTransaction` e `Transaction` regular
- Atualizar `NextOccurrenceDate`
- Enviar notificações (opcional)

**Lógica:**
```csharp
foreach (var recurring in recurringInvestmentTransactions)
{
    if (recurring.NextOccurrenceDate <= DateTime.Today && recurring.IsActive)
    {
        // 1. Criar InvestmentTransaction
        // 2. Criar Transaction regular
        // 3. Atualizar saldo da conta
        // 4. Atualizar NextOccurrenceDate
        // 5. Log de sucesso
    }
}
```

#### Arquivos a Modificar

2. **`src/MoneyManager.Worker/Program.cs`**
   - Registrar `InvestmentYieldProcessorJob` no DI
   - Configurar schedule (diário às 00:00)

3. **`src/MoneyManager.Domain/Entities/RecurringTransaction.cs`**
   - Adicionar campo opcional: `LinkedInvestmentAssetId`

#### Entregáveis
- ✅ Worker Service funcional
- ✅ Logs detalhados
- ✅ Tratamento de erros
- ✅ Notificações (opcional)

#### Critérios de Aceitação
- [ ] Job executa no horário configurado
- [ ] Rendimentos processados corretamente
- [ ] Saldos atualizados
- [ ] Logs auditáveis
- [ ] Idempotência (não processar duplicados)

---

### **🔷 FASE 14: Relatórios e Exportação**

**Objetivo:** Gerar relatórios para análise e declaração de IR.

#### Arquivos a Criar

1. **`src/MoneyManager.Application/Services/IInvestmentReportService.cs`**
   - Métodos:
     - `GenerateSalesReportAsync(userId, year)` → Relatório de vendas para IR
     - `GenerateYieldsReportAsync(userId, year)` → Relatório de rendimentos
     - `GenerateConsolidatedStatementAsync(userId, startDate, endDate)`
     - `ExportToPdfAsync(reportData)`
     - `ExportToExcelAsync(reportData)`

2. **`src/MoneyManager.Application/Services/InvestmentReportService.cs`**
   - Implementação completa
   - Cálculo de custo médio ponderado
   - Agrupamentos por mês/ano
   - Totalizadores

3. **`src/MoneyManager.API/Controllers/InvestmentReportsController.cs`**

**Endpoints:**
- `GET /api/investment-reports/sales/{year}` → JSON
- `GET /api/investment-reports/yields/{year}` → JSON
- `GET /api/investment-reports/consolidated?start={date}&end={date}` → JSON
- `GET /api/investment-reports/sales/{year}/pdf` → PDF
- `GET /api/investment-reports/sales/{year}/excel` → XLSX

4. **`src/MoneyManager.Web/Pages/InvestmentReports.razor`**

**Página de Relatórios:**

**Seções:**
- Seleção de Ano/Período
- Abas:
  - Vendas (para IR)
  - Rendimentos
  - Extrato Consolidado
- Botões de exportação (PDF, Excel)

**Tabela de Vendas:**
| Data | Ativo | Quantidade | Preço Médio | Preço Venda | Lucro/Prejuízo | IR Devido |
|------|-------|------------|-------------|-------------|----------------|-----------|
| ...  | ...   | ...        | ...         | ...         | ...            | ...       |

**Totalizadores:**
- Total Vendido
- Lucro Total
- Prejuízo Total
- IR Total Devido (estimativa)

#### Dependências

5. **Pacotes NuGet:**
   - `iTextSharp` ou `QuestPDF` (para PDF)
   - `EPPlus` ou `ClosedXML` (para Excel)

#### Entregáveis
- ✅ Serviço de relatórios
- ✅ Endpoints de API
- ✅ Página de relatórios no frontend
- ✅ Exportação PDF e Excel funcionando

#### Critérios de Aceitação
- [ ] Cálculos de IR corretos (15% sobre lucro)
- [ ] Relatórios com dados precisos
- [ ] PDFs gerados corretamente
- [ ] Excel com formatação adequada
- [ ] Performance aceitável (< 5s para gerar)

---

### **🔷 FASE 15: Integração com APIs Externas (Opcional)**

**Objetivo:** Atualizar preços de ativos automaticamente.

#### Arquivos a Criar

1. **`src/MoneyManager.Application/Services/IMarketDataService.cs`**
   - Métodos:
     - `GetCurrentPriceAsync(ticker, assetType)`
     - `GetHistoricalPricesAsync(ticker, startDate, endDate)`
     - `GetAssetInfoAsync(ticker)`

2. **`src/MoneyManager.Application/Services/BrapiMarketDataService.cs`**
   - Implementação para API Brapi (https://brapi.dev)
   - Suporte para ações B3 e FIIs
   - Cache de cotações (Redis ou In-Memory)
   - Rate limiting

3. **`src/MoneyManager.Worker/Jobs/PriceUpdateJob.cs`**
   - Executar diariamente em três momentos, as 12 horas, as 15 horas e após fechamento do mercado (18:00)
   - Buscar todos os ativos com ticker
   - Atualizar `CurrentPrice` e `LastPriceUpdate`
   - Recalcular `CurrentValue` e `ProfitLoss`
   - Logs de atualização

#### Arquivos a Modificar

4. **`src/MoneyManager.Web/Pages/Investments.razor`**
   - Botão "Atualizar Preços" (manual)
   - Indicador de última atualização
   - Loading durante atualização

5. **`appsettings.json`**
   ```json
   "MarketData": {
     "Provider": "Brapi",
     "ApiKey": "your-api-key",
     "CacheExpirationMinutes": 60,
     "RateLimitPerMinute": 30
   }
   ```

#### APIs Recomendadas

**Para Ações e FIIs (Brasil):**
- Brapi (https://brapi.dev) - Gratuito, sem necessidade de API key
- Yahoo Finance API

**Para Criptomoedas:**
- CoinGecko API (https://www.coingecko.com/api)
- Binance API

**Para Renda Fixa:**
- Geralmente não disponível via API
- Atualização manual recomendada

#### Entregáveis
- ✅ Serviço de cotações integrado
- ✅ Worker job para atualização automática
- ✅ Cache implementado
- ✅ Atualização manual via UI

#### Critérios de Aceitação
- [ ] Cotações atualizadas diariamente
- [ ] Rate limiting respeitado
- [ ] Tratamento de erros de API
- [ ] Fallback se API estiver indisponível
- [ ] Cache funcionando

---

### **🔷 FASE 16: Testes e Validação**

**Objetivo:** Garantir qualidade e confiabilidade do sistema.

#### Arquivos a Criar

**Testes Unitários:**

1. **`tests/MoneyManager.Tests/Services/InvestmentAssetServiceTests.cs`**
   - Testar cálculos de preço médio ponderado
   - Testar operações de compra/venda
   - Testar validações de negócio
   - Testar cálculos de lucro/prejuízo

2. **`tests/MoneyManager.Tests/Services/InvestmentTransactionServiceTests.cs`**
   - Testar criação de transações
   - Testar atualização de saldos
   - Testar lançamento de rendimentos

3. **`tests/MoneyManager.Tests/Controllers/InvestmentAssetsControllerTests.cs`**
   - Testar autorização
   - Testar validações de entrada
   - Testar respostas HTTP

**Testes de Integração:**

4. **`tests/MoneyManager.Tests/Integration/InvestmentFlowTests.cs`**
   - Fluxo completo: Criar ativo → Comprar → Vender → Rendimento
   - Validar integridade dos dados
   - Validar saldos de contas

**Testes de Cálculos Financeiros:**

5. **`tests/MoneyManager.Tests/Calculations/InvestmentCalculationsTests.cs`**
   - Casos de teste para preço médio
   - Casos de teste para lucro/prejuízo
   - Casos extremos (valores negativos, zero, etc.)

#### Estrutura de Teste Exemplo

```csharp
[Fact]
public async Task BuyAsset_ShouldCalculateAveragePriceCorrectly()
{
    // Arrange
    var asset = new InvestmentAsset
    {
        Quantity = 100,
        AveragePurchasePrice = 10.00m,
        TotalInvested = 1000.00m
    };

    var buyRequest = new BuyAssetRequestDto
    {
        Quantity = 50,
        Price = 12.00m,
        Fees = 5.00m
    };

    // Act
    await _service.BuyAsync(userId, assetId, buyRequest);

    // Assert
    // Novo preço médio = (1000 + 600 + 5) / 150 = 10.70
    Assert.Equal(150, asset.Quantity);
    Assert.Equal(10.70m, asset.AveragePurchasePrice);
    Assert.Equal(1605.00m, asset.TotalInvested);
}
```

#### Cenários de Teste Críticos

**Compra de Ativos:**
- ✅ Compra inicial (ativo vazio)
- ✅ Compras subsequentes (ajuste de preço médio)
- ✅ Compra com taxas
- ✅ Compra com quantidade zero (deve falhar)
- ✅ Compra com preço negativo (deve falhar)

**Venda de Ativos:**
- ✅ Venda parcial
- ✅ Venda total
- ✅ Venda com lucro
- ✅ Venda com prejuízo
- ✅ Venda maior que quantidade disponível (deve falhar)

**Rendimentos:**
- ✅ Rendimento em conta de investimento
- ✅ Rendimento sem ativo (deve falhar)
- ✅ Múltiplos rendimentos no mesmo dia

**Ajuste de Preço:**
- ✅ Ajuste para cima
- ✅ Ajuste para baixo
- ✅ Ajuste para zero (deve falhar ou alertar)

#### Entregáveis
- ✅ Suite de testes completa
- ✅ Cobertura mínima de 80%
- ✅ Testes passando no CI/CD
- ✅ Documentação de casos de teste

#### Critérios de Aceitação
- [ ] Todos os testes passam
- [ ] Cobertura ≥ 80%
- [ ] Sem warnings de compilação
- [ ] Testes executam em < 30s

---

### **🔷 FASE 17: Documentação e Localização**

**Objetivo:** Documentar sistema e internacionalizar.

#### Arquivos a Criar

1. **`docs/INVESTMENTS.md`** (Documentação Técnica)

**Conteúdo:**
- Arquitetura do módulo
- Diagramas de fluxo
- Modelos de dados (ER Diagram)
- Endpoints da API
- Fórmulas de cálculo
- Decisões arquiteturais
- Troubleshooting

2. **`docs/INVESTMENT_USER_GUIDE.md`** (Guia do Usuário)

**Conteúdo:**
- Como adicionar um ativo
- Como registrar compras e vendas
- Como lançar rendimentos
- Como ajustar preços
- Como interpretar relatórios
- FAQs
- Screenshots

3. **`src/MoneyManager.Web/Resources/Localization.pt-BR.json`**

**Strings a adicionar:**
```json
{
  "Investments.Title": "Investimentos",
  "Investments.MyAssets": "Meus Ativos",
  "Investments.TotalInvested": "Total Investido",
  "Investments.CurrentValue": "Valor Atual",
  "Investments.ProfitLoss": "Lucro/Prejuízo",
  "Investments.AddAsset": "Adicionar Ativo",
  "Investments.BuyAsset": "Comprar Ativo",
  "Investments.SellAsset": "Vender Ativo",
  "Investments.RecordYield": "Lançar Rendimento",
  "Investments.AdjustPrice": "Ajustar Preço",
  "Investments.AssetType.Stock": "Ações",
  "Investments.AssetType.FixedIncome": "Renda Fixa",
  "Investments.AssetType.RealEstate": "Fundos Imobiliários",
  "Investments.AssetType.Crypto": "Criptomoedas",
  "Investments.AssetType.Fund": "Fundos de Investimento",
  "Investments.AssetType.ETF": "ETFs",
  "Investments.Quantity": "Quantidade",
  "Investments.AveragePrice": "Preço Médio",
  "Investments.CurrentPrice": "Preço Atual",
  "Investments.Performance": "Rentabilidade",
  // ... mais strings
}
```

4. **`src/MoneyManager.Web/Resources/Localization.en-US.json`**

**Strings em inglês:**
```json
{
  "Investments.Title": "Investments",
  "Investments.MyAssets": "My Assets",
  // ... traduções
}
```

5. **`docs/api/INVESTMENT_API.md`** (Documentação da API)

**Conteúdo:**
- Lista de todos os endpoints
- Exemplos de requisição/resposta
- Códigos de erro
- Autenticação
- Rate limiting

6. **`README_INVESTMENTS.md`** (Raiz do projeto)

**Conteúdo:**
- Overview do módulo
- Features implementadas
- Como começar
- Links para documentação detalhada

#### Diagramas a Criar

7. **Diagrama de Entidades** (PlantUML ou Draw.io)
```
InvestmentAsset 1--* InvestmentTransaction
InvestmentAsset *--1 Account
InvestmentAsset *--1 User
InvestmentTransaction *--1 Transaction
```

8. **Diagrama de Fluxo - Compra de Ativo**
```
[Usuário] -> [UI] -> [API] -> [Service] -> [Repository]
                              |
                              v
                        [Calcular Preço Médio]
                              |
                              v
                        [Criar Transação]
                              |
                              v
                        [Atualizar Saldo]
```

#### Screenshots

9. **`docs/screenshots/investments-*.png`**
   - investments-list.png
   - investments-dashboard.png
   - investments-buy-modal.png
   - investments-sell-modal.png
   - investments-reports.png

#### Entregáveis
- ✅ Documentação técnica completa
- ✅ Guia do usuário ilustrado
- ✅ Strings traduzidas (pt-BR e en-US)
- ✅ API documentada
- ✅ Diagramas criados
- ✅ Screenshots atualizados

#### Critérios de Aceitação
- [ ] Documentação técnica revisada
- [ ] Guia do usuário testado por não-desenvolvedores
- [ ] Todas as strings localizadas
- [ ] Screenshots atualizados
- [ ] Links de documentação funcionam

---

## 📊 Resumo Executivo

### Estatísticas do Projeto

| Métrica | Valor |
|---------|-------|
| Total de Fases | 17 |
| Arquivos Novos | ~60 |
| Arquivos Modificados | ~10 |
| Endpoints de API | 12 |
| Componentes Blazor | 10 |
| Páginas | 4 |
| Testes | 25+ |
| Tempo Estimado | 31-42 horas |

### Priorização de Fases

**🔴 Críticas (MVP):**
- Fases 1-9: Base funcional completa

**🟡 Importantes:**
- Fases 10-14: Features avançadas e relatórios

**🟢 Opcionais:**
- Fases 15-17: Integrações, testes e documentação

### Stack Tecnológica

- **Backend:** .NET 9, MongoDB
- **Frontend:** Blazor WebAssembly
- **Gráficos:** Chart.js
- **PDF:** QuestPDF / iTextSharp
- **Excel:** EPPlus / ClosedXML
- **API Externa:** Brapi (B3)
- **Worker:** .NET BackgroundService

---

## 🎯 Próximos Passos

1. ✅ Revisar e aprovar este plano
2. ✅ Iniciar execução da **FASE 1**
3. ✅ Validar cada fase antes de avançar
4. ✅ Realizar testes contínuos
5. ✅ Documentar decisões e mudanças

---

## 📝 Controle de Versão

| Versão | Data | Alterações |
|--------|------|------------|
| 1.0 | 2026-02-13 | Versão inicial do plano |

---

## 👥 Equipe

- **Product Owner:** Luan Fauth
- **Desenvolvedor:** Cloud Sonet 4.5 - GitHub Copilot
- **Revisor:** Luan Fauth

---

## 📧 Contato

Para dúvidas ou sugestões sobre este plano, entre em contato com a equipe de desenvolvimento.

---

**Última atualização:** 2026-02-13
