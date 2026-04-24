# Plano de implementação — Cartão de crédito (MoneyManager)

## Visão geral

Nova funcionalidade de cartão de crédito com domínio completamente separado das contas bancárias. Cada usuário gerencia seus próprios cartões, faturas e transações de forma independente.

---

## Modelo de dados (MongoDB)

### Collection `credit_cards`

| Campo | Tipo | Descrição |
|---|---|---|
| `_id` | ObjectId | |
| `userId` | string | Dono do cartão |
| `name` | string | Ex: "Nubank", "Itaú Platinum" |
| `limit` | decimal | Limite total do cartão |
| `closingDay` | int | Dia do fechamento da fatura (1–28) |
| `billingDueDay` | int | Dia do vencimento da fatura (1–28) |
| `bestPurchaseDay` | int | Melhor dia para compras; default = `closingDay` |
| `createdAt` | datetime | |

> `bestPurchaseDay` é gerado automaticamente igual ao `closingDay` no cadastro. O usuário pode ajustar depois.

---

### Collection `credit_card_invoices`

| Campo | Tipo | Descrição |
|---|---|---|
| `_id` | ObjectId | |
| `userId` | string | |
| `creditCardId` | ObjectId | Referência ao cartão |
| `referenceMonth` | string | Formato `YYYY-MM` (ex: `2025-03`) |
| `closingDate` | date | Data de fechamento desta fatura |
| `dueDate` | date | Data de vencimento |
| `status` | enum | `pending`, `open`, `closed`, `paid`, `overdue` |
| `totalAmount` | decimal | Soma das transações vinculadas |
| `paidAt` | datetime? | Data do pagamento |
| `paidWithAccountId` | ObjectId? | Conta bancária usada no pagamento |
| `paidAmount` | decimal? | Valor pago (pode diferir do total) |
| `createdAt` | datetime | |

---

### Collection `credit_card_transactions`

| Campo | Tipo | Descrição |
|---|---|---|
| `_id` | ObjectId | |
| `userId` | string | |
| `creditCardId` | ObjectId | |
| `invoiceId` | ObjectId | Fatura à qual esta parcela pertence |
| `description` | string | |
| `category` | string | |
| `purchaseDate` | date | Data real da compra |
| `totalAmount` | decimal | Valor total da compra (não da parcela) |
| `installmentAmount` | decimal | Valor desta parcela (`totalAmount / totalInstallments`) |
| `installmentNumber` | int | Número desta parcela (1, 2, 3…) |
| `totalInstallments` | int | Total de parcelas (1 = à vista) |
| `parentTransactionId` | ObjectId? | `null` na parcela 1; aponta para a original nas demais |
| `createdAt` | datetime | |

---

## Regras de negócio

### 1. Cadastro do cartão

- Ao salvar um novo cartão, o backend **cria automaticamente a primeira fatura** com `status: open`.
- `closingDate` e `dueDate` da primeira fatura são calculados com base no mês corrente e nos dias configurados no cartão.
- Se o `closingDay` já passou no mês corrente, a primeira fatura é criada para o **próximo mês**.

### 2. Resolução da fatura corrente

A fatura corrente é sempre a fatura com `status: open` de `referenceMonth` **mais antigo** para aquele cartão. Faturas com `status: pending` não são consideradas correntes.

### 3. Transação à vista (1x)

- Criado um único documento em `credit_card_transactions` vinculado à fatura corrente (`open`).
- `installmentNumber = 1`, `totalInstallments = 1`, `parentTransactionId = null`.

### 4. Transação parcelada (2x a 18x)

#### Checkbox "Entra na fatura corrente"

- **Marcado (padrão):** parcela 1 vai para a fatura corrente (`open`); parcelas 2..N vão para as faturas subsequentes.
- **Desmarcado:** parcela 1 vai para a **próxima** fatura; parcelas 2..N seguem sequencialmente a partir daí.

#### Algoritmo de distribuição

```
Para cada parcela i de 1 até N:
  monthOffset = (i - 1) se checkbox marcado
  monthOffset = i       se checkbox desmarcado

  targetDate = SafeInstallmentDate(purchaseDate, monthOffset)
  fatura = BuscarOuCriarFatura(creditCardId, targetDate)

  Criar transaction com:
    invoiceId = fatura._id
    installmentNumber = i
    totalInstallments = N
    parentTransactionId = transação original (null se i == 1)
```

#### Criação automática de faturas futuras

Faturas criadas automaticamente para abrigar parcelas futuras nascem com `status: pending`. Elas não são a fatura corrente e não aceitam transações manuais até transicionarem para `open`.

#### Ajuste de datas em meses curtos

```csharp
private static DateTime SafeInstallmentDate(DateTime purchaseDate, int monthOffset)
{
    var target = purchaseDate.AddMonths(monthOffset);
    var lastDay = DateTime.DaysInMonth(target.Year, target.Month);
    return new DateTime(target.Year, target.Month, Math.Min(purchaseDate.Day, lastDay));
}
```

**Exemplos:**
- Compra em 30/jan → parcela de fev = 28/fev (ou 29/fev em ano bissexto)
- Compra em 31/mar → parcela de abr = 30/abr
- Compra em 28/fev → parcelas seguintes normalmente

### 5. Transição `pending` → `open`

Uma fatura `pending` transiciona para `open` quando o seu `referenceMonth` se torna o mês corrente. Essa verificação ocorre:

- Via **job agendado diário** que promove faturas cujo `referenceMonth` <= mês atual e `status == pending`.
- Ou via **lazy evaluation** no momento em que o usuário acessa o cartão (como fallback de segurança).

Ao se tornar `open`, a fatura passa a ser a fatura corrente e começa a aceitar novas transações manuais.

### 6. Fechamento de fatura

- Fatura passa de `open` para `closed` quando `closingDate <= hoje`.
- Uma fatura `closed` **congela**: nenhuma transação pode ser adicionada ou removida.
- Qualquer nova compra após o fechamento vai automaticamente para a próxima fatura (`open` ou `pending` → promovida para `open`).
- Ao fechar uma fatura, se não existir nenhuma fatura `open` ou `pending` para o mês seguinte, uma nova fatura `open` é criada automaticamente.

### 7. Pagamento de fatura

- O usuário registra o pagamento informando:
  - Conta bancária debitada (`paidWithAccountId`) — pode ser qualquer conta do usuário, independente do banco do cartão.
  - Valor pago (`paidAmount`).
  - Data do pagamento (`paidAt`).
- Status muda de `closed` ou `overdue` para `paid`.
- O pagamento **gera um lançamento de débito** na conta bancária vinculada, usando a infraestrutura de transações bancárias existente. Essa operação deve ser atômica.

### 8. Fatura vencida

- Uma fatura `closed` com `dueDate < hoje` e `status != paid` é marcada como `overdue`.
- Verificação feita por job agendado diário ou lazy evaluation na leitura.
- Uma fatura `overdue` ainda pode ser paga normalmente → vai para `paid`.

### 9. Ciclo de vida completo da fatura

```
[Cartão cadastrado]          → open   (primeira fatura, criada automaticamente)
[Parcela futura registrada]  → pending (faturas dos meses seguintes)

pending  →  (referenceMonth == mês atual)  →  open
open     →  (closingDate atingida)          →  closed
closed   →  (pagamento registrado)          →  paid
closed   →  (dueDate ultrapassada)          →  overdue
overdue  →  (pagamento registrado)          →  paid
```

---

## Débitos técnicos (fora do escopo desta entrega)

- [ ] Tratamento de datas de fechamento/vencimento que caem em fins de semana ou feriados
- [ ] Estorno de transação em fatura fechada
- [ ] Notificação de fatura próxima do vencimento (push notification via PWA já implementado)
- [ ] Relatório consolidado de gastos por cartão

---

## Estrutura de rotas sugeridas

| Rota | Descrição |
|---|---|
| `GET /cartoes` | Lista de cartões do usuário |
| `POST /cartoes` | Cadastrar novo cartão |
| `GET /cartoes/:id` | Detalhe do cartão |
| `PUT /cartoes/:id` | Editar cartão (nome, bestPurchaseDay, etc.) |
| `GET /cartoes/:id/faturas` | Faturas do cartão |
| `GET /cartoes/:id/faturas/:invoiceId` | Detalhe de uma fatura + transações |
| `POST /cartoes/:id/faturas/:invoiceId/pagar` | Registrar pagamento |
| `GET /transacoes/cartao` | Transações de cartão do usuário |
| `POST /transacoes/cartao` | Nova transação de cartão |
| `DELETE /transacoes/cartao/:id` | Excluir transação (somente em fatura open) |

---

## Estrutura de UI

### Página de cartões (`/cartoes`)

- Cards visuais para cada cartão cadastrado: nome, limite, % utilizado, próximo vencimento.
- Botão "Novo cartão".
- Ao clicar no cartão, navega para a lista de faturas.

### Formulário de cadastro do cartão

Campos: Nome, Limite, Dia de fechamento, Dia de vencimento.
`Melhor dia de compra` exibido como read-only derivado do fechamento, com opção de editar.

### Página de faturas (`/cartoes/:id/faturas`)

- Fatura corrente (`open`) em destaque no topo.
- Lista cronológica reversa das faturas anteriores e futuras.
- Badge de status por fatura:

| Status | Badge |
|---|---|
| `pending` | "Futura" — visual neutro/discreto |
| `open` | "Em aberto" — destaque principal |
| `closed` | "Fechada" — neutro |
| `paid` | "Paga" — verde |
| `overdue` | "Vencida" — vermelho |

- Faturas `pending` são exibidas na lista mas com interação limitada: apenas visualização das parcelas já vinculadas, sem opção de adicionar transações manuais.
- Ao clicar, abre o detalhe com a lista de transações.

### Transações — adição ao formulário existente

Na página de transações, adicionar toggle ou tab **"Bancárias / Cartão"**.

Formulário de nova transação de cartão:
- Cartão (select)
- Descrição
- Categoria
- Data da compra
- Valor total
- Número de parcelas (1–18; se 1, esconde os campos abaixo)
- Checkbox: "Primeira parcela na fatura corrente" (visível somente se parcelas ≥ 2)

---

## Ordem de implementação sugerida

1. **Collections e models** — `CreditCard`, `CreditCardInvoice`, `CreditCardTransaction`
2. **Service de cartão** — CRUD + geração automática da primeira fatura (`open`)
3. **Service de fatura** — busca/criação lazy, transição `pending → open`, fechamento, pagamento, cálculo de `overdue`
4. **Service de transação** — lógica de parcelamento, `SafeInstallmentDate`, distribuição de parcelas, criação de faturas `pending`
5. **Job agendado** — promoção de `pending → open` e marcação de `overdue`
6. **APIs (controllers)** — rotas listadas acima
7. **UI — Página de cartões** — listagem e cadastro
8. **UI — Página de faturas** — listagem com badges de status, detalhe, registrar pagamento
9. **UI — Integração na página de transações** — toggle + formulário estendido