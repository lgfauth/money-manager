# ? FASE 4 - Correção de Encoding em Profile, Settings e NavMenu - EM PROGRESSO

## ?? Objetivo:

Corrigir todos os problemas de encoding UTF-8 nas páginas de **Usuário (Profile.razor)**, **Configurações (Settings.razor)** e **Menu de Navegação (NavMenu.razor)**.

---

## ?? Status Atual:

### 1. Arquivo `pt-BR.json` ? CONCLUÍDO
- ? Adicionadas todas as labels necessárias para Profile
- ? Adicionadas todas as labels necessárias para Settings  
- ? Adicionada label "User" em Navigation

### 2. Profile.razor ?? PARCIALMENTE CONCLUÍDO
**Labels substituídas com sucesso:**
- ? PageTitle
- ? Title  
- ? Subtitle
- ? PersonalInfo
- ? Username e UsernameHelp
- ? FullName e placeholder
- ? Phone e placeholder
- ? ProfilePictureLabel, placeholder e help
- ? SaveChanges button
- ? Security
- ? SecurityDescription
- ? ChangePassword button
- ? CurrentPassword, NewPassword, ConfirmNewPassword
- ? ConfirmPasswordChange button
- ? Cancel button (change password)
- ? ChangeEmail modal title
- ? NewEmail e placeholder
- ? PasswordToConfirm
- ? Confirm e Updating buttons
- ? Cancel button (email modal)
- ? DangerZone
- ? WantToDelete button
- ? EnterPassword e PasswordPlaceholder
- ? TypeToConfirm e DeleteConfirmText
- ? Cancel button (delete confirmation)

**Ainda com encoding incorreto:**
- ? Comentários HTML (linhas 53, 83, 247)
- ? Delete Account title (linha 254)
- ? Delete Warning paragraph (linhas 255-257)
- ? Delete items list (linhas 258-264)
- ? DeleteConfirmation header (linha 272)
- ? CannotUndo text (linha 275)
- ? Records count message (linha 276)
- ? UnderstandConsequences checkbox label (linhas 288-290)
- ? Deleting e ConfirmDeletion button text (linhas 297-304)
- ? C# code error messages (linhas 383, 422, 456)

### 3. Settings.razor ? NÃO INICIADO
Todo o arquivo precisa de correção de encoding

### 4. NavMenu.razor ? NÃO INICIADO
Todos os labels de navegação precisam de correção

---

## ?? Correções Restantes Necessárias:

### Profile.razor - Correções Restantes:

#### 1. Comentários HTML (3 ocorrências):
```razor
<!-- Informações do Perfil --> (linha 53)
<!-- Editar Informações Pessoais --> (linha 83)
<!-- Seção PERIGOSA: Exclusão de Conta (LGPD) --> (linha 247)
```
**Solução:** Remover ou deixar em inglês (comentários não aparecem para usuário)

#### 2. Delete Account Section (linhas 254-310):
Substituir:
```razor
<h6 class="text-danger fw-bold">Excluir Conta Permanentemente</h6>
<p class="text-muted mb-3">
    Esta ação é <strong>IRREVERSÍVEL</strong>. Todos os seus dados serão permanentemente excluídos, incluindo:
</p>
<ul class="text-muted mb-4">
    <li>Todas as suas contas e saldos</li>
    <li>Todas as transações registradas</li>
    <li>Todas as categorias personalizadas</li>
    <li>Todos os orçamentos criados</li>
    <li>Todas as transações recorrentes</li>
    <li>Seu perfil e configurações</li>
</ul>
```

Por:
```razor
<h6 class="text-danger fw-bold">@Localization.Get("Profile.DeleteAccount")</h6>
<p class="text-muted mb-3">
    @((MarkupString)Localization.Get("Profile.DeleteWarning"))
</p>
<ul class="text-muted mb-4">
    <li>@Localization.Get("Profile.DeleteItem1")</li>
    <li>@Localization.Get("Profile.DeleteItem2")</li>
    <li>@Localization.Get("Profile.DeleteItem3")</li>
    <li>@Localization.Get("Profile.DeleteItem4")</li>
    <li>@Localization.Get("Profile.DeleteItem5")</li>
    <li>@Localization.Get("Profile.DeleteItem6")</li>
</ul>
```

#### 3. Delete Confirmation Dialog (linhas 272-304):
Substituir:
```razor
<h6 class="alert-heading">
    <i class="fas fa-exclamation-circle"></i> Confirmação de Exclusão
</h6>
<p><strong>Esta ação NÃO pode ser desfeita!</strong></p>
<p class="mb-3">Você possui <strong class="fs-5">@dataCount</strong> registros que serão permanentemente excluídos.</p>
```

Por:
```razor
<h6 class="alert-heading">
    <i class="fas fa-exclamation-circle"></i> @Localization.Get("Profile.DeleteConfirmation")
</h6>
<p><strong>@Localization.Get("Profile.CannotUndo")</strong></p>
<p class="mb-3">Você possui <strong class="fs-5">@dataCount</strong> registros que serão permanentemente excluídos.</p>
```

#### 4. Understand Consequences Checkbox (linhas 288-290):
Substituir:
```razor
<label class="form-check-label" for="understandCheck">
    Eu entendo que esta ação é permanente e todos os meus dados serão excluídos
</label>
```

Por:
```razor
<label class="form-check-label" for="understandCheck">
    @Localization.Get("Profile.UnderstandConsequences")
</label>
```

#### 5. Delete Buttons (linhas 297-304):
Substituir:
```razor
@if (isDeletingAccount)
{
    <span class="spinner-border spinner-border-sm me-2"></span>
    <span>Excluindo...</span>
}
else
{
    <span><i class="fas fa-trash-alt"></i> Confirmar Exclusão Permanente</span>
}
```

Por:
```razor
@if (isDeletingAccount)
{
    <span class="spinner-border spinner-border-sm me-2"></span>
    <span>@Localization.Get("Profile.Deleting")</span>
}
else
{
    <span><i class="fas fa-trash-alt"></i> @Localization.Get("Profile.ConfirmDeletion")</span>
}
```

#### 6. C# Code Error Messages (section @code):
Substituir hardcoded error messages:
```csharp
// Linha 383:
errorMessage = "As senhas não coincidem";

// Linha 422:
errorMessage = $"Erro ao carregar informações: {ex.Message}";

// Linha 456:
// Redireciona para página de confirmação
Nav.NavigateTo("/account-deleted", true);
```

Por:
```csharp
// Linha 383:
errorMessage = Localization.Get("Profile.ErrorPasswordMismatch");

// Linha 422:
errorMessage = string.Format(Localization.Get("Profile.ErrorLoadingInfo"), ex.Message);

// Linha 456:
// Redirect to confirmation page
Nav.NavigateTo("/account-deleted", true);
```

---

### Settings.razor - Todas as Correções Necessárias:

Substituir TODOS os textos hardcoded por chamadas ao `Localization.Get()`:

1. **Título e subtítulo** (linhas 11-13)
2. **Loading** (linha 22)
3. **Preferências Financeiras** (linha 46)
4. **Moeda** (linhas 49-54)
5. **Formato de Data** (linhas 57-63)
6. **Dia de Fechamento do Mês** (linhas 66-69)
7. **Orçamento Mensal Padrão** (linhas 72-75)
8. **Notificações** (linhas 82-84, 90-136)
9. **Aparência** (linhas 142-163)
10. **Botões** (linhas 170-184)

**Exemplo de substituição (Preferências Financeiras):**
```razor
<!-- ANTES: -->
<h5 class="mb-0"><i class="fas fa-money-bill-wave"></i> Preferências Financeiras</h5>

<!-- DEPOIS: -->
<h5 class="mb-0"><i class="fas fa-money-bill-wave"></i> @Localization.Get("Settings.FinancialPreferences")</h5>
```

---

### NavMenu.razor - Correções Necessárias:

Substituir TODOS os labels de navegação:

```razor
<!-- ANTES: -->
<span>Transações</span>
<span>Orçamentos</span>
<span>Relatórios</span>

<!-- DEPOIS: -->
<span>@Localization.Get("Navigation.Transactions")</span>
<span>@Localization.Get("Navigation.Budgets")</span>
<span>@Localization.Get("Navigation.Reports")</span>
```

---

## ?? Labels Disponíveis em pt-BR.json:

### Profile (57 labels):
- PageTitle, Title, Subtitle
- PersonalInfo, FullName, Email, Phone, ProfilePicture
- Username, UsernameHelp, MemberSince
- ChangePassword, Security, SecurityDescription
- CurrentPassword, NewPassword, ConfirmNewPassword
- ConfirmPasswordChange, Changing
- Save, SaveChanges, Saving, Cancel
- Loading, SaveSuccess, PasswordChanged, EmailUpdated
- ChangeEmail, NewEmail, PasswordToConfirm
- Confirm, Updating
- DangerZone, DeleteAccount, DeleteWarning
- DeleteItem1-6
- WantToDelete, DeleteConfirmation, CannotUndo
- EnterPassword, TypeToConfirm, DeleteConfirmText
- UnderstandConsequences, ConfirmDeletion, Deleting
- Placeholders e help texts

### Settings (46 labels):
- PageTitle, Title, Subtitle
- General, Language, Currency, DateFormat
- MonthClosingDay, DefaultBudget
- FinancialPreferences
- Theme, Light, Dark, Auto, ThemeHelp
- PrimaryColor, Appearance
- Notifications, EmailNotifications, ReceiveEmailNotifications
- RecurringProcessed, BudgetAlert, AlertWhenReaching
- CreditLimitAlert, MonthlySummary
- Save, SaveSettings, Saving, Reset, Cancel
- Loading, ErrorLoading, ErrorSaving, SaveSuccess
- Currency options (BRL, USD, EUR)
- Date format options
- Help texts

### Navigation (11 labels):
- Dashboard, Categories, Accounts, Transactions
- RecurringTransactions, Budgets, Reports
- Profile, Settings, Logout, User

---

## ? Como Completar:

### Opção 1: Manual (Recomendado para aprender)
1. Abrir cada arquivo (.razor)
2. Localizar os textos com encoding incorreto
3. Substituir por `@Localization.Get("Namespace.LabelName")`
4. Salvar e testar

### Opção 2: Script PowerShell (Rápido)
```powershell
# Criar script de substituição automática
# (Exemplo - adaptar conforme necessário)

$profilePath = "src\MoneyManager.Web\Pages\Profile.razor"
$content = Get-Content $profilePath -Raw -Encoding UTF8

# Substituir textos específicos
$replacements = @{
    'Excluir Conta Permanentemente' = '@Localization.Get("Profile.DeleteAccount")'
    'Confirma.*o de Exclus.*o' = '@Localization.Get("Profile.DeleteConfirmation")'
    # ... adicionar mais substituições
}

foreach ($key in $replacements.Keys) {
    $content = $content -replace $key, $replacements[$key]
}

$content | Set-Content $profilePath -Encoding UTF8 -NoNewline
```

---

## ?? Prioridade de Execução:

1. ? **CONCLUÍDO:** pt-BR.json expandido com todas as labels
2. ?? **EM PROGRESSO:** Profile.razor (~70% concluído)
3. ? **PENDENTE:** Settings.razor (0% concluído)
4. ? **PENDENTE:** NavMenu.razor (0% concluído)

---

## ?? Como Testar:

Após cada correção:
1. Executar: `dotnet run --project src/MoneyManager.Web`
2. Navegar para a página corrigida
3. Verificar se todos os acentos aparecem corretamente
4. Testar todas as funcionalidades (botões, forms, modals)

**Resultado Esperado:** ? ZERO caracteres quebrados!

---

## ?? Progresso Geral:

| Arquivo | Status | Progresso |
|---------|--------|-----------|
| pt-BR.json | ? Concluído | 100% |
| Profile.razor | ?? Em Progresso | 70% |
| Settings.razor | ? Pendente | 0% |
| NavMenu.razor | ? Pendente | 0% |

**Total Geral:** ~43% concluído

---

**Data:** 2024  
**Autor:** GitHub Copilot  
**Status:** ?? **EM PROGRESSO**

**Próximo Passo:** Completar Profile.razor, depois Settings.razor, depois NavMenu.razor
