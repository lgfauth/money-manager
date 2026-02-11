# ? FASE 4 EM PROGRESSO: Interface Visual (UI)

## ?? RESUMO DA IMPLEMENTAÇÃO

### **Status:** ?? **EM ANDAMENTO** (50% completo)
### **Tempo:** ~2 horas
### **Build:** ? **SUCESSO**

---

## ?? O QUE FOI IMPLEMENTADO

### **1. Formulário de Cadastro de Cartão Atualizado** ?

#### **Novos Campos Adicionados:**
- ? **Limite de Crédito** (R$)
  - Input numérico com decimais
  - Opcional (pode ficar em branco)
  - Hint: "Deixe em branco se não quiser limite"

- ? **Dias até Vencimento**
  - Input numérico (1-30 dias)
  - Valor padrão: 7 dias
  - Hint: "Dias entre fechamento e vencimento"

#### **Exibição no Card do Cartão:**
- ? Mostra "Vencimento: X dias após fechamento"
- ? Mostra "Limite: R$ X.XXX,XX"
- ? Calcula e mostra "Disponível: R$ X.XXX,XX"
  ```csharp
  var used = Math.Abs(account.Balance);
  var available = account.CreditLimit.Value - used;
  ```

#### **Código Implementado:**
```razor
@if (newAccount.Type == AccountType.CreditCard)
{
    <div class="col-12 col-md-3">
        <label class="form-label">Limite de crédito (R$)</label>
        <input type="number" step="0.01" class="form-control" @bind="creditLimitInput" placeholder="0,00" />
        <small class="text-muted">Deixe em branco se não quiser limite.</small>
    </div>
    <div class="col-12 col-md-3">
        <label class="form-label">Dias até vencimento</label>
        <input type="number" class="form-control" min="1" max="30" @bind="newAccount.InvoiceDueDayOffset" />
        <small class="text-muted">Dias entre fechamento e vencimento (padrão: 7).</small>
    </div>
}
```

---

## ?? O QUE FALTA IMPLEMENTAR

### **2. Modal "Pagar Fatura" com Lista de Faturas** ?

**Status:** Preparado mas não finalizado

**O que precisa:**
- Transformar modal simples em lista de faturas pendentes
- Buscar faturas via `InvoiceService.GetPendingInvoicesAsync()`
- Mostrar cards de faturas com:
  - Período (dd/MM a dd/MM/yyyy)
  - Valor total
  - Valor restante
  - Status (badge colorido)
  - Dias até vencimento
  - Número de transações
  - Botão "Pagar"

**Estrutura Sugerida:**
```razor
@if (showPayInvoice)
{
    <div class="modal fade show d-block">
        <div class="modal-dialog modal-xl">
            <div class="modal-content">
                <div class="modal-header">
                    <h5>Faturas Pendentes - @selectedCard?.Name</h5>
                </div>
                <div class="modal-body">
                    @foreach (var invoice in pendingInvoices)
                    {
                        <div class="card mb-3">
                            <!-- Card da fatura -->
                        </div>
                    }
                    
                    @if (selectedInvoiceToPay != null)
                    {
                        <!-- Formulário de pagamento -->
                    }
                </div>
            </div>
        </div>
    </div>
}
```

---

### **3. Página de Detalhes da Fatura** ?

**Arquivo:** `src/MoneyManager.Web/Pages/InvoiceDetails.razor` (criar)

**Route:** `@page "/invoices/{InvoiceId}"`

**Funcionalidades:**
- Header com informações da fatura
- Lista de transações da fatura
- Total por categoria (gráfico de pizza)
- Histórico de pagamentos
- Botão "Pagar" se não estiver paga

---

### **4. Componente de Lista de Transações da Fatura** ?

**Arquivo:** `src/MoneyManager.Web/Components/InvoiceTransactionsList.razor` (criar)

**Props:**
- `InvoiceId` (string)
- `ShowCategory` (bool)
- `ShowActions` (bool)

---

### **5. Dashboard do Cartão** ?

**Arquivo:** `src/MoneyManager.Web/Pages/CreditCardDashboard.razor` (criar)

**Route:** `@page "/credit-cards/{AccountId}"`

**Seções:**
- Card "Fatura Atual (Aberta)"
- Card "Fatura Fechada (A Vencer)"
- Card "Limite Disponível"
- Gráfico de gastos por mês
- Histórico de faturas (tabela)

---

## ?? CÓDIGO ADICIONAL NECESSÁRIO

### **Variáveis no @code (Accounts.razor):**
```csharp
private bool isLoadingInvoices;
private List<CreditCardInvoiceResponseDto>? pendingInvoices;
private CreditCardInvoiceResponseDto? selectedInvoiceToPay;

private async Task LoadPendingInvoices()
{
    if (selectedCard == null) return;
    
    isLoadingInvoices = true;
    try
    {
        var allInvoices = await InvoiceService.GetInvoicesByAccountAsync(selectedCard.Id);
        pendingInvoices = allInvoices
            .Where(i => i.Status != InvoiceStatus.Paid)
            .OrderBy(i => i.DueDate)
            .ToList();
    }
    catch (Exception ex)
    {
        invoiceError = $"Erro ao carregar faturas: {ex.Message}";
    }
    finally
    {
        isLoadingInvoices = false;
    }
}

private void SelectInvoiceToPay(CreditCardInvoiceResponseDto invoice)
{
    selectedInvoiceToPay = invoice;
    invoicePaymentAmount = invoice.RemainingAmount;
    payFromAccountId = string.Empty;
}

private async Task ConfirmInvoicePayment()
{
    // Implementar pagamento via InvoiceService
    var request = new PayInvoiceRequestDto
    {
        InvoiceId = selectedInvoiceToPay.Id,
        PayFromAccountId = payFromAccountId,
        Amount = invoicePaymentAmount,
        PaymentDate = invoicePaymentDate
    };
    
    if (invoicePaymentAmount >= selectedInvoiceToPay.RemainingAmount)
        await InvoiceService.PayInvoiceAsync(request);
    else
        await InvoiceService.PayPartialInvoiceAsync(request);
    
    await LoadPendingInvoices();
}

private static string GetInvoiceStatusBadgeClass(InvoiceStatus status, bool isOverdue)
{
    if (isOverdue) return "bg-danger";
    return status switch
    {
        InvoiceStatus.Open => "bg-info",
        InvoiceStatus.Closed => "bg-warning text-dark",
        InvoiceStatus.Paid => "bg-success",
        InvoiceStatus.PartiallyPaid => "bg-warning text-dark",
        _ => "bg-secondary"
    };
}
```

---

## ??? ARQUIVOS CRIADOS/MODIFICADOS

### **Modificados:**
```
src/MoneyManager.Web/Pages/Accounts.razor
??? + Limite de crédito (input)
??? + Dias até vencimento (input)
??? + Exibição de limite disponível no card
??? + InvoiceService injetado
??? + creditLimitInput variável
??? + Sincronização limite com Account

src/MoneyManager.Domain/Entities/Account.cs (já feito na FASE 1)
??? + CreditLimit (decimal?)
??? + InvoiceDueDayOffset (int, default 7)
??? + LastInvoiceClosedAt (DateTime?)
??? + CurrentOpenInvoiceId (string?)
```

### **A Criar:**
```
src/MoneyManager.Web/Pages/
??? InvoiceDetails.razor (detalhes da fatura)
??? CreditCardDashboard.razor (dashboard do cartão)

src/MoneyManager.Web/Components/
??? InvoiceTransactionsList.razor (lista transações)
??? InvoiceStatusBadge.razor (badge status)
```

---

## ?? VALIDAÇÃO

### **Build:**
```
? Compilação bem-sucedida
? Sem erros
? Sem warnings
```

### **Testes Manuais Necessários:**
1. Criar cartão com limite
2. Verificar se limite aparece no card
3. Editar cartão e alterar limite
4. Criar despesa e validar limite
5. Clicar em "Pagar Fatura" (ainda mostra modal antigo)

---

## ?? PRÓXIMOS PASSOS

### **Passo 1: Completar Modal de Pagamento**
- Substituir modal simples por lista de faturas
- Adicionar métodos LoadPendingInvoices, SelectInvoiceToPay
- Implementar ConfirmInvoicePayment com InvoiceService

### **Passo 2: Criar Página de Detalhes**
- Novo arquivo InvoiceDetails.razor
- Buscar fatura via InvoiceService.GetInvoiceSummaryAsync()
- Mostrar transações, categorias, pagamentos

### **Passo 3: Criar Dashboard do Cartão**
- Novo arquivo CreditCardDashboard.razor
- 3 cards principais (atual, fechada, limite)
- Histórico de faturas
- Gráficos

### **Passo 4: Componentes Reutilizáveis**
- InvoiceTransactionsList
- InvoiceStatusBadge
- InvoiceCard

---

## ?? MELHORIAS SUGERIDAS

### **UX:**
- Animação de loading ao buscar faturas
- Tooltips explicativos
- Confirmação antes de pagar
- Toast de sucesso após pagamento

### **Validações:**
- Validar se valor do pagamento é válido
- Validar se tem saldo na conta pagadora
- Alertar se pagamento parcial
- Mostrar quanto falta pagar

### **Visual:**
- Badge colorido de status (verde=paga, vermelho=vencida, amarelo=fechada)
- Ícones indicando tipo de fatura
- Progress bar de quanto foi pago
- Gráfico de gastos por categoria

---

## ?? PROGRESSO GERAL

| Fase | Sub-tarefa | Status | %
|------|-----------|--------|----
| **FASE 4** | Interface Visual | ?? | 50%
| 4.1 | Formulário Cartão | ? | 100%
| 4.2 | Modal Pagamento | ? | 20%
| 4.3 | Detalhes Fatura | ? | 0%
| 4.4 | Dashboard Cartão | ? | 0%
| 4.5 | Componentes | ? | 0%

---

## ?? RESUMO GERAL DO PROJETO

| Fase | Status | Funcionalidade |
|------|--------|----------------|
| **FASE 1** | ? | Fundação (Entidades, Repos) |
| **FASE 2** | ? | Serviço de Gestão |
| **FASE 3** | ? | Integração + Workers |
| **FASE 4** | ?? | Interface Visual (50%) |

**Total Implementado:** Backend 100% + Frontend 50%

---

**Próximo Comando:**
```
"Continuar FASE 4: Completar modal de pagamento de faturas"
```

---

**Estimativa para Conclusão da FASE 4:**
- Modal de Pagamento: 2 horas
- Página Detalhes: 3 horas
- Dashboard: 4 horas
- Componentes: 2 horas
- **Total:** ~11 horas (2 dias)
