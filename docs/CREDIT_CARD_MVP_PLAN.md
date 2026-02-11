# ?? RESUMO EXECUTIVO: Plano de Cartão de Crédito

## ? PROBLEMA ATUAL

O botão "Pagar Fatura" **funciona**, mas não da forma correta:
- Cria uma transferência (conta ? cartão)
- Reduz o saldo do cartão
- **MAS:** Não há conceito de "fatura fechada" vs "fatura aberta"
- **MAS:** Não separa o que você deve AGORA vs o que vai para próxima fatura
- **MAS:** Não há histórico de faturas pagas

**Exemplo do problema:**
```
Hoje: 11/02/2026
Fechamento: dia 09
Saldo do cartão: -R$ 2.000,00

Quando clica "Pagar Fatura":
- Sistema mostra: R$ 2.000,00 (tudo misturado)
- Correto seria:
  * Fatura fechada (09/01-09/02): R$ 1.200,00  ? Deve pagar AGORA
  * Fatura atual (10/02-09/03): R$ 800,00      ? Vai vencer em março
```

---

## ? SOLUÇÃO PROPOSTA

### **1. Criar Entidade "Fatura" (`CreditCardInvoice`)**
```
Cada fatura tem:
- Período (ex: 10/01 a 09/02)
- Vencimento (ex: 17/02)
- Valor total
- Status (Aberta, Fechada, Paga, Vencida)
- Lista de transações vinculadas
```

### **2. Worker Fecha Faturas Automaticamente**
```
Todo dia às 08:00:
- Verifica se é dia de fechamento de algum cartão
- Fecha a fatura atual
- Cria nova fatura aberta para próximo período
```

### **3. Modificar "Pagar Fatura"**
```
ANTES: Mostra saldo total do cartão
DEPOIS: Mostra lista de faturas FECHADAS pendentes

Exemplo:
???????????????????????????????????????????
? Faturas Pendentes - Cartão Nubank       ?
???????????????????????????????????????????
? ?? Fatura Jan/2026 - VENCIDA            ?
?    Vencimento: 17/01/2026               ?
?    Valor: R$ 1.200,00                   ?
?    [Pagar] [Ver Detalhes]               ?
???????????????????????????????????????????
? ?? Fatura Fev/2026 - FECHADA            ?
?    Vencimento: 17/02/2026               ?
?    Valor: R$ 800,00                     ?
?    [Pagar] [Ver Detalhes]               ?
???????????????????????????????????????????
```

### **4. Dashboard do Cartão (Nova Página)**
```
Tela com 3 cards principais:
??????????????????????????????????????????????????????????
? Fatura Atual     ? Fatura Fechada   ? Limite          ?
? (Aberta)         ? (A Vencer)       ? Disponível      ?
??????????????????????????????????????????????????????????
? Fecha: 09/03     ? Vence: 17/02     ? Limite: 5.000   ?
? R$ 450,00        ? R$ 800,00        ? Usado: 1.250    ?
? [Ver]            ? [PAGAR]          ? Livre: 3.750    ?
??????????????????????????????????????????????????????????

+ Histórico de faturas (tabela)
+ Gráfico de gastos por mês
```

---

## ?? IMPLEMENTAÇÃO EM FASES

### **MVP (2-3 semanas):**
1. Criar entidade `CreditCardInvoice`
2. Serviço de gestão de faturas
3. Modificar criação de transações (vincular à fatura)
4. Modificar "Pagar Fatura" (mostrar faturas fechadas)

### **Versão Completa (4-6 semanas):**
5. Worker para fechar faturas automaticamente
6. Dashboard do cartão
7. Limite de crédito
8. Alertas e notificações

---

## ?? DECISÕES NECESSÁRIAS

### **1. Migração de Dados Antigos**
```
Transações existentes não têm InvoiceId.

Opção A (Recomendada):
- Criar UMA fatura "Histórico" com tudo antes de hoje
- Marcar como "Paga"
- Novas transações usam sistema de faturas

Opção B:
- Deixar transações antigas sem fatura
- Sistema novo só para transações novas

Opção C:
- Criar faturas retroativas (complexo, trabalhoso)
```

**Sua escolha:** [ ] A  [ ] B  [ ] C

### **2. Limite de Crédito**
```
Adicionar campo CreditLimit no cartão?

Se sim:
- Validar ao criar transação
- Mostrar limite disponível no dashboard
- Alertar quando próximo do limite

Se não:
- Pode adicionar depois
```

**Sua escolha:** [ ] Sim, desde o início  [ ] Não, depois

### **3. Vencimento da Fatura**
```
Quantos dias após o fechamento?
```

**Sua escolha:** [___] dias (padrão: 7 dias)

### **4. Worker - Horário de Fechamento**
```
Faturas devem fechar automaticamente:
- Todo dia às 00:01 (logo após meia-noite)
- OU junto com processamento de recorrências (08:00)
```

**Sua escolha:** [ ] 00:01  [ ] 08:00  [ ] Outro: ____

---

## ?? ESFORÇO vs VALOR

| Item | Esforço | Valor para Usuário | Prioridade |
|------|---------|-------------------|------------|
| Entidade Invoice | 2 dias | ????? | ?? CRÍTICO |
| Serviço de Faturas | 3 dias | ????? | ?? CRÍTICO |
| Modificar "Pagar Fatura" | 2 dias | ????? | ?? CRÍTICO |
| Worker Fechar Faturas | 2 dias | ???? | ?? ALTA |
| Dashboard do Cartão | 4 dias | ???? | ?? ALTA |
| Limite de Crédito | 1 dia | ??? | ?? MÉDIA |
| Histórico/Relatórios | 3 dias | ??? | ?? MÉDIA |
| Notificações | 2 dias | ?? | ? BAIXA |

**Total MVP:** ~7 dias úteis  
**Total Completo:** ~19 dias úteis

---

## ?? PRÓXIMOS PASSOS

**Se aprovar:**
1. Responder as 4 decisões acima
2. Eu crio a branch `feature/credit-card-invoices`
3. Implemento a Fase 1 (Entidade + Migração)
4. Você testa e aprova
5. Continuo para Fase 2...

**Se quiser ajustar:**
1. Me diga o que mudar no plano
2. Eu reviso e crio versão 2.0
3. Voltamos para aprovação

**Se preferir simplificar:**
1. Me diga qual parte é mais importante
2. Eu crio um plano MVP menor
3. Implementamos em 1 semana

---

**Documentação completa:** `docs/CREDIT_CARD_FLOW_ANALYSIS.md`

**Aguardando suas decisões para começar! ??**
