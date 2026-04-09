# Fechamento Manual de Fatura e Pagamento

**Objetivo:** Permitir que o usuário feche manualmente uma fatura ainda aberta quando o fechamento automático não ocorreu, e pague a fatura fechada debitando de uma conta que não seja cartão de crédito.

---

## Contexto

O worker `InvoiceClosureWorker` roda diariamente e verifica se o dia do mês bate com o `InvoiceClosingDay` do cartão. Pode ocorrer uma janela em que a data de fechamento passou mas o worker ainda não rodou (ex: fora do horário configurado, falha temporária). O usuário precisa de um mecanismo manual de fallback.

A funcionalidade de **fechar fatura** via API (`POST /api/credit-card-invoices/{invoiceId}/close`) já existe no backend (`CloseInvoiceAsync`). O que está faltando é:
1. Um botão de fechamento manual na UI (tela de detalhes da fatura).
2. Um hook React Query para disparar a chamada.
3. Confirmação visual antes de executar a ação.
4. Exibição do botão de pagamento após o fechamento.

---

## Critérios de Exibição do Botão de Fechamento Manual

O botão **"Fechar fatura manualmente e abrir novo período de fatura"** deve ser exibido quando:
- `invoice.status === InvoiceStatus.Open`
- A data atual é **igual ou posterior** a `invoice.periodEnd` (ou seja, o período de fechamento já chegou)

## Fluxo Completo

```
Fatura Open (período vencido)
  → botão "Fechar fatura manualmente..."
    → confirm dialog
      → POST /api/credit-card-invoices/{invoiceId}/close
        → fatura passa para status Closed
          → nova fatura Open é criada pelo backend
            → botão "Pagar Fatura" aparece (já existe na UI)
```

---

## Fases de Implementação

### Fase 1 — Hook `useCloseInvoice` no Frontend

**Arquivo:** `src/Frontends/MoneyManager.Web/src/hooks/use-invoices.ts`

Adicionar o hook de mutação que chama o endpoint já existente:

```ts
export function useCloseInvoice() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (invoiceId: string) =>
      apiClient.post<void>(`/api/credit-card-invoices/${invoiceId}/close`, {}),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: queryKeys.accounts });
      qc.invalidateQueries({ queryKey: ["invoices"] });
      qc.invalidateQueries({ queryKey: ["dashboard"] });
      toast.success("Fatura fechada com sucesso. Novo período iniciado.");
    },
    onError: (error) =>
      toast.error(getApiErrorMessage(error, "Erro ao fechar fatura")),
  });
}
```

---

### Fase 2 — Botão de Fechamento Manual na Página de Detalhes da Fatura

**Arquivo:** `src/Frontends/MoneyManager.Web/src/app/(dashboard)/invoices/[invoiceId]/page.tsx`

**Lógica de exibição:**
```ts
const canManualClose =
  invoice.status === InvoiceStatus.Open &&
  new Date() >= new Date(invoice.periodEnd);
```

**Confirmação:** usar o componente `Dialog` já importado na página — abrir um dialog de confirmação antes de executar.

**Posicionamento:** no `PageHeader`, ao lado (ou abaixo) do botão "Pagar Fatura" existente.

**Texto do botão:** `"Fechar fatura manualmente e abrir novo período de fatura"`

**Ícone sugerido:** `LockKeyhole` ou `Lock` do lucide-react.

---

### Fase 3 — Botão de Fechamento Manual na Página do Cartão de Crédito

**Arquivo:** `src/Frontends/MoneyManager.Web/src/app/(dashboard)/credit-cards/[accountId]/page.tsx`

Ao exibir o `InvoiceCard` da fatura aberta, verificar se a data de fechamento passou. Se sim, exibir um alerta ou botão de fechamento manual junto ao card.

**Alert de aviso sugerido** (usando o componente `Alert` já presente):
```
⚠️ A data de fechamento desta fatura já passou. Feche manualmente se necessário.
[Fechar Fatura Manualmente]
```

O botão dispara o mesmo hook `useCloseInvoice`.

---

### Fase 4 — Botão de Pagamento na Fatura Recém-Fechada

O botão de pagamento já está implementado. Ele aparece quando:
```ts
const canPay =
  invoice.status !== InvoiceStatus.Paid &&
  invoice.status !== InvoiceStatus.Open &&
  invoice.remainingAmount > 0;
```

Após o fechamento manual (Fase 2/3), a invalidação de queries fará a UI recarregar a fatura com `status = Closed`, e o botão "Pagar Fatura" surgirá automaticamente.

O modal de pagamento (`InvoicePaymentModal` / dialog inline da página) já filtra contas `AccountType !== CreditCard`:
```ts
const debitAccounts = accounts?.filter(a => a.type !== AccountType.CreditCard);
```

**Nenhuma mudança de backend é necessária.**

---

## Arquivos a Modificar

| Arquivo | Mudança |
|---|---|
| `src/hooks/use-invoices.ts` | Adicionar `useCloseInvoice` |
| `src/app/(dashboard)/invoices/[invoiceId]/page.tsx` | Botão + dialog de confirmação de fechamento manual |
| `src/app/(dashboard)/credit-cards/[accountId]/page.tsx` | Alert + botão quando período da fatura aberta expirou |

## Arquivos NÃO modificados (backend já suporta tudo)

- `CreditCardInvoicesController.cs` — endpoint `/close` já existe
- `CreditCardInvoiceService.cs` — `CloseInvoiceAsync` já existe
- `CreditCardInvoice.cs` — entidade sem mudanças
- Worker — sem mudanças

---

## Notas de UX

- O botão de fechamento manual deve ter tom de **aviso** (variant `"outline"` com ícone de cadeado), não de ação destrutiva.
- O dialog de confirmação deve explicar que: ao fechar, novas transações serão lançadas na próxima fatura.
- Após fechamento, o toast de sucesso já orienta o usuário ("Novo período iniciado").
