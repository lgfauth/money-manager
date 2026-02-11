# ?? PLANO DE CORREÇÃO - Encoding UTF-8 em Todas as Páginas

## ?? PROBLEMA IDENTIFICADO:

As páginas **Dashboard** e **Transactions** têm textos hardcoded com **encoding incorreto** (UTF-8 malformado):

```
? "Transaçµes" ? deve ser "Transações"
? "Períÿodo" ? deve ser "Período"
? "Poupanša" ? deve ser "Poupança"
```

---

## ? SOLUÇÃO EM 2 PASSOS:

### PASSO 1: Corrigir Encoding de Todos os Arquivos .razor

Execute este script PowerShell na raiz do projeto:

```powershell
# Script para corrigir encoding de TODOS os arquivos .razor
$files = Get-ChildItem "src\MoneyManager.Web" -Filter "*.razor" -Recurse

$replacements = @{
    'Ã§' = 'ç'
    'Ã£' = 'ã'
    'Ã©' = 'é'
    'Ã­' = 'í'
    'Ã³' = 'ó'
    'Ãª' = 'ê'
    'Ã¡' = 'á'
    'Ã' = 'Õ'
    'Ã‚' = ''
    'ÃƒÂ§' = 'ç'
    'ÃƒÂ£' = 'ã'
    'ÃƒÂ©' = 'é'
    'ÃƒÂ­' = 'í'
    'ÃƒÂ³' = 'ó'
    'ÃƒÂª' = 'ê'
    'ÃƒÂ¡' = 'á'
    'ÃƒÂ' = 'Õ'
}

foreach ($file in $files) {
    Write-Host "Processando: $($file.Name)" -ForegroundColor Cyan
    
    $content = Get-Content $file.FullName -Raw -Encoding UTF8
    
    foreach ($key in $replacements.Keys) {
        $content = $content -replace $key, $replacements[$key]
    }
    
    $content | Set-Content $file.FullName -Encoding UTF8 -NoNewline
    Write-Host "? Corrigido: $($file.Name)" -ForegroundColor Green
}

Write-Host ""
Write-Host "?? CONCLUÍDO! Todos os arquivos .razor foram corrigidos!" -ForegroundColor Green
```

---

### PASSO 2: Substituir Textos Hardcoded por Localizações

Após corrigir o encoding, substituir os textos por chamadas ao `Localization`:

#### Dashboard (Index.razor):

**Antes:**
```razor
<h6 class="text-muted mb-2">SALDO LÍQUIDO</h6>
<small class="text-muted">Corrente + Poupança + Dinheiro</small>
```

**Depois:**
```razor
<h6 class="text-muted mb-2">@Localization.Get("Dashboard.LiquidBalance").ToUpper()</h6>
<small class="text-muted">@Localization.Get("Dashboard.LiquidBalanceDesc")</small>
```

#### Transactions.razor:

**Antes:**
```razor
<PageTitle>Transações - MoneyManager</PageTitle>
<h1>Transações</h1>
```

**Depois:**
```razor
<PageTitle>@Localization.Get("Transactions.PageTitle")</PageTitle>
<h1>@Localization.Get("Transactions.Title")</h1>
```

---

## ?? CHECKLIST DE EXECUÇÃO:

### 1?? Correção de Encoding:
- [ ] Execute o script PowerShell acima
- [ ] Verifique os logs (deve mostrar ? para cada arquivo)
- [ ] Commit as mudanças: `git add . && git commit -m "fix: Corrigir encoding UTF-8 em arquivos .razor"`

### 2?? Localização do Dashboard:
- [ ] Substituir "SALDO LÍQUIDO" por `@Localization.Get("Dashboard.LiquidBalance").ToUpper()`
- [ ] Substituir "PATRIMÔNIO TOTAL" por `@Localization.Get("Dashboard.TotalAssets").ToUpper()`
- [ ] Substituir "Receitas do Mês" por `@Localization.Get("Dashboard.MonthlyIncome")`
- [ ] Substituir "Despesas do Mês" por `@Localization.Get("Dashboard.MonthlyExpenses")`
- [ ] Substituir "Orçamento do Mês" por `@Localization.Get("Dashboard.BudgetChart")`
- [ ] Substituir "Contas Líquidas" por `@Localization.Get("Dashboard.LiquidAccounts")`
- [ ] Substituir "Cartões de Crédito" por `@Localization.Get("Dashboard.CreditCards")`
- [ ] Substituir "Limite Disponível" por `@Localization.Get("Dashboard.CreditLimit")`
- [ ] Substituir "Transações Recentes" por `@Localization.Get("Dashboard.RecentTransactions")`

### 3?? Localização do Transactions.razor:
- [ ] Substituir "Transações" por `@Localization.Get("Transactions.Title")`
- [ ] Substituir "Nova Transação" por `@Localization.Get("Transactions.NewTransaction")`
- [ ] Substituir "Ordenar por" por label
- [ ] Substituir "Período" por `@Localization.Get("Transactions.Period")`
- [ ] Substituir headers da tabela

### 4?? Localização do Categories.razor:
- [ ] Substituir "Categorias" por `@Localization.Get("Categories.Title")`
- [ ] Substituir "Nova Categoria" por `@Localization.Get("Categories.NewCategory")`
- [ ] Substituir "Carregando categorias..." por `@Localization.Get("Categories.Loading")`

---

## ?? EXECUÇÃO RÁPIDA:

### Opção 1: Script PowerShell Completo

Salve este script como `fix-encoding.ps1`:

```powershell
# fix-encoding.ps1
$ErrorActionPreference = "Stop"

Write-Host "?? INICIANDO CORREÇÃO DE ENCODING..." -ForegroundColor Cyan
Write-Host ""

$projectRoot = "E:\_Projetos\money-manager"
$webPath = Join-Path $projectRoot "src\MoneyManager.Web"

# Navegar para o diretório
Set-Location $projectRoot

# Encontrar todos os arquivos .razor
$files = Get-ChildItem $webPath -Filter "*.razor" -Recurse

# Mapeamento de caracteres corrompidos
$replacements = @{
    'ÃƒÂ§ÃƒÂµ' = 'ções'
    'ÃƒÂ§ÃƒÂ£' = 'ção'
    'ÃƒÂ­' = 'í'
    'ÃƒÂ©' = 'é'
    'ÃƒÂª' = 'ê'
    'ÃƒÂ³' = 'ó'
    'ÃƒÂ£' = 'ã'
    'ÃƒÂ§' = 'ç'
    'ÃƒÂ' = 'Õ'
    'ÃƒÂ¡' = 'á'
    'Ã§Ãµ' = 'ções'
    'Ã§Ã£' = 'ção'
    'Ã­' = 'í'
    'Ã©' = 'é'
    'Ãª' = 'ê'
    'Ã³' = 'ó'
    'Ã£' = 'ã'
    'Ã§' = 'ç'
    'Ã' = 'Õ'
    'Ã¡' = 'á'
}

$totalFiles = 0
$fixedFiles = 0

foreach ($file in $files) {
    $totalFiles++
    Write-Host "[$totalFiles/$($files.Count)] Processando: $($file.Name)" -ForegroundColor Yellow
    
    try {
        $content = Get-Content $file.FullName -Raw -Encoding UTF8
        $originalContent = $content
        
        foreach ($key in $replacements.Keys) {
            $content = $content -replace [regex]::Escape($key), $replacements[$key]
        }
        
        if ($content -ne $originalContent) {
            $content | Set-Content $file.FullName -Encoding UTF8 -NoNewline
            Write-Host "   ? CORRIGIDO!" -ForegroundColor Green
            $fixedFiles++
        } else {
            Write-Host "   ??  Nenhuma mudança necessária" -ForegroundColor Gray
        }
    }
    catch {
        Write-Host "   ? ERRO: $_" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "? CONCLUÍDO!" -ForegroundColor Green
Write-Host "   Total de arquivos: $totalFiles" -ForegroundColor White
Write-Host "   Arquivos corrigidos: $fixedFiles" -ForegroundColor Green
Write-Host "=====================================" -ForegroundColor Cyan
```

**Execute:**
```powershell
.\fix-encoding.ps1
```

---

### Opção 2: Comando Único

```powershell
Get-ChildItem "E:\_Projetos\money-manager\src\MoneyManager.Web" -Filter "*.razor" -Recurse | ForEach-Object { $c = Get-Content $_.FullName -Raw -Encoding UTF8; $c = $c -replace 'ÃƒÂ§ÃƒÂµ','ções' -replace 'ÃƒÂ§ÃƒÂ£','ção' -replace 'ÃƒÂ­','í' -replace 'ÃƒÂ©','é' -replace 'ÃƒÂª','ê' -replace 'ÃƒÂ³','ó' -replace 'ÃƒÂ£','ã' -replace 'ÃƒÂ§','ç' -replace 'Ã­','í' -replace 'Ã©','é' -replace 'Ãª','ê' -replace 'Ã³','ó' -replace 'Ã£','ã' -replace 'Ã§','ç'; $c | Set-Content $_.FullName -Encoding UTF8 -NoNewline }
```

---

## ? TESTE APÓS CORREÇÃO:

1. Execute a aplicação
2. Acesse `/dashboard`
3. Verifique se todos os acentos estão corretos
4. Acesse `/transactions`
5. Verifique tabela e filtros

**Resultado Esperado:**
```
? "Transações" (não "Transaçµes")
? "Período" (não "Períÿodo")
? "Poupança" (não "Poupanša")
? "Orçamento" (não "Orçamento")
```

---

## ?? COMMIT:

Após as correções:

```bash
git add .
git commit -m "fix: Corrigir encoding UTF-8 em todas as páginas .razor"
git push origin main
```

---

**Status:** ?? **SCRIPT PRONTO**  
**Próximo Passo:** Executar o script PowerShell

---

*Esta correção vai resolver TODOS os problemas de encoding de uma vez!*
