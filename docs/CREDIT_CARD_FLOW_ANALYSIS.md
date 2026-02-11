# ?? ANÁLISE COMPLETA: Fluxo de Cartão de Crédito

## ?? SITUAÇÃO ATUAL

### **O que existe hoje:**

1. **Cadastro de Cartão:**
   - ? Nome do cartão
   - ? Dia de fechamento da fatura
   - ? Saldo (representa dívida atual)
   - ? InitialBalance (saldo inicial quando criado)

2. **Criação de Despesas:**
   - ? Transações normais no cartão
   - ? Compras parceladas (1ª parcela imediata + recorrência)
   - ? Saldo do cartão aumenta automaticamente (dívida cresce)

3. **Botão "Pagar Fatura":**
   - ? Abre modal para pagamento
   - ? Permite escolher conta pagadora
   - ? Permite definir valor do pagamento
   - ? Cria uma **transferência** (conta ? cartão)
   - ? **Reduz o saldo do cartão** (paga a dívida)

### **? O QUE ESTÁ FALTANDO/ERRADO:**

#### **Problema 1: Sem Conceito de "Fatura"**
```
Sistema atual:
- Não há separação entre "fatura atual" e "fatura anterior"
- Não há rastreamento do período da fatura
- Não há conceito de "fatura fechada" vs "fatura aberta"
```

#### **Problema 2: Pagamento Não Considera Período**
```
Quando clica "Pagar Fatura":
- Sistema mostra o SALDO TOTAL do cartão
- Não mostra apenas o que está na "fatura vencida"
- Transações após o fechamento se misturam com anteriores
```

#### **Problema 3: Sem Histórico de Faturas**
```
- Não há registro de faturas pagas
- Não há relatório de "qual foi o valor da fatura de janeiro"
- Não há controle de vencimento
```

#### **Problema 4: Confusão de Nomenclatura**
```
"Saldo" do cartão atualmente = Dívida total acumulada
Não diferencia:
- Fatura vencida (precisa pagar agora)
- Fatura atual (ainda aberta, vai fechar no dia X)
- Fatura futura (compras que vão para próxima)
```

---

## ?? PLANO DE AÇÃO: Implementação Real de Cartão de Crédito

### **FASE 1: Adicionar Entidade "Invoice" (Fatura)**

#### **1.1 Nova Entidade: `CreditCardInvoice`**
```csharp
public class CreditCardInvoice
{
    public string Id { get; set; }
    public string AccountId { get; set; }  // ID do cartão
    public string UserId { get; set; }
    
    // Período da fatura
    public DateTime PeriodStart { get; set; }    // Data de início (fechamento anterior + 1)
    public DateTime PeriodEnd { get; set; }      // Data de fechamento desta fatura
    public DateTime DueDate { get; set; }        // Data de vencimento (geralmente 7-10 dias após fechamento)
    
    // Valores
    public decimal TotalAmount { get; set; }     // Valor total da fatura
    public decimal PaidAmount { get; set; }      // Quanto foi pago
    public decimal RemainingAmount { get; set; } // Quanto ainda falta pagar
    
    // Status
    public InvoiceStatus Status { get; set; }    // Open, Closed, Paid, Overdue, PartiallyPaid
    
    // Metadados
    public DateTime ClosedAt { get; set; }       // Quando fechou
    public DateTime? PaidAt { get; set; }        // Quando foi paga
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum InvoiceStatus
{
    Open = 0,           // Fatura aberta (ainda aceitando transações)
    Closed = 1,         // Fatura fechada (não aceita mais transações)
    Paid = 2,           // Fatura paga completamente
    PartiallyPaid = 3,  // Fatura parcialmente paga
    Overdue = 4         // Fatura vencida (passou da data de vencimento)
}
```

#### **1.2 Modificar Entidade `Transaction`**
```csharp
// Adicionar campo:
public string? InvoiceId { get; set; }  // Vincula transação à fatura
```

#### **1.3 Modificar Entidade `Account` (Cartão)**
```csharp
// Adicionar campos:
public DateTime? LastInvoiceClosedAt { get; set; }  // Última vez que fatura fechou
public string? CurrentOpenInvoiceId { get; set; }   // Fatura aberta atual
public int InvoiceDueDayOffset { get; set; } = 7;   // Dias entre fechamento e vencimento
```

---

### **FASE 2: Serviço de Gestão de Faturas**

#### **2.1 Criar `ICreditCardInvoiceService`**
```csharp
public interface ICreditCardInvoiceService
{
    // Gestão de faturas
    Task<CreditCardInvoice> GetCurrentOpenInvoiceAsync(string userId, string accountId);
    Task<CreditCardInvoice> GetInvoiceByIdAsync(string userId, string invoiceId);
    Task<IEnumerable<CreditCardInvoice>> GetInvoicesByAccountAsync(string userId, string accountId);
    Task<IEnumerable<CreditCardInvoice>> GetOverdueInvoicesAsync(string userId);
    
    // Fechamento automático de faturas
    Task<CreditCardInvoice> CloseInvoiceAsync(string userId, string invoiceId);
    Task ProcessMonthlyInvoiceClosuresAsync();  // Worker executa todo dia
    
    // Pagamento de faturas
    Task<Transaction> PayInvoiceAsync(string userId, PayInvoiceRequestDto request);
    Task<Transaction> PayPartialInvoiceAsync(string userId, PayInvoiceRequestDto request);
    
    // Relatórios
    Task<InvoiceSummaryDto> GetInvoiceSummaryAsync(string userId, string invoiceId);
    Task<IEnumerable<Transaction>> GetInvoiceTransactionsAsync(string userId, string invoiceId);
}
```

#### **2.2 Lógica de Fechamento Automático**
```
Worker roda TODO DIA às 08:00:
1. Busca todos os cartões de crédito ativos
2. Para cada cartão:
   - Verifica se hoje é o dia de fechamento
   - Se sim:
     a) Busca fatura aberta atual
     b) Calcula valor total das transações
     c) Marca fatura como "Closed"
     d) Cria nova fatura "Open" para próximo período
     e) Vincula transações futuras à nova fatura
```

---

### **FASE 3: Ajustar Fluxo de Transações**

#### **3.1 Ao Criar Transação em Cartão:**
```csharp
// TransactionService.CreateAsync()
if (account.Type == AccountType.CreditCard)
{
    // 1. Determinar a qual fatura esta transação pertence
    var invoice = await DetermineInvoiceForTransaction(account, transaction.Date);
    
    // 2. Vincular transação à fatura
    transaction.InvoiceId = invoice.Id;
    
    // 3. Atualizar total da fatura
    invoice.TotalAmount += transaction.Amount;
    await _invoiceService.UpdateInvoiceAsync(invoice);
}
```

#### **3.2 Determinar Fatura da Transação:**
```csharp
private async Task<CreditCardInvoice> DetermineInvoiceForTransaction(Account card, DateTime transactionDate)
{
    // Regra:
    // - Se transação for até o dia de fechamento ? Fatura atual (que vai fechar)
    // - Se transação for após o fechamento ? Próxima fatura
    
    var closingDay = card.InvoiceClosingDay ?? 1;
    var currentMonth = DateTime.Today;
    var closingDateThisMonth = new DateTime(currentMonth.Year, currentMonth.Month, closingDay);
    
    if (transactionDate.Date <= closingDateThisMonth)
    {
        // Entra na fatura que fecha neste mês
        return await GetOrCreateInvoiceForPeriod(card, closingDateThisMonth);
    }
    else
    {
        // Entra na fatura que fecha no próximo mês
        var nextClosing = closingDateThisMonth.AddMonths(1);
        return await GetOrCreateInvoiceForPeriod(card, nextClosing);
    }
}
```

---

### **FASE 4: Modificar "Pagar Fatura"**

#### **4.1 Novo Fluxo do Botão:**
```
Ao clicar "Pagar Fatura":
1. Buscar faturas FECHADAS e NÃO PAGAS do cartão
2. Mostrar lista de faturas pendentes:
   - Fatura Jan/2026 - Vencimento 17/01 - R$ 1.250,00 - STATUS: VENCIDA
   - Fatura Fev/2026 - Vencimento 17/02 - R$ 890,50 - STATUS: FECHADA
3. Usuário seleciona qual fatura quer pagar
4. Usuário escolhe:
   - Pagar total
   - Pagar parcial (informar valor)
5. Sistema cria transação de PAGAMENTO (não transferência)
6. Atualiza status da fatura
```

#### **4.2 Nova Interface (Modal):**
```razor
<div class="invoice-payment-modal">
    <h5>Faturas Pendentes - @selectedCard.Name</h5>
    
    @foreach (var invoice in pendingInvoices)
    {
        <div class="invoice-card">
            <div class="invoice-header">
                <span>Fatura @invoice.PeriodEnd.ToString("MMM/yyyy")</span>
                <span class="badge @GetStatusBadgeClass(invoice.Status)">
                    @GetStatusLabel(invoice.Status)
                </span>
            </div>
            <div class="invoice-body">
                <p>Período: @invoice.PeriodStart.ToString("dd/MM") a @invoice.PeriodEnd.ToString("dd/MM")</p>
                <p>Vencimento: @invoice.DueDate.ToString("dd/MM/yyyy")</p>
                <h4>Valor: R$ @invoice.TotalAmount.ToString("F2")</h4>
                @if (invoice.PaidAmount > 0)
                {
                    <p class="text-success">Pago: R$ @invoice.PaidAmount.ToString("F2")</p>
                    <p class="text-danger">Restante: R$ @invoice.RemainingAmount.ToString("F2")</p>
                }
            </div>
            <div class="invoice-actions">
                <button @onclick="() => SelectInvoiceToPay(invoice)">
                    Pagar Esta Fatura
                </button>
                <button @onclick="() => ViewInvoiceDetails(invoice)">
                    Ver Detalhes
                </button>
            </div>
        </div>
    }
</div>
```

---

### **FASE 5: Dashboard de Cartão (Nova Página)**

#### **5.1 Criar `/credit-card-details/{accountId}`**
```razor
<h1>@card.Name</h1>

<div class="row">
    <div class="col-md-4">
        <div class="card">
            <h5>Fatura Atual (Aberta)</h5>
            <p>Período: @currentInvoice.PeriodStart - @currentInvoice.PeriodEnd</p>
            <p>Fecha em: @currentInvoice.PeriodEnd.ToString("dd/MM")</p>
            <h3>R$ @currentInvoice.TotalAmount.ToString("F2")</h3>
            <button>Ver Lançamentos</button>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card">
            <h5>Fatura Fechada (A Vencer)</h5>
            <p>Vencimento: @closedInvoice.DueDate.ToString("dd/MM/yyyy")</p>
            <h3 class="text-danger">R$ @closedInvoice.RemainingAmount.ToString("F2")</h3>
            <button class="btn-primary">Pagar Fatura</button>
        </div>
    </div>
    
    <div class="col-md-4">
        <div class="card">
            <h5>Limite Disponível</h5>
            <p>Limite Total: R$ @card.CreditLimit.ToString("F2")</p>
            <p>Usado: R$ @usedLimit.ToString("F2")</p>
            <h3 class="text-success">R$ @availableLimit.ToString("F2")</h3>
        </div>
    </div>
</div>

<div class="row mt-4">
    <div class="col-12">
        <h4>Histórico de Faturas</h4>
        <table>
            <thead>
                <tr>
                    <th>Mês/Ano</th>
                    <th>Período</th>
                    <th>Valor</th>
                    <th>Vencimento</th>
                    <th>Status</th>
                    <th>Ações</th>
                </tr>
            </thead>
            <tbody>
                @foreach (var inv in invoiceHistory)
                {
                    <tr>
                        <td>@inv.PeriodEnd.ToString("MMM/yyyy")</td>
                        <td>@inv.PeriodStart.ToString("dd/MM") - @inv.PeriodEnd.ToString("dd/MM")</td>
                        <td>R$ @inv.TotalAmount.ToString("F2")</td>
                        <td>@inv.DueDate.ToString("dd/MM/yyyy")</td>
                        <td>@inv.Status</td>
                        <td>
                            <button>Ver</button>
                            @if (inv.Status != InvoiceStatus.Paid)
                            {
                                <button>Pagar</button>
                            }
                        </td>
                    </tr>
                }
            </tbody>
        </table>
    </div>
</div>
```

---

### **FASE 6: Adicionar Limite de Crédito (Opcional)**

#### **6.1 Modificar `Account`:**
```csharp
public decimal? CreditLimit { get; set; }  // Limite do cartão
```

#### **6.2 Validação ao Criar Transação:**
```csharp
if (account.Type == AccountType.CreditCard && account.CreditLimit.HasValue)
{
    var totalUsed = Math.Abs(account.Balance);
    var newTotal = totalUsed + transaction.Amount;
    
    if (newTotal > account.CreditLimit.Value)
    {
        throw new InvalidOperationException(
            $"Limite de crédito excedido. Disponível: R$ {account.CreditLimit.Value - totalUsed:F2}"
        );
    }
}
```

---

## ?? CRONOGRAMA DE IMPLEMENTAÇÃO

### **Sprint 1: Fundação (3-5 dias)**
- [ ] Criar entidade `CreditCardInvoice`
- [ ] Criar migração/seeds para MongoDB
- [ ] Adicionar `InvoiceId` em `Transaction`
- [ ] Criar repositório `ICreditCardInvoiceRepository`
- [ ] Criar testes unitários para entidade

### **Sprint 2: Serviço de Faturas (5-7 dias)**
- [ ] Implementar `CreditCardInvoiceService`
- [ ] Lógica de determinação de fatura para transação
- [ ] Lógica de fechamento automático de fatura
- [ ] Worker task para fechar faturas automaticamente
- [ ] Testes unitários do serviço

### **Sprint 3: Integração com Transações (3-4 dias)**
- [ ] Modificar `TransactionService.CreateAsync()`
- [ ] Vincular transações a faturas ao criar
- [ ] Atualizar total da fatura automaticamente
- [ ] Testes de integração

### **Sprint 4: Interface de Pagamento (4-5 dias)**
- [ ] Modificar modal "Pagar Fatura"
- [ ] Listar faturas pendentes
- [ ] Implementar pagamento total/parcial
- [ ] Atualizar status da fatura após pagamento
- [ ] Validações e feedback visual

### **Sprint 5: Dashboard do Cartão (5-7 dias)**
- [ ] Criar página `/credit-card-details`
- [ ] Card de fatura atual (aberta)
- [ ] Card de fatura fechada (a vencer)
- [ ] Histórico de faturas
- [ ] Detalhes de fatura individual
- [ ] Gráficos e relatórios

### **Sprint 6: Funcionalidades Extras (3-5 dias)**
- [ ] Adicionar limite de crédito
- [ ] Alertas de vencimento
- [ ] Notificações de fatura fechada
- [ ] Exportar fatura para PDF
- [ ] Melhorias de UX

---

## ?? BENEFÍCIOS DA IMPLEMENTAÇÃO

### **Para o Usuário:**
? Visão clara de quanto deve pagar AGORA vs futuro  
? Histórico completo de faturas anteriores  
? Controle de vencimentos  
? Pagamento parcial de faturas  
? Relatórios mensais detalhados  

### **Para o Sistema:**
? Rastreabilidade completa  
? Dados estruturados para relatórios  
? Automação de fechamento de faturas  
? Prevenção de erros (limite de crédito)  
? Conformidade com fluxo real de cartões  

---

## ?? PONTOS DE ATENÇÃO

### **1. Migração de Dados Existentes**
```
Transações antigas não têm InvoiceId:
- Opção A: Criar faturas retroativas (complexo)
- Opção B: Marcar como "legacy" e não vincular
- Opção C: Criar UMA fatura "histórico" e vincular todas
```

### **2. Timezone**
```
Fechamento de fatura às 23:59:59 do dia X:
- Usar sempre Date sem hora
- Worker processa às 00:01 do dia seguinte
```

### **3. Parcelamento**
```
Compra parcelada em 12x:
- 1ª parcela vai para fatura do mês da compra
- 2ª em diante vão para faturas futuras (recorrência)
- Cada parcela deve vincular à fatura correta
```

### **4. Performance**
```
Histórico de faturas pode crescer muito:
- Implementar paginação
- Considerar arquivamento de faturas antigas (>2 anos)
```

---

## ?? RECOMENDAÇÃO FINAL

**Abordagem Sugerida:** Implementação incremental em sprints

**Prioridade Alta:**
1. Entidade `CreditCardInvoice`
2. Serviço básico de faturas
3. Modificar "Pagar Fatura" para usar faturas

**Prioridade Média:**
4. Worker para fechar faturas automaticamente
5. Dashboard do cartão
6. Histórico de faturas

**Prioridade Baixa:**
7. Limite de crédito
8. Notificações e alertas
9. Exportação PDF

---

**Estimativa Total:** 4-6 semanas de desenvolvimento full-time

**MVP (Mínimo Viável):** Sprints 1-4 (2-3 semanas)

**Versão Completa:** Todos os sprints (4-6 semanas)
