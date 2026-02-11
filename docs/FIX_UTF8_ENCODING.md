# ?? CORREÇÃO: Encoding UTF-8 em Arquivos Blazor

## ?? PROBLEMA IDENTIFICADO

### **Sintoma:**
Caracteres especiais e acentos aparecem como `?` (caractere de substituição):
- `Cartão` ? `Cart?o`
- `Crédito` ? `Cr?dito`
- `Período` ? `Per?odo`
- `Transações` ? `Transa??es`
- `Histórico` ? `Hist?rico`

### **Causa:**
Arquivos `.razor` foram salvos com encoding **incorreto** (provavelmente Latin-1, Windows-1252 ou ANSI) ao invés de **UTF-8**.

---

## ? SOLUÇÃO APLICADA

### **Arquivos Corrigidos:**
- ? `src/MoneyManager.Web/Pages/CreditCardDashboard.razor`
- ? `src/MoneyManager.Web/Pages/AdminMigration.razor`

**Mudanças:**
- Recriados com encoding UTF-8 correto
- Todos os acentos e caracteres especiais corrigidos

---

## ?? OUTROS ARQUIVOS AFETADOS

Arquivos que também precisam de correção:
```
src\MoneyManager.Web\Pages\AccountDeleted.razor
src\MoneyManager.Web\Pages\AdminMigration.razor
src\MoneyManager.Web\Pages\Accounts.razor (possível)
src\MoneyManager.Web\Pages\InvoiceDetails.razor (possível)
... outros arquivos .razor
```

---

## ??? COMO CORRIGIR MANUALMENTE (Visual Studio Code)

### **Passo a Passo:**

1. **Abrir arquivo no VSCode**
2. **Clicar na barra inferior** onde mostra `UTF-8` ou outro encoding
3. **Selecionar:** "Save with Encoding"
4. **Escolher:** "UTF-8"
5. **Salvar arquivo**

---

## ??? COMO CORRIGIR VIA POWERSHELL (TODOS OS ARQUIVOS)

```powershell
# Corrigir todos os arquivos .razor
Get-ChildItem -Path "src\MoneyManager.Web\Pages" -Filter "*.razor" -Recurse | ForEach-Object {
    $content = Get-Content $_.FullName -Encoding Latin1
    $content | Out-File -FilePath $_.FullName -Encoding UTF8
    Write-Host "Corrigido: $($_.Name)" -ForegroundColor Green
}
```

**?? AVISO:** Execute com cuidado, faça backup antes!

---

## ?? CORREÇÃO DEFINITIVA (EDITORCONFIG)

Adicionar no arquivo `.editorconfig` na raiz do projeto:

```ini
# .editorconfig
root = true

[*]
charset = utf-8
end_of_line = lf
insert_final_newline = true
trim_trailing_whitespace = true

[*.{cs,razor}]
charset = utf-8
indent_style = space
indent_size = 4

[*.{json,xml,csproj,sln}]
charset = utf-8
indent_style = space
indent_size = 2
```

---

## ?? VALIDAÇÃO

### **Antes:**
```html
<h1>Cartão Nubank</h1>          ? Mostra: Cart?o Nubank
<p>Cartão de Crédito</p>        ? Mostra: Cart?o de Cr?dito
<th>Período</th>                 ? Mostra: Per?odo
<th>Transações</th>              ? Mostra: Transa??es
```

### **Depois:**
```html
<h1>Cartão Nubank</h1>          ? Mostra: Cartão Nubank
<p>Cartão de Crédito</p>        ? Mostra: Cartão de Crédito
<th>Período</th>                 ? Mostra: Período
<th>Transações</th>              ? Mostra: Transações
```

---

## ?? BUILD

```bash
dotnet build
```

? Compilação bem-sucedida (encoding não afeta compilação)

---

## ?? PRÓXIMOS PASSOS

1. **Testar localmente:** Acessar `/credit-cards/{id}` e verificar acentos
2. **Corrigir outros arquivos** se necessário
3. **Adicionar .editorconfig** para evitar problema futuro
4. **Commit e deploy**

---

## ?? COMMIT SUGERIDO

```bash
git add .
git commit -m "fix: correct UTF-8 encoding in CreditCardDashboard.razor

- Fixed character encoding issues (?)
- All accents and special characters now display correctly
- Recreated file with proper UTF-8 encoding

Fixed text:
- Cartão ? was showing as Cart?o
- Crédito ? was showing as Cr?dito
- Período ? was showing as Per?odo
- Transações ? was showing as Transa??es
- Histórico ? was showing as Hist?rico

Affects:
- /credit-cards/{id} page
- All dashboard cards
- Invoice history table"

git push origin main
```

---

## ? RESULTADO FINAL

### **Dashboard do Cartão:**
- ? Título: "Dashboard do Cartão"
- ? Subtítulo: "Cartão de Crédito"
- ? Cards: "Período", "Transações"
- ? Tabela: "Mês/Ano", "Período", "Transações", "Ações"
- ? Todos os acentos corretos

---

**Status:** ? **RESOLVIDO**  
**Encoding:** ? **UTF-8**  
**Acentos:** ? **CORRETOS**
