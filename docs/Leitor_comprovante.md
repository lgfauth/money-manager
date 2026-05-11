# MoneyManager — Leitura de Comprovantes via IA
## Prompts para GitHub Copilot

---

## PROMPT 1 — DTO de resultado da análise (Application layer)

Create a C# record called `ReceiptAnalysisResult` in the Application layer under `Features/Receipts/DTOs/`. This is the structured output that Claude Vision will return after reading a receipt image.

Fields:
- `string Description` — merchant name or bill description extracted from the receipt
- `decimal Amount` — total amount paid, always positive
- `DateOnly Date` — transaction date found on the receipt; if not found, use today
- `string TransactionType` — either "expense" or "income"
- `string? CategoryHint` — suggested category name in Portuguese (e.g. "Alimentação", "Transporte", "Contas fixas"); null if unclear
- `string? PaymentMethod` — detected payment method (e.g. "Débito", "Crédito", "PIX", "Boleto"); null if not found
- `int? Installments` — number of installments if the receipt shows a parcelado purchase; null otherwise
- `string? Notes` — any extra relevant info found on the receipt (e.g. establishment address, order number)
- `decimal Confidence` — a value between 0 and 1 representing how confident the extraction was

This is a plain record with no behavior. Follow the naming conventions already used in the project for other DTO records.

---

## PROMPT 2 — Command e Handler (Application layer)

Create a CQRS Command and Handler in `Features/Receipts/Commands/` for analyzing a receipt image.

Command: `AnalyzeReceiptCommand`
- `string FileBase64` — the image content encoded in base64
- `string MimeType` — the MIME type of the file (e.g. "image/jpeg", "image/png", "image/webp")

Handler: `AnalyzeReceiptCommandHandler`
- Inject `IReceiptAnalysisService` (interface to be defined in Domain/Interfaces or Application/Interfaces, consistent with how other service interfaces are declared in this project)
- Call `IReceiptAnalysisService.AnalyzeAsync(command.FileBase64, command.MimeType)` 
- Return `Union<ReceiptAnalysisResult, ReceiptAnalysisError>` following the Union/Result pattern already used in the project
- Define `ReceiptAnalysisError` as a discriminated union with cases: `InvalidFile`, `ExtractionFailed`, `ServiceUnavailable`

No business logic in the handler beyond orchestration. Validation (file size limit 10MB, accepted MIME types: image/jpeg, image/png, image/webp) goes in a FluentValidation validator class `AnalyzeReceiptCommandValidator` in the same folder.

---

## PROMPT 3 — Interface e Service de IA (Infrastructure layer)

Create interface `IReceiptAnalysisService` in the appropriate interfaces folder (follow where other service interfaces live in this project).

Then create `AnthropicReceiptAnalysisService` implementing `IReceiptAnalysisService` in the Infrastructure layer under `Services/AI/`.

The service calls the Anthropic Messages API to extract structured data from a receipt image. Implementation details:

1. Inject `IConfiguration` to read `Anthropic:ApiKey` and `Anthropic:Model` (default to `claude-opus-4-5-20251101`) from appsettings.
2. Inject `HttpClient` (named client "anthropic").
3. The `AnalyzeAsync(string fileBase64, string mimeType)` method builds a request to `https://api.anthropic.com/v1/messages` with:
   - Header `x-api-key` from config
   - Header `anthropic-version: 2023-06-01`
   - Model from config
   - max_tokens: 1024
   - A single user message containing two content blocks:
     a. An image block: `{ "type": "image", "source": { "type": "base64", "media_type": "<mimeType>", "data": "<fileBase64>" } }`
     b. A text block with this exact prompt:

```
Analyze this receipt, bill, or payment confirmation image and extract the following information. Respond ONLY with a valid JSON object, no markdown, no explanation.

{
  "description": "merchant name or bill description",
  "amount": 0.00,
  "date": "YYYY-MM-DD",
  "transactionType": "expense or income",
  "categoryHint": "category in Portuguese or null",
  "paymentMethod": "payment method in Portuguese or null",
  "installments": null or number,
  "notes": "any extra relevant info or null",
  "confidence": 0.0 to 1.0
}

Rules:
- amount is always a positive number
- date format is YYYY-MM-DD; use today if not found
- transactionType is "expense" for purchases/bills, "income" for deposits/refunds
- categoryHint suggestions: Alimentação, Transporte, Saúde, Lazer, Contas fixas, Compras, Educação, Serviços
- confidence reflects how clearly the receipt data was readable
```

4. Parse the JSON response into `ReceiptAnalysisResult`.
5. Handle HTTP errors and JSON parse failures by returning appropriate `ReceiptAnalysisError` cases.

Register the named HttpClient and the service in the DI container in the Infrastructure registration file (follow the pattern already used in the project).

Also add to appsettings.json (and appsettings.Development.json):
```json
"Anthropic": {
  "ApiKey": "",
  "Model": "claude-opus-4-5-20251101"
}
```

---

## PROMPT 4 — Controller (Presentation layer)

Create `ReceiptController` in the Presentation/Controllers folder following the naming and base class conventions already used in the project.

Single endpoint: `POST /api/receipts/analyze`
- Accepts multipart/form-data with one file field named `file`
- Validates that the file is not null and has a supported MIME type (image/jpeg, image/png, image/webp)
- Converts the uploaded file to base64 in memory (no disk write, no storage)
- Dispatches `AnalyzeReceiptCommand` via MediatR (or whatever command dispatcher is already used in the project)
- On success: returns 200 with the `ReceiptAnalysisResult` JSON
- On `InvalidFile`: returns 400 with a problem details response
- On `ExtractionFailed` or `ServiceUnavailable`: returns 502 with a problem details response
- Max file size accepted: 10MB — reject with 400 if exceeded before dispatching

No business logic in the controller. Follow the same response pattern (success/error shape) used in other controllers in the project.

---

## PROMPT 5 — Hook useReceiptAnalysis (Frontend)

Create a custom React hook `useReceiptAnalysis` in `hooks/useReceiptAnalysis.ts`.

The hook manages the full lifecycle of uploading a receipt file and receiving the analysis result.

State it exposes:
- `status: 'idle' | 'uploading' | 'analyzing' | 'success' | 'error'`
- `result: ReceiptAnalysisResult | null`
- `error: string | null`

Type `ReceiptAnalysisResult` (define in `types/receipt.ts`):
```typescript
export interface ReceiptAnalysisResult {
  description: string
  amount: number
  date: string // YYYY-MM-DD
  transactionType: 'expense' | 'income'
  categoryHint: string | null
  paymentMethod: string | null
  installments: number | null
  notes: string | null
  confidence: number
}
```

Methods the hook exposes:
- `analyze(file: File): Promise<void>` — sends the file to `POST /api/receipts/analyze` as multipart/form-data and updates state accordingly
- `reset(): void` — resets all state back to idle

Implementation notes:
- Use the existing API client/fetch wrapper already used in the project (follow the same pattern as other data-fetching hooks)
- Set status to 'uploading' immediately when analyze is called, then 'analyzing' after the request is sent (they can be the same step if the API is fast — just use 'analyzing' for the whole request duration)
- On HTTP error responses, set a user-friendly error message in Portuguese (e.g. "Não foi possível ler o comprovante. Tente uma foto mais nítida.")
- Abort ongoing requests if analyze is called again before completion (use AbortController)

---

## PROMPT 6 — FAB de upload (Frontend)

Create a Client Component `ReceiptFab` in `components/receipts/ReceiptFab.tsx`.

This is a Floating Action Button for capturing or selecting a receipt photo. It is **mobile-only** — render nothing on `md` and larger breakpoints (use `md:hidden` on the wrapping element).

Visual spec:
- Fixed position: bottom-right, `bottom-6 right-6` (or `bottom-24` if there's already a FAB in that corner — check the layout)
- Button shape: circle, using the app's primary color (Mint Green `#00C896`)
- Icon: `Camera` from lucide-react
- Always icon-only — no label text
- Loading state: replace the icon with a spinner and disable the button while `status === 'analyzing'`

Behavior:
- On click: open a hidden `<input type="file" accept="image/jpeg,image/png,image/webp" capture="environment" />` via ref. The `capture="environment"` attribute opens the rear camera directly on mobile browsers instead of the file picker.
- On file selected: call `analyze(file)` from `useReceiptAnalysis`
- On success: open `ReceiptConfirmationModal` passing the `result`
- On error: show a toast notification with the error message (use whatever toast library is already in the project)

Props: none. The component manages its own state via the hook.

---

## PROMPT 7 — Modal de confirmação (Frontend)

Create a Client Component `ReceiptConfirmationModal` in `components/receipts/ReceiptConfirmationModal.tsx`.

This modal shows the data extracted from the receipt, lets the user review and edit it, then creates the transaction. It is **mobile-only** — it should only be rendered when the `ReceiptFab` is visible, which already handles the `md:hidden` constraint. The modal itself should use a bottom-sheet style (slides up from the bottom) rather than a centered dialog, fitting the mobile context better. Follow the bottom-sheet or drawer pattern if one already exists in the project; otherwise implement it as a fixed overlay that covers the full screen from the bottom up.

Props:
```typescript
interface ReceiptConfirmationModalProps {
  isOpen: boolean
  onClose: () => void
  result: ReceiptAnalysisResult
}
```

The modal contains a form with these fields (all pre-filled from `result`, all editable):

1. **Descrição** — text input, pre-filled with `result.description`
2. **Valor** — currency input, pre-filled with `result.amount`
3. **Data** — date picker, pre-filled with `result.date`
4. **Tipo** — toggle/select between "Despesa" and "Receita", pre-filled from `result.transactionType`
5. **Categoria** — select/combobox from the user's existing categories. Pre-select the category whose name best matches `result.categoryHint` (case-insensitive). Fetch categories from the existing categories API endpoint already used in the project.
6. **Conta / Cartão** — if `result.paymentMethod` contains "Crédito" or "crédito", show a card selector (fetch from the credit cards API). Otherwise show an account selector (fetch from the accounts API). The user must select one before confirming.
7. **Parcelas** — number input, visible only when a credit card is selected. Pre-filled with `result.installments ?? 1`.
8. **Observações** — optional textarea, pre-filled with `result.notes ?? ''`

At the bottom:
- A confidence indicator: if `result.confidence < 0.7`, show a yellow warning "Leitura com baixa confiança — revise os dados antes de confirmar"
- "Cancelar" button — closes the modal
- "Confirmar transação" button — submits the form, creating the transaction using the existing transaction creation flow already in the project, then closes the modal and shows a success toast

Form validation: description required, amount > 0, date required, category required, account or card required.

Follow the bottom-sheet/drawer pattern already used in the project for mobile overlays. If none exists, implement as a fixed full-width panel anchored to the bottom with a drag handle indicator at the top, with backdrop overlay behind it.