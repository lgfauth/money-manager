# ?? Tarefa: Localização dos Arquivos de Investimentos

## ? Status Atual

- ? Arquivo `pt-BR.json` atualizado com seções `Investments` e `InvestmentsDashboard`
- ? Sistema de localização funcionando
- ? Arquivos `.razor` precisam ser atualizados

---

## ?? Arquivos que Precisam ser Atualizados

### 1. **Investments.razor** (Página Principal)
**Localização:** `src\MoneyManager.Web\Pages\Investments.razor`

#### Passo 1: Adicionar injeção de dependência no topo
```razor
@inject ILocalizationService Localization
```

#### Passo 2: Substituir Textos (Lista Completa)

| Linha Aproximada | Texto Atual | Substituir por |
|------------------|-------------|----------------|
| ~15 | `<PageTitle>Meus Investimentos</PageTitle>` | `<PageTitle>@Localization.Get("Investments.PageTitle")</PageTitle>` |
| ~20 | `Meus Investimentos` | `@Localization.Get("Investments.Title")` |
| ~25 | `Relatórios` | `@Localization.Get("Investments.Reports")` |
| ~26 | `Dashboard` | `@Localization.Get("Investments.Dashboard")` |
| ~27 | `Adicionar Ativo` | `@Localization.Get("Investments.AddAsset")` |
| ~35 | `Carregando investimentos...` | `@Localization.Get("Investments.Loading")` |
| ~50 | `Seus ativos são atualizados automaticamente...` | `@Localization.Get("Investments.UpdatePricesInfo")` |
| ~53 | `Última atualização:` | `@Localization.Get("Investments.LastUpdate")` |
| ~57 | `Atualizar Agora` | `@Localization.Get("Investments.UpdateNow")` |
| ~82 | `Tipo de Ativo` | `@Localization.Get("Investments.AssetType")` |
| ~84 | `Todos` | `@Localization.Get("Investments.AllTypes")` |
| ~85 | `Ações` | `@Localization.Get("Investments.Stock")` |
| ~86 | `Renda Fixa` | `@Localization.Get("Investments.FixedIncome")` |
| ~87 | `Fundos Imobiliários` | `@Localization.Get("Investments.RealEstate")` |
| ~88 | `Criptomoedas` | `@Localization.Get("Investments.Crypto")` |
| ~89 | `Fundos de Investimento` | `@Localization.Get("Investments.Fund")` |
| ~90 | `ETFs` | `@Localization.Get("Investments.ETF")` |
| ~91 | `Outros` | `@Localization.Get("Investments.Other")` |
| ~96 | `Ordenar por` | `@Localization.Get("Investments.SortBy")` |
| ~98 | `Nome` | `@Localization.Get("Investments.SortByName")` |
| ~99 | `Tipo` | `@Localization.Get("Investments.SortByType")` |
| ~100 | `Valor (Maior)` | `@Localization.Get("Investments.SortByValueDesc")` |
| ~101 | `Valor (Menor)` | `@Localization.Get("Investments.SortByValueAsc")` |
| ~102 | `Lucro/Prejuízo (Maior)` | `@Localization.Get("Investments.SortByProfitDesc")` |
| ~103 | `Lucro/Prejuízo (Menor)` | `@Localization.Get("Investments.SortByProfitAsc")` |
| ~104 | `Rentabilidade % (Maior)` | `@Localization.Get("Investments.SortByPerformanceDesc")` |
| ~105 | `Rentabilidade % (Menor)` | `@Localization.Get("Investments.SortByPerformanceAsc")` |
| ~110 | `Pesquisar` | `@Localization.Get("Investments.Search")` |
| ~112 | `Nome ou ticker do ativo...` | `@Localization.Get("Investments.SearchPlaceholder")` |
| ~132 | `Nenhum ativo encontrado com os filtros aplicados.` | `@Localization.Get("Investments.NoResults")` |
| ~141 | `Você ainda não possui investimentos` | `@Localization.Get("Investments.NoAssets")` |
| ~142 | `Comece adicionando seu primeiro ativo de investimento` | `@Localization.Get("Investments.NoAssetsDescription")` |
| ~143 | `Adicionar Primeiro Ativo` | `@Localization.Get("Investments.AddFirstAsset")` |
| ~158 | `Adicionar Ativo` ou `Editar Ativo` | `@Localization.Get(isEditing ? "Investments.EditAsset" : "Investments.NewAsset")` |
| ~175 | `Conta de Investimento` | `@Localization.Get("Investments.Account")` |
| ~177 | `-- Selecione uma conta --` | `@Localization.Get("Investments.SelectAccount")` |
| ~190 | `Tipo de Ativo` | `@Localization.Get("Investments.AssetType")` |
| ~192-198 | Todos os tipos de ativo | Usar `@Localization.Get("Investments.Stock")` etc |
| ~204 | `Nome do Ativo` | `@Localization.Get("Investments.Name")` |
| ~210 | `Ex: Itaú Unibanco` | `@Localization.Get("Investments.NamePlaceholder")` |
| ~215 | `Ticker/Código` | `@Localization.Get("Investments.Ticker")` |
| ~221 | `Ex: ITUB4` | `@Localization.Get("Investments.TickerPlaceholder")` |
| ~228 | `Quantidade Inicial` | `@Localization.Get("Investments.InitialQuantity")` |
| ~237 | `Preço Inicial` | `@Localization.Get("Investments.InitialPrice")` |
| ~242 | `Taxas/Corretagem` | `@Localization.Get("Investments.InitialFees")` |
| ~249 | `Total a Investir:` | `@Localization.Get("Investments.TotalToInvest")` |
| ~257 | `Observações` | `@Localization.Get("Investments.Notes")` |
| ~263 | `Adicione notas sobre este ativo...` | `@Localization.Get("Investments.NotesPlaceholder")` |
| ~270 | `Cancelar` | `@Localization.Get("Investments.Cancel")` |
| ~276 | `Salvar Alterações` ou `Adicionar Ativo` | `@Localization.Get(isEditing ? "Investments.Save" : "Investments.Add")` |
| ~292 | `Confirmar Exclusão` | `@Localization.Get("Investments.ConfirmDelete")` |
| ~303 | `Tem certeza que deseja excluir o ativo` | `@Localization.Get("Investments.DeleteQuestion", assetToDelete.Name)` |
| ~307 | `Esta ação não pode ser desfeita...` | `@Localization.Get("Investments.DeleteWarning")` |
| ~313 | `Cancelar` | `@Localization.Get("Investments.Cancel")` |
| ~321 | `Sim, Excluir` | `@Localization.Get("Investments.DeleteConfirm")` |

#### Passo 3: Atualizar mensagens no código C#

No bloco `@code` no final do arquivo, substituir:

```csharp
// Linha ~350
errorMessage = $"Erro ao carregar investimentos: {ex.Message}";
// Substituir por:
errorMessage = Localization.Get("Investments.ErrorLoading", ex.Message);

// Linha ~372
priceUpdateMessage = $"Preços atualizados! {response.Updated} ativo(s)...";
// Substituir por:
priceUpdateMessage = Localization.Get("Investments.UpdateSuccess", response.Updated, response.Skipped);

// Linha ~377
priceUpdateMessage = "Erro ao atualizar preços. Tente novamente.";
// Substituir por:
priceUpdateMessage = Localization.Get("Investments.UpdateError");

// Linha ~398
modalError = "Selecione uma conta de investimento.";
// Substituir por:
modalError = Localization.Get("Investments.ErrorAccountRequired");

// Linha ~403
modalError = "O nome do ativo é obrigatório.";
// Substituir por:
modalError = Localization.Get("Investments.ErrorNameRequired");

// Linha ~416
busyMessage = isEditing ? "Salvando alterações..." : "Criando ativo...";
// Substituir por:
busyMessage = Localization.Get(isEditing ? "Investments.Saving" : "Investments.Creating");

// Linha ~423
modalError = $"Erro: {ex.Message}";
// Substituir por:
modalError = Localization.Get("Investments.ErrorSaving", ex.Message);

// Linha ~446
busyMessage = "Excluindo ativo...";
// Substituir por:
busyMessage = Localization.Get("Investments.Deleting");

// Linha ~453
errorMessage = $"Erro ao excluir ativo: {ex.Message}";
// Substituir por:
errorMessage = Localization.Get("Investments.ErrorDeleting", ex.Message);
```

---

### 2. **InvestmentsDashboard.razor** (Dashboard)
**Localização:** `src\MoneyManager.Web\Pages\InvestmentsDashboard.razor`

#### Passo 1: Adicionar injeção (se não existir)
```razor
@inject ILocalizationService Localization
```

#### Passo 2: Substituir Textos

| Linha Aproximada | Texto Atual | Substituir por |
|------------------|-------------|----------------|
| ~10 | `<PageTitle>Dashboard de Investimentos</PageTitle>` | `<PageTitle>@Localization.Get("InvestmentsDashboard.PageTitle")</PageTitle>` |
| ~17 | `Dashboard de Investimentos` | `@Localization.Get("InvestmentsDashboard.Title")` |
| ~23 | `Relatórios` | `@Localization.Get("InvestmentsDashboard.Reports")` |
| ~26 | `Voltar para Ativos` | `@Localization.Get("InvestmentsDashboard.BackToAssets")` |
| ~38 | `Carregando dashboard...` | `@Localization.Get("InvestmentsDashboard.Loading")` |
| ~66 | `Total Investido` | `@Localization.Get("InvestmentsDashboard.TotalInvested")` |
| ~81 | `Valor Atual` | `@Localization.Get("InvestmentsDashboard.CurrentValue")` |
| ~96 | `Lucro/Prejuízo` | `@Localization.Get("InvestmentsDashboard.ProfitLoss")` |
| ~114 | `Rentabilidade Total` | `@Localization.Get("InvestmentsDashboard.TotalReturn")` |
| ~135 | `Diversificação por Tipo de Ativo` | `@Localization.Get("InvestmentsDashboard.DiversificationByType")` |
| ~148 | `Top 10 Ativos por Valor` | `@Localization.Get("InvestmentsDashboard.Top10Assets")` |
| ~161 | `Evolução Patrimonial` | `@Localization.Get("InvestmentsDashboard.Evolution")` |
| ~165-169 | `1M`, `3M`, `6M`, `1A`, `Tudo` | `@Localization.Get("InvestmentsDashboard.Period1M")` etc |
| ~179 | `Rendimentos Mensais (Últimos 12 meses)` | `@Localization.Get("InvestmentsDashboard.MonthlyYields")` |
| ~193 | `Top 5 Melhores Performers` | `@Localization.Get("InvestmentsDashboard.TopPerformers")` |
| ~221 | `Sem dados disponíveis` | `@Localization.Get("InvestmentsDashboard.NoData")` |
| ~231 | `Top 5 Piores Performers` | `@Localization.Get("InvestmentsDashboard.WorstPerformers")` |
| ~259 | `Sem dados disponíveis` | `@Localization.Get("InvestmentsDashboard.NoData")` |
| ~269 | `Transações Recentes` | `@Localization.Get("InvestmentsDashboard.RecentTransactions")` |
| ~308 | `Sem transações` | `@Localization.Get("InvestmentsDashboard.NoTransactions")` |
| ~320 | `Dashboard Vazio` | `@Localization.Get("InvestmentsDashboard.EmptyTitle")` |
| ~321 | `Adicione investimentos para visualizar análises e gráficos` | `@Localization.Get("InvestmentsDashboard.EmptyDescription")` |
| ~323 | `Adicionar Investimentos` | `@Localization.Get("InvestmentsDashboard.AddInvestments")` |

#### Passo 3: Atualizar funções de tradução no código C#

```csharp
// Substituir a função GetAssetTypeName:
private string GetAssetTypeName(InvestmentAssetType type)
{
    return type switch
    {
        InvestmentAssetType.Stock => Localization.Get("Investments.Stock"),
        InvestmentAssetType.FixedIncome => Localization.Get("Investments.FixedIncome"),
        InvestmentAssetType.RealEstate => Localization.Get("Investments.RealEstate"),
        InvestmentAssetType.Crypto => Localization.Get("Investments.Crypto"),
        InvestmentAssetType.Fund => Localization.Get("Investments.Fund"),
        InvestmentAssetType.ETF => Localization.Get("Investments.ETF"),
        _ => Localization.Get("Investments.Other")
    };
}

// Substituir a função GetTransactionTypeName:
private string GetTransactionTypeName(InvestmentTransactionType type)
{
    return type switch
    {
        InvestmentTransactionType.Buy => Localization.Get("InvestmentsDashboard.TransactionBuy"),
        InvestmentTransactionType.Sell => Localization.Get("InvestmentsDashboard.TransactionSell"),
        InvestmentTransactionType.Dividend => Localization.Get("InvestmentsDashboard.TransactionDividend"),
        InvestmentTransactionType.Interest => Localization.Get("InvestmentsDashboard.TransactionInterest"),
        InvestmentTransactionType.YieldPayment => Localization.Get("InvestmentsDashboard.TransactionYield"),
        InvestmentTransactionType.MarketAdjustment => Localization.Get("InvestmentsDashboard.TransactionAdjustment"),
        InvestmentTransactionType.Fee => Localization.Get("InvestmentsDashboard.TransactionFee"),
        _ => type.ToString()
    };
}

// Atualizar mensagem de erro:
// Linha ~374
errorMessage = $"Erro ao carregar dashboard: {ex.Message}";
// Substituir por:
errorMessage = Localization.Get("InvestmentsDashboard.ErrorLoading", ex.Message);
```

---

### 3. **AssetTypeIcon.razor** (Componente)
**Localização:** `src\MoneyManager.Web\Components\Investment\AssetTypeIcon.razor`

Este componente apenas exibe ícones, não precisa de localização.

---

### 4. **InvestmentAssetCard.razor** (Componente)
**Localização:** `src\MoneyManager.Web\Components\Investment\InvestmentAssetCard.razor`

#### Adicionar injeção:
```razor
@inject ILocalizationService Localization
```

#### Substituir textos dos botões:
- `Comprar` ? `@Localization.Get("Investments.Buy")`
- `Vender` ? `@Localization.Get("Investments.Sell")`
- `Editar` ? `@Localization.Get("Investments.Edit")`
- `Excluir` ? `@Localization.Get("Investments.Delete")`

---

### 5. **InvestmentSummaryCard.razor** (Componente)
**Localização:** `src\MoneyManager.Web\Components\Investment\InvestmentSummaryCard.razor`

#### Adicionar injeção:
```razor
@inject ILocalizationService Localization
```

#### Substituir textos:
- `Total Investido` ? `@Localization.Get("Investments.TotalInvested")`
- `Valor Atual` ? `@Localization.Get("Investments.CurrentValue")`
- `Lucro/Prejuízo` ? `@Localization.Get("Investments.ProfitLoss")`

---

## ?? Como Testar

Após fazer as alterações:

1. **Compile o projeto**:
   ```bash
   dotnet build
   ```

2. **Execute a aplicação**:
   ```bash
   dotnet run --project src/MoneyManager.Web
   ```

3. **Acesse as páginas de investimentos**:
   - `/investments` - Lista de ativos
   - `/investments/dashboard` - Dashboard

4. **Verifique**:
   - ? Todos os textos aparecem corretamente
   - ? Sem caracteres quebrados (?)
   - ? Acentos corretos em português

---

## ?? Dicas Importantes

1. **Sempre use UTF-8**: Ao salvar arquivos, certifique-se de que o encoding está UTF-8
2. **Teste incrementalmente**: Faça alterações em um arquivo por vez
3. **Use Ctrl+F**: Para encontrar rapidamente os textos a substituir
4. **Backup**: Faça backup dos arquivos antes de começar

---

## ? Checklist

- [x] Arquivo `pt-BR.json` atualizado com seções de Investimentos
- [ ] `Investments.razor` localizado
- [ ] `InvestmentsDashboard.razor` localizado
- [ ] `InvestmentAssetCard.razor` localizado
- [ ] `InvestmentSummaryCard.razor` localizado
- [ ] Testes realizados
- [ ] Sem caracteres quebrados

---

**Estimativa de Tempo:** 2-3 horas  
**Dificuldade:** Média  
**Prioridade:** Alta (problema de UX)

---

**Criado por:** GitHub Copilot  
**Data:** 2024  
**Versão:** 1.0
