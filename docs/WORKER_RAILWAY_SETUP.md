# MoneyManager Worker - Guia de Configuração no Railway

## ?? Variáveis de Ambiente Obrigatórias

Configure as seguintes variáveis de ambiente no Railway para o serviço do Worker:

### **MongoDB (Banco de Dados)**
```bash
MongoDB__ConnectionString=mongodb+srv://usuario:senha@cluster.mongodb.net/?retryWrites=true&w=majority
MongoDB__DatabaseName=MoneyManager
```
?? **IMPORTANTE:** Use o MESMO valor que está configurado na API!

### **Agendamento (Horário de Execução)**
```bash
Schedule__Hour=8
Schedule__Minute=0
Schedule__TimeZoneId=E. South America Standard Time
Schedule__LoopDelaySeconds=30
```

### **Worker (Timeout)**
```bash
Worker__ExecutionTimeoutMinutes=5
```

---

## ?? Diagnóstico de Problemas

### **Problema 1: Worker não está rodando**

**Sintomas:**
- Nenhum log após 07:41
- Transações recorrentes não são criadas às 08:00

**Causas possíveis:**

1. **Worker não está em execução no Railway**
   - Acesse o Railway Dashboard
   - Vá para o serviço do Worker
   - Verifique se o status está "Active" ou "Healthy"
   - Se estiver "Crashed" ou "Failed", veja os logs de erro

2. **Variáveis de ambiente não configuradas**
   - Vá em Settings ? Variables
   - Confira se TODAS as variáveis acima estão configuradas
   - Clique em "Redeploy" após adicionar variáveis

3. **MongoDB não está acessível**
   - Teste a connection string manualmente
   - Verifique se o IP do Railway está na whitelist do MongoDB Atlas

4. **Fuso horário errado**
   - Railway roda em UTC por padrão
   - 08:00 BRT = 11:00 UTC
   - Confira se `TimeZoneId` está configurado corretamente

### **Problema 2: Worker roda mas não processa**

**Sintomas:**
- Logs mostram "Worker iniciado"
- Mas não há logs de "Iniciando processamento"

**Diagnóstico via logs:**

Procure por estas mensagens no console do Railway:

```
? SUCESSO (deve aparecer):
TransactionSchedulerWorker INICIADO
Agendado para 08:00 (TimeZone: E. South America Standard Time)

? SE APARECER ISTO = Configuração ausente:
System.ComponentModel.DataAnnotations.ValidationException: Options validation failed
```

**Solução:** Configurar todas as variáveis de ambiente listadas acima.

---

## ?? Debug Rápido

### **1. Forçar execução imediata (teste local)**

Altere temporariamente o `appsettings.Development.json`:
```json
{
  "Schedule": {
    "Hour": 0,
    "Minute": 0,
    "LoopDelaySeconds": 10
  }
}
```

Isso faz o worker rodar a cada ~10 segundos (qualquer horário).

### **2. Verificar logs detalhados**

Com a correção aplicada, você deve ver logs assim:

```
2025-02-10 08:00:15|INFO|ScheduledTransactionWorker|========================================
2025-02-10 08:00:15|INFO|ScheduledTransactionWorker|TransactionSchedulerWorker INICIADO
2025-02-10 08:00:15|INFO|ScheduledTransactionWorker|Agendado para 08:00 (TimeZone: E. South America Standard Time)
2025-02-10 08:00:15|INFO|ScheduledTransactionWorker|Loop delay: 30s | Timeout: 5min
2025-02-10 08:00:15|INFO|ScheduledTransactionWorker|========================================
2025-02-10 08:00:15|INFO|ScheduledTransactionWorker|STARTUP EXECUTION: Processando recorrências vencidas imediatamente...
2025-02-10 08:00:15|INFO|ScheduledTransactionWorker|Iniciando processamento em 2025-02-10T11:00:15Z
2025-02-10 08:00:15|INFO|RecurringTransactionsProcessor|Processando recorrências vencidas...
2025-02-10 08:00:16|INFO|RecurringTransactionService|Starting recurring transactions processing for date: 2025-02-10
2025-02-10 08:00:16|INFO|RecurringTransactionService|Found 15 due recurring transactions to process
2025-02-10 08:00:17|INFO|RecurringTransactionService|Processed 3 transaction(s) from recurring rec-123
...
2025-02-10 08:00:20|INFO|ScheduledTransactionWorker|STARTUP EXECUTION: Concluída com sucesso. Aguardando próximo horário agendado.
2025-02-10 08:00:45|DEBUG|ScheduledTransactionWorker|Schedule check: Now=2025-02-10 08:00:45 | NextRun=2025-02-10 08:00:00 | AlreadyRan=False
...
```

**Comportamento:**
1. Worker executa **IMEDIATAMENTE** ao iniciar (processa backlog)
2. Depois aguarda o próximo horário agendado (08:00)
3. A partir daí, executa todo dia às 08:00

---

## ?? Checklist Railway

- [ ] Worker service está "Active/Healthy"
- [ ] Variáveis `MongoDB__ConnectionString` e `MongoDB__DatabaseName` configuradas
- [ ] Variável `Schedule__Hour=8` configurada
- [ ] Variável `Schedule__Minute=0` configurada
- [ ] Variável `Schedule__TimeZoneId=E. South America Standard Time` configurada
- [ ] Variável `Worker__ExecutionTimeoutMinutes=5` configurada
- [ ] Redeploy feito após adicionar variáveis
- [ ] Logs mostram "TransactionSchedulerWorker INICIADO"
- [ ] Logs mostram "STARTUP EXECUTION: Concluída com sucesso"
- [ ] Logs mostram "Schedule check" a cada 30 segundos

---

## ?? Solução Emergencial

Se o worker continuar não funcionando, você pode processar manualmente via MongoDB:

```javascript
// Conecte no MongoDB e execute:
db.recurring_transactions.find({
  isActive: true,
  isDeleted: false,
  nextOccurrenceDate: { $lte: new Date() }
}).forEach(function(rec) {
  print("Recorrência vencida: " + rec.description + " - " + rec.nextOccurrenceDate);
});
```

Depois podemos criar um endpoint temporário para processar manualmente.

---

## ?? Próximos Passos

1. **Verificar variáveis de ambiente no Railway**
2. **Fazer redeploy do worker**
3. **Acompanhar os logs em tempo real**
4. **Me enviar os logs se continuar com problema**

Configurações aplicadas e logs melhorados! Agora você terá muito mais visibilidade do que está acontecendo.
