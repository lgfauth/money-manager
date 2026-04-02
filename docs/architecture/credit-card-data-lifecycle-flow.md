# Credit Card Data Lifecycle and Flow

## 1. Objetivo

Este documento descreve, de forma detalhada, como o sistema trata cartoes de credito no repositorio Money Manager.

Escopo:
- Como os dados entram no sistema
- Como os dados saem do sistema
- Como funciona o fluxo de faturas
- Como funcionam entradas de transacoes
- Como sao tratadas transacoes parceladas
- Quais componentes (Frontend, API, Application, Domain, Infrastructure, Worker) participam

---

## 2. Visao geral da arquitetura

Camadas envolvidas:
- Frontend (Next.js): coleta a entrada do usuario e consome APIs
- Presentation (ASP.NET Core Controllers): endpoints HTTP autenticados
- Application (Services): regras de negocio
- Domain (Entities, Enums, Interfaces): modelo de negocio
- Infrastructure (MongoContext, Repositories, UnitOfWork): persistencia
- Worker: processamento agendado (fechamento de faturas e recorrencias)

Principais servicos registrados:
- `ITransactionService` + `TransactionService`
- `IRecurringTransactionService` + `RecurringTransactionService`
- `ICreditCardInvoiceService` + `CreditCardInvoiceService`

Arquivos-chave:
- `src/MoneyManager.Presentation/Program.cs`
- `src/MoneyManager.Worker/WorkerHost/DependencyInjection/ApplicationServicesExtensions.cs`

---

## 3. Modelo de dados de cartao de credito

### 3.1 Conta de cartao (`Account`)

Arquivo: `src/MoneyManager.Domain/Entities/Account.cs`

Campos importantes:
- `Type` (deve ser `CreditCard`)
- `CreditLimit`: limite total do cartao
- `CommittedCredit`: limite comprometido
- `InvoiceClosingDay`: dia de fechamento da fatura
- `InvoiceDueDayOffset`: dias entre fechamento e vencimento
- `LastInvoiceClosedAt`: data do ultimo fechamento
- `CurrentOpenInvoiceId`: id da fatura atualmente aberta

Enum de tipo de conta:
- Arquivo: `src/MoneyManager.Domain/Enums/AccountType.cs`
- Valor relevante: `CreditCard = 3`

### 3.2 Fatura (`CreditCardInvoice`)

Arquivo: `src/MoneyManager.Domain/Entities/CreditCardInvoice.cs`

Campos importantes:
- `AccountId`, `UserId`
- `PeriodStart`, `PeriodEnd`
- `DueDate`
- `TotalAmount`, `PaidAmount`, `RemainingAmount`
- `Status`
- `ClosedAt`, `PaidAt`
- `ReferenceMonth` (formato `yyyy-MM`)

Enum de status da fatura:
- Arquivo: `src/MoneyManager.Domain/Enums/InvoiceStatus.cs`
- Status: `Open`, `Closed`, `Paid`, `PartiallyPaid`, `Overdue`

### 3.3 Transacao (`Transaction`)

Arquivo: `src/MoneyManager.Domain/Entities/Transaction.cs`

Campos importantes no contexto de cartao:
- `InvoiceId`: vinculo da transacao a uma fatura
- `SkipAccountBalanceImpact`
- `SkipCommittedCreditImpact`
- `SkipCreditLimitValidation`
- `InstallmentGroupId`, `InstallmentNumber`, `InstallmentCount`

---

## 4. Como os dados entram

## 4.1 Entrada de transacao comum

Fluxo:
1. Frontend envia `POST /api/transactions`
2. `TransactionsController` valida request
3. `TransactionService.CreateAsync` aplica regra de negocio
4. Para despesa em cartao, transacao e vinculada a uma fatura
5. Sistema recalcula o total da fatura impactada

Arquivos:
- Frontend hook: `src/MoneyManager.Frontend/src/hooks/use-transactions.ts`
- Controller: `src/MoneyManager.Presentation/Controllers/TransactionsController.cs`
- Service: `src/MoneyManager.Application/Services/TransactionService.cs`

## 4.2 Entrada de compra parcelada

Fluxo:
1. Frontend envia `POST /api/transactions/installment-purchase`
2. `TransactionService.CreateInstallmentPurchaseAsync` valida request
3. Sistema reserva imediatamente o valor total no cartao
4. Pode lancar a primeira parcela na fatura atual (configuravel)
5. Parcelas futuras viram recorrencias tecnicas

Arquivos:
- DTO: `src/MoneyManager.Application/DTOs/Request/CreateTransactionRequestDto.cs`
- Controller: `src/MoneyManager.Presentation/Controllers/TransactionsController.cs`
- Service: `src/MoneyManager.Application/Services/TransactionService.cs`

## 4.3 Entrada de pagamento de fatura

Fluxo:
1. Frontend envia:
   - `POST /api/credit-card-invoices/pay` (pagamento total)
   - `POST /api/credit-card-invoices/pay-partial` (pagamento parcial)
2. `CreditCardInvoiceService` valida valores e contas
3. Atualiza fatura, conta pagadora e conta do cartao
4. Gera transacao de transferencia (conta -> cartao)

Arquivos:
- Hook frontend: `src/MoneyManager.Frontend/src/hooks/use-invoices.ts`
- Controller: `src/MoneyManager.Presentation/Controllers/CreditCardInvoicesController.cs`
- Service: `src/MoneyManager.Application/Services/CreditCardInvoiceService.cs`

---

## 5. Como os dados saem

A saida ocorre principalmente via APIs para telas de dashboard e detalhe de fatura.

Consultas principais:
- Fatura aberta do cartao
- Historico de faturas por cartao
- Faturas vencidas
- Resumo da fatura
- Transacoes da fatura

Arquivos:
- Hooks: `src/MoneyManager.Frontend/src/hooks/use-invoices.ts`
- Dashboard cartao: `src/MoneyManager.Frontend/src/app/(dashboard)/credit-cards/[accountId]/page.tsx`
- Detalhe fatura: `src/MoneyManager.Frontend/src/app/(dashboard)/invoices/[invoiceId]/page.tsx`

Observacao importante de leitura exibida no frontend:
- Fatura atual mostra apenas o periodo aberto
- Limite comprometido pode ser maior por incluir parcelas futuras reservadas

---

## 6. Fluxo de fatura (ciclo de vida)

## 6.1 Criacao/garantia da fatura aberta

Metodo: `GetOrCreateOpenInvoiceAsync`

Comportamento:
- Valida conta e tipo `CreditCard`
- Calcula a referencia esperada (`yyyy-MM`) para o ciclo atual
- Reusa fatura aberta valida
- Se a fatura aberta estiver "stale", normaliza
- Se nao existir fatura esperada, cria nova fatura aberta
- Atualiza `CurrentOpenInvoiceId` na conta

Arquivo:
- `src/MoneyManager.Application/Services/CreditCardInvoiceService.cs`

## 6.2 Determinacao da fatura para uma transacao

Metodo: `DetermineInvoiceForTransactionAsync`

Regra:
- Se a compra ocorreu ate o dia de fechamento do mes: entra na fatura que fecha no mes atual
- Se ocorreu apos o fechamento: entra na fatura que fecha no mes seguinte

Se nao existir fatura da referencia, o sistema cria automaticamente.

Arquivo:
- `src/MoneyManager.Application/Services/CreditCardInvoiceService.cs`

## 6.3 Recalculo do total da fatura

Metodo: `RecalculateInvoiceTotalAsync`

Regra:
- Soma transacoes elegiveis da fatura
- Elegivel = `Type == Expense`, `InvoiceId` igual, `IsDeleted == false`
- Atualiza `TotalAmount` e `RemainingAmount`

Arquivo:
- `src/MoneyManager.Application/Services/CreditCardInvoiceService.cs`

## 6.4 Fechamento de fatura

### Fechamento manual
- Endpoint: `POST /api/credit-card-invoices/{invoiceId}/close`
- Recalcula total
- Muda status para `Closed`
- Cria nova fatura aberta
- Atualiza `LastInvoiceClosedAt` e `CurrentOpenInvoiceId`

### Fechamento automatico
- Worker executa diariamente no horario configurado
- Para cada cartao: se `today.Day == InvoiceClosingDay`, fecha a fatura aberta
- Cria nova fatura aberta para o proximo ciclo

Arquivos:
- `src/MoneyManager.Application/Services/CreditCardInvoiceService.cs`
- `src/MoneyManager.Worker/WorkerHost/Services/InvoiceClosureWorker.cs`
- `src/MoneyManager.Worker/WorkerHost/Services/InvoiceClosureProcessor.cs`
- `src/MoneyManager.Worker/WorkerHost/Options/InvoiceClosureScheduleOptions.cs`
- `src/MoneyManager.Worker/appsettings.json`

## 6.5 Pagamento de fatura

Metodo interno: `ProcessPaymentAsync`

Regras:
- Conta pagadora nao pode ser cartao
- Deve haver saldo suficiente na conta pagadora
- Pagamento total exige valor exato do `RemainingAmount`
- Pagamento parcial exige valor > 0 e <= `RemainingAmount`

Efeitos:
- Fatura: atualiza `PaidAmount`, `RemainingAmount`, `Status`, `PaidAt`
- Conta pagadora: debita saldo
- Conta cartao: reduz divida e reduz `CommittedCredit`
- Gera transacao de `Transfer`

Arquivo:
- `src/MoneyManager.Application/Services/CreditCardInvoiceService.cs`

---

## 7. Fluxo de entrada de transacoes e impacto financeiro

## 7.1 Transacao comum

Metodo: `CreateAsync`

Principais passos:
- Idempotencia por `ClientRequestId` (evita duplicidade)
- Valida conta
- Se despesa em cartao e validacao habilitada: valida limite
- Aplica impacto no saldo (`CalculateBalanceImpact`)
- Atualiza `CommittedCredit` quando aplicavel
- Se for despesa em cartao: vincula `InvoiceId`
- Persiste e recalcula fatura

Arquivo:
- `src/MoneyManager.Application/Services/TransactionService.cs`

## 7.2 Atualizacao e exclusao de transacao

Update:
- Reverte impacto antigo (`RevertTransactionImpact`)
- Aplica novo impacto (`ApplyTransactionImpact`)
- Reavalia vinculo de fatura
- Recalcula faturas envolvidas (antiga e nova)

Delete (soft delete):
- Reverte impacto
- Marca `IsDeleted = true`
- Recalcula fatura, se houver `InvoiceId`

Arquivo:
- `src/MoneyManager.Application/Services/TransactionService.cs`

---

## 8. Tratamento de transacoes parceladas

## 8.1 Regras principais

Metodo: `CreateInstallmentPurchaseAsync`

Regras:
- Minimo 2 parcelas
- Valor total > 0
- Tipo deve ser `Expense`
- Conta deve ser `CreditCard`
- Idempotencia por `ClientRequestId` (transacoes e agendas)

## 8.2 Reserva integral no momento da compra

Ao criar o parcelamento:
- O sistema valida limite sobre o valor total
- Aplica imediatamente impacto total no cartao (`Balance` e `CommittedCredit`)

Consequencia:
- O limite comprometido passa a refletir toda a compra parcelada desde o inicio

## 8.3 Distribuicao das parcelas

Metodos:
- `CalculateInstallmentAmounts` (arredondamento e ajuste final)
- `BuildInstallmentDescription` (`descricao (n/total)`)

Opcao `FirstInstallmentInCurrentInvoice`:
- `true`: primeira parcela e criada ja na transacao atual
- `false`: todas as parcelas vao para agenda recorrente futura

## 8.4 Parcelas futuras como recorrencia tecnica

Para parcelas futuras, o sistema cria `RecurringTransaction` com:
- `IsInstallmentSchedule = true`
- `RemainingOccurrences = 1` por item
- Flags `Skip...` ativadas para nao duplicar impacto financeiro

Data de postagem:
- `CalculateInstallmentPostingDate`
- Regra: dia seguinte ao fechamento (`closingDay + 1`, com ajustes de calendario)

Arquivo:
- `src/MoneyManager.Application/Services/TransactionService.cs`

## 8.5 Execucao das parcelas no Worker

Processamento:
- Worker de recorrencias encontra ocorrencias vencidas
- Cria transacao real via `TransactionService.CreateAsync`
- Atualiza proxima ocorrencia e encerra quando necessario

Arquivo:
- `src/MoneyManager.Application/Services/RecurringTransactionService.cs`
- `src/MoneyManager.Worker/WorkerHost/Services/RecurringTransactionsProcessor.cs`
- `src/MoneyManager.Worker/WorkerHost/Services/ScheduledTransactionWorker.cs`

---

## 9. Persistencia, colecoes e consultas

Colecoes Mongo relevantes:
- `accounts`
- `transactions`
- `recurring_transactions`
- `credit_card_invoices`

Criacao e indices de faturas:
- `src/MoneyManager.Infrastructure/Data/MongoContext.cs`

Repositorio de faturas:
- `GetOpenInvoiceByAccountIdAsync`
- `GetByAccountIdAsync`
- `GetClosedUnpaidInvoicesAsync`
- `GetOverdueInvoicesAsync`
- `GetByReferenceMonthAsync`

Arquivo:
- `src/MoneyManager.Infrastructure/Repositories/CreditCardInvoiceRepository.cs`

---

## 10. Rotinas administrativas e reconciliacao

Endpoints administrativos:
- Migracao historica: `POST /api/admin/migrate-credit-card-invoices`
- Recalculo de faturas: `POST /api/admin/recalculate-invoices`
- Reconciliacao de cartoes: `POST /api/admin/reconcile-credit-cards`
- Criar faturas abertas faltantes: `POST /api/admin/create-missing-open-invoices`

Arquivos:
- `src/MoneyManager.Presentation/Controllers/AdminController.cs`
- `src/MoneyManager.Application/Services/CreditCardInvoiceService.cs`

Reconciliacao recompone `CommittedCredit` usando:
- faturas nao pagas (remaining)
- parcelas futuras agendadas
- comparacao com `abs(balance)` para consistencia

---

## 11. Leitura operacional do ciclo de vida do cartao

Resumo em sequencia:
1. Usuario cria conta de cartao com limite e regras de ciclo.
2. Sistema garante fatura aberta corrente.
3. Compras entram como transacoes, validam limite e sao vinculadas a fatura correta.
4. Total da fatura e recalculado a cada mudanca relevante.
5. No dia de fechamento, fatura e fechada e nova fatura aberta e criada.
6. Pagamentos parciais/totais atualizam fatura, contas e comprometimento.
7. Parcelamentos reservam limite integral no ato e distribuem parcelas ao longo dos ciclos.
8. Worker materializa parcelas futuras no tempo correto.

Resultado:
- O sistema separa claramente "fatura do periodo" de "limite comprometido total".
- Isso explica por que o comprometido pode ser maior que a fatura atual quando ha parcelas futuras.

---

## 12. Pontos de atencao para analise de fluxo

- O valor comprometido pode divergir da leitura intuitiva de divida de fatura (por desenho), pois inclui reservas futuras.
- Flags `SkipAccountBalanceImpact`, `SkipCommittedCreditImpact`, `SkipCreditLimitValidation` sao criticas para evitar dupla contabilizacao em parcelamentos.
- A associacao transacao->fatura depende da data da compra versus dia de fechamento.
- Recalculos de fatura sao essenciais em update/delete para manter integridade.
- Workflows de migracao e reconciliacao existem para correcao de legado e consistencia.
