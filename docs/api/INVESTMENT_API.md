# ?? API de Investimentos - Documentação

## Base URL

```
Production: https://money-manager-api.up.railway.app
Development: https://localhost:5001
```

---

## ?? Autenticação

Todos os endpoints requerem autenticação via JWT.

### Header Obrigatório

```http
Authorization: Bearer {your_jwt_token}
```

### Obtendo Token

```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "usuario@exemplo.com",
  "password": "sua-senha"
}
```

**Response 200 OK:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "user_123",
    "email": "usuario@exemplo.com",
    "name": "João Silva"
  }
}
```

---

## ?? Endpoints - InvestmentAssets

### 1. Listar Todos os Ativos

```http
GET /api/investment-assets
```

**Headers:**
```
Authorization: Bearer {token}
```

**Response 200 OK:**
```json
[
  {
    "id": "64a1b2c3d4e5f6789012345",
    "userId": "user_123",
    "accountId": "acc_456",
    "assetType": 0,
    "name": "Petrobras PN",
    "ticker": "PETR4",
    "quantity": 100.0,
    "averagePurchasePrice": 32.50,
    "currentPrice": 35.00,
    "totalInvested": 3250.00,
    "currentValue": 3500.00,
    "profitLoss": 250.00,
    "profitLossPercentage": 7.69,
    "lastPriceUpdate": "2025-02-13T15:30:00Z",
    "notes": "Primeira compra de ações",
    "createdAt": "2025-01-15T10:00:00Z",
    "updatedAt": "2025-02-13T15:30:00Z"
  }
]
```

**Códigos de Status:**
- `200 OK` - Sucesso
- `401 Unauthorized` - Token inválido/expirado
- `500 Internal Server Error` - Erro no servidor

---

### 2. Obter Ativo por ID

```http
GET /api/investment-assets/{id}
```

**Parameters:**
- `id` (path) - ID do ativo

**Response 200 OK:**
```json
{
  "id": "64a1b2c3d4e5f6789012345",
  "userId": "user_123",
  "accountId": "acc_456",
  "assetType": 0,
  "name": "Petrobras PN",
  "ticker": "PETR4",
  "quantity": 100.0,
  "averagePurchasePrice": 32.50,
  "currentPrice": 35.00,
  "totalInvested": 3250.00,
  "currentValue": 3500.00,
  "profitLoss": 250.00,
  "profitLossPercentage": 7.69,
  "lastPriceUpdate": "2025-02-13T15:30:00Z",
  "notes": "Primeira compra",
  "createdAt": "2025-01-15T10:00:00Z",
  "updatedAt": "2025-02-13T15:30:00Z"
}
```

**Códigos de Status:**
- `200 OK` - Sucesso
- `404 Not Found` - Ativo não encontrado
- `401 Unauthorized` - Token inválido
- `403 Forbidden` - Ativo pertence a outro usuário

---

### 3. Criar Novo Ativo

```http
POST /api/investment-assets
Content-Type: application/json
```

**Request Body:**
```json
{
  "accountId": "acc_456",
  "assetType": 0,
  "name": "Petrobras PN",
  "ticker": "PETR4",
  "initialQuantity": 100.0,
  "initialPrice": 32.50,
  "initialFees": 10.00,
  "notes": "Primeira compra de ações"
}
```

**Campos:**
- `accountId` (obrigatório) - ID da conta de investimento
- `assetType` (obrigatório) - Tipo do ativo (enum)
  - `0` = Stock (Ações)
  - `1` = FixedIncome (Renda Fixa)
  - `2` = RealEstate (FIIs)
  - `3` = Crypto (Criptomoedas)
  - `4` = Fund (Fundos)
  - `5` = ETF
  - `6` = Other (Outros)
- `name` (obrigatório) - Nome do ativo
- `ticker` (opcional) - Código do ativo para cotações automáticas
- `initialQuantity` (opcional, default: 0) - Quantidade inicial
- `initialPrice` (opcional, default: 0) - Preço inicial
- `initialFees` (opcional, default: 0) - Taxas da compra inicial
- `notes` (opcional) - Observações

**Response 201 Created:**
```json
{
  "id": "64a1b2c3d4e5f6789012345",
  "userId": "user_123",
  "accountId": "acc_456",
  "assetType": 0,
  "name": "Petrobras PN",
  "ticker": "PETR4",
  "quantity": 100.0,
  "averagePurchasePrice": 32.60,
  "currentPrice": 32.50,
  "totalInvested": 3260.00,
  "currentValue": 3250.00,
  "profitLoss": -10.00,
  "profitLossPercentage": -0.31,
  "lastPriceUpdate": "2025-02-13T10:00:00Z",
  "notes": "Primeira compra de ações",
  "createdAt": "2025-02-13T10:00:00Z",
  "updatedAt": "2025-02-13T10:00:00Z"
}
```

**Validações:**
- Conta deve existir e pertencer ao usuário
- Conta deve ser do tipo "Investment"
- Nome não pode ser vazio
- Quantidade e preço não podem ser negativos

**Códigos de Status:**
- `201 Created` - Ativo criado com sucesso
- `400 Bad Request` - Validação falhou
- `404 Not Found` - Conta não encontrada
- `401 Unauthorized` - Token inválido

---

### 4. Atualizar Ativo

```http
PUT /api/investment-assets/{id}
Content-Type: application/json
```

**Request Body:**
```json
{
  "name": "Petrobras PN - Atualizado",
  "ticker": "PETR4",
  "notes": "Estratégia de longo prazo"
}
```

**Response 200 OK:**
```json
{
  "id": "64a1b2c3d4e5f6789012345",
  "name": "Petrobras PN - Atualizado",
  "ticker": "PETR4",
  "notes": "Estratégia de longo prazo",
  ...
}
```

**Códigos de Status:**
- `200 OK` - Atualizado com sucesso
- `404 Not Found` - Ativo não encontrado
- `400 Bad Request` - Validação falhou

---

### 5. Deletar Ativo

```http
DELETE /api/investment-assets/{id}
```

**Response 204 No Content**

> **Nota:** Soft delete - o registro não é removido do banco, apenas marcado como deletado.

**Códigos de Status:**
- `204 No Content` - Deletado com sucesso
- `404 Not Found` - Ativo não encontrado

---

### 6. Comprar Ativo

```http
POST /api/investment-assets/{id}/buy
Content-Type: application/json
```

**Request Body:**
```json
{
  "quantity": 50.0,
  "price": 35.00,
  "fees": 5.00,
  "date": "2025-02-13T10:00:00Z",
  "description": "Aporte mensal"
}
```

**Campos:**
- `quantity` (obrigatório) - Quantidade a comprar
- `price` (obrigatório) - Preço unitário
- `fees` (opcional, default: 0) - Taxas (corretagem, emolumentos)
- `date` (obrigatório) - Data da operação
- `description` (opcional) - Descrição da operação

**Response 200 OK:**
```json
{
  "id": "64a1b2c3d4e5f6789012345",
  "quantity": 150.0,
  "averagePurchasePrice": 33.50,
  "totalInvested": 5025.00,
  "currentPrice": 35.00,
  "currentValue": 5250.00,
  "profitLoss": 225.00,
  "profitLossPercentage": 4.48,
  ...
}
```

**O que acontece:**
1. ? Calcula novo preço médio ponderado
2. ? Atualiza quantidade total
3. ? Cria `InvestmentTransaction` (tipo Buy)
4. ? Cria `Transaction` regular (tipo InvestmentBuy)
5. ? Debita saldo da conta: `-R$ 1.755,00`
6. ? Atualiza lucro/prejuízo

**Cálculo do Preço Médio:**
```
Preço Médio Anterior: R$ 32,50 (100 ações)
Nova Compra: 50 ações @ R$ 35,00 + R$ 5,00 taxas = R$ 1.755,00

Novo Preço Médio = (R$ 3.250,00 + R$ 1.755,00) / (100 + 50)
                 = R$ 5.005,00 / 150
                 = R$ 33,50
```

**Códigos de Status:**
- `200 OK` - Compra registrada
- `404 Not Found` - Ativo não encontrado
- `400 Bad Request` - Validação falhou (quantidade/preço inválidos)
- `402 Payment Required` - Saldo insuficiente na conta

---

### 7. Vender Ativo

```http
POST /api/investment-assets/{id}/sell
Content-Type: application/json
```

**Request Body:**
```json
{
  "quantity": 50.0,
  "price": 38.00,
  "fees": 5.00,
  "date": "2025-02-13T14:00:00Z",
  "description": "Realização de lucro"
}
```

**Response 200 OK:**
```json
{
  "id": "64a1b2c3d4e5f6789012345",
  "quantity": 100.0,
  "averagePurchasePrice": 33.50,
  "totalInvested": 3350.00,
  "currentPrice": 38.00,
  "currentValue": 3800.00,
  "profitLoss": 450.00,
  "profitLossPercentage": 13.43,
  ...
}
```

**O que acontece:**
1. ? Valida quantidade disponível
2. ? Calcula lucro/prejuízo da venda
3. ? Reduz quantidade (preço médio não muda!)
4. ? Cria `InvestmentTransaction` (tipo Sell)
5. ? Cria `Transaction` regular (tipo InvestmentSell)
6. ? Credita saldo da conta: `+R$ 1.895,00`

**Cálculo do Lucro:**
```
Quantidade Vendida: 50
Preço de Venda: R$ 38,00
Preço Médio: R$ 33,50
Taxas: R$ 5,00

Lucro = (R$ 38,00 - R$ 33,50) × 50 - R$ 5,00
      = R$ 4,50 × 50 - R$ 5,00
      = R$ 225,00 - R$ 5,00
      = R$ 220,00
```

**Códigos de Status:**
- `200 OK` - Venda registrada
- `400 Bad Request` - Quantidade insuficiente ou validação falhou

---

### 8. Ajustar Preço

```http
POST /api/investment-assets/{id}/adjust-price
Content-Type: application/json
```

**Request Body:**
```json
{
  "newPrice": 36.50,
  "date": "2025-02-13T15:30:00Z"
}
```

**Response 200 OK:**
```json
{
  "id": "64a1b2c3d4e5f6789012345",
  "currentPrice": 36.50,
  "currentValue": 3650.00,
  "profitLoss": 300.00,
  "profitLossPercentage": 8.96,
  "lastPriceUpdate": "2025-02-13T15:30:00Z",
  ...
}
```

**O que acontece:**
1. ? Atualiza `CurrentPrice`
2. ? Recalcula `CurrentValue` e `ProfitLoss`
3. ? Cria `InvestmentTransaction` (tipo MarketAdjustment)
4. ? Atualiza `LastPriceUpdate`

**Códigos de Status:**
- `200 OK` - Preço ajustado
- `400 Bad Request` - Preço negativo

---

### 9. Obter Resumo (Summary)

```http
GET /api/investment-assets/summary
```

**Response 200 OK:**
```json
{
  "totalInvested": 50000.00,
  "currentValue": 55000.00,
  "totalProfitLoss": 5000.00,
  "totalProfitLossPercentage": 10.00,
  "totalAssets": 12,
  "totalYields": 1500.00,
  "assetsByType": [
    {
      "assetType": 0,
      "assetTypeName": "Stock",
      "count": 5,
      "totalInvested": 30000.00,
      "currentValue": 33000.00,
      "profitLoss": 3000.00,
      "profitLossPercentage": 10.00,
      "portfolioPercentage": 60.00
    },
    {
      "assetType": 2,
      "assetTypeName": "RealEstate",
      "count": 3,
      "totalInvested": 15000.00,
      "currentValue": 16500.00,
      "profitLoss": 1500.00,
      "profitLossPercentage": 10.00,
      "portfolioPercentage": 30.00
    }
  ],
  "topPerformers": [
    {
      "assetId": "asset_1",
      "assetName": "Magazine Luiza",
      "assetTicker": "MGLU3",
      "assetType": 0,
      "totalInvested": 5000.00,
      "currentValue": 7500.00,
      "profitLoss": 2500.00,
      "profitLossPercentage": 50.00
    }
  ],
  "worstPerformers": [
    {
      "assetId": "asset_5",
      "assetName": "IRB Brasil",
      "assetTicker": "IRBR3",
      "assetType": 0,
      "totalInvested": 10000.00,
      "currentValue": 7000.00,
      "profitLoss": -3000.00,
      "profitLossPercentage": -30.00
    }
  ]
}
```

**Códigos de Status:**
- `200 OK` - Sucesso
- `401 Unauthorized` - Token inválido

---

### 10. Atualizar Preços Manualmente

```http
POST /api/investment-assets/update-prices
```

**Response 200 OK:**
```json
{
  "message": "Preços atualizados com sucesso",
  "timestamp": "2025-02-13T15:35:00Z",
  "total": 14,
  "updated": 12,
  "skipped": 2,
  "errors": 0,
  "details": [
    {
      "ticker": "PETR4",
      "oldPrice": 35.00,
      "newPrice": 36.50,
      "change": 4.29
    },
    {
      "ticker": "VALE3",
      "oldPrice": 68.50,
      "newPrice": 69.00,
      "change": 0.73
    }
  ]
}
```

**Códigos de Status:**
- `200 OK` - Atualização concluída
- `503 Service Unavailable` - API Brapi indisponível

---

## ?? Endpoints - InvestmentTransactions

### 1. Listar Transações

```http
GET /api/investment-transactions?startDate=2025-01-01&endDate=2025-12-31
```

**Query Parameters:**
- `startDate` (opcional) - Data inicial (formato: YYYY-MM-DD)
- `endDate` (opcional) - Data final

**Response 200 OK:**
```json
[
  {
    "id": "trans_123",
    "userId": "user_456",
    "assetId": "asset_789",
    "assetName": "Petrobras PN",
    "assetTicker": "PETR4",
    "accountId": "acc_101",
    "transactionType": 0,
    "transactionTypeName": "Buy",
    "quantity": 100.0,
    "price": 32.50,
    "totalAmount": 3260.00,
    "fees": 10.00,
    "date": "2025-01-15T10:00:00Z",
    "description": "Compra inicial PETR4",
    "linkedTransactionId": "regular_trans_456",
    "createdAt": "2025-01-15T10:00:00Z"
  }
]
```

**Transaction Types:**
- `0` = Buy (Compra)
- `1` = Sell (Venda)
- `2` = Dividend (Dividendo)
- `3` = Interest (Juros)
- `4` = YieldPayment (Rendimento/Aluguel)
- `5` = MarketAdjustment (Ajuste de Preço)
- `6` = Fee (Taxa)

---

### 2. Transações por Ativo

```http
GET /api/investment-transactions/asset/{assetId}
```

**Response 200 OK:**
```json
[
  {
    "id": "trans_123",
    "transactionType": 0,
    "quantity": 100.0,
    "price": 32.50,
    "totalAmount": 3260.00,
    "date": "2025-01-15T10:00:00Z",
    "description": "Compra inicial"
  },
  {
    "id": "trans_124",
    "transactionType": 2,
    "quantity": 0,
    "price": 150.00,
    "totalAmount": 150.00,
    "date": "2025-02-10T00:00:00Z",
    "description": "Dividendos PETR4"
  }
]
```

---

### 3. Registrar Rendimento

```http
POST /api/investment-transactions/yield
Content-Type: application/json
```

**Request Body:**
```json
{
  "assetId": "asset_789",
  "amount": 150.00,
  "yieldType": 2,
  "date": "2025-02-10T00:00:00Z",
  "description": "Dividendos PETR4 - Referência 2024"
}
```

**Campos:**
- `assetId` (obrigatório) - ID do ativo
- `amount` (obrigatório) - Valor líquido recebido
- `yieldType` (obrigatório) - Tipo de rendimento
  - `2` = Dividend
  - `3` = Interest
  - `4` = YieldPayment (Aluguel de FII)
- `date` (obrigatório) - Data do recebimento
- `description` (opcional) - Descrição

**Response 200 OK:**
```json
{
  "id": "trans_125",
  "assetId": "asset_789",
  "assetName": "Petrobras PN",
  "transactionType": 2,
  "amount": 150.00,
  "date": "2025-02-10T00:00:00Z",
  "description": "Dividendos PETR4 - Referência 2024"
}
```

**O que acontece:**
1. ? Cria `InvestmentTransaction` (tipo Dividend/Interest/YieldPayment)
2. ? Cria `Transaction` regular (tipo InvestmentYield)
3. ? Credita saldo da conta: `+R$ 150,00`

---

## ?? Endpoints - InvestmentReports

### 1. Relatório de Vendas (IR)

```http
GET /api/investment-reports/sales/{year}
```

**Parameters:**
- `year` (path) - Ano (ex: 2025)

**Response 200 OK:**
```json
{
  "year": 2025,
  "sales": [
    {
      "date": "2025-02-13",
      "assetId": "asset_789",
      "assetName": "Petrobras PN",
      "ticker": "PETR4",
      "assetType": "Stock",
      "quantity": 50.0,
      "averagePrice": 33.50,
      "salePrice": 38.00,
      "grossAmount": 1900.00,
      "fees": 5.00,
      "netAmount": 1895.00,
      "profitLoss": 220.00,
      "taxDue": 33.00
    }
  ],
  "summary": {
    "totalSales": 5,
    "totalGrossAmount": 50000.00,
    "totalFees": 150.00,
    "totalNetAmount": 49850.00,
    "totalProfit": 5000.00,
    "totalLoss": 0.00,
    "netProfitLoss": 5000.00,
    "totalTaxDue": 750.00
  }
}
```

**Nota:** IR de 15% sobre lucro (estimativa simplificada).

---

### 2. Relatório de Rendimentos

```http
GET /api/investment-reports/yields/{year}
```

**Response 200 OK:**
```json
{
  "year": 2025,
  "yields": [
    {
      "month": 1,
      "date": "2025-01-10",
      "assetId": "asset_789",
      "assetName": "Petrobras PN",
      "ticker": "PETR4",
      "yieldType": "Dividend",
      "amount": 120.00
    },
    {
      "month": 2,
      "date": "2025-02-10",
      "assetId": "asset_789",
      "assetName": "Petrobras PN",
      "ticker": "PETR4",
      "yieldType": "Dividend",
      "amount": 150.00
    }
  ],
  "summary": {
    "totalYields": 1500.00,
    "totalDividends": 800.00,
    "totalInterest": 500.00,
    "totalRentYields": 200.00,
    "byMonth": [
      { "month": 1, "total": 120.00 },
      { "month": 2, "total": 150.00 }
    ]
  }
}
```

---

### 3. Extrato Consolidado

```http
GET /api/investment-reports/consolidated?start=2025-01-01&end=2025-12-31
```

**Query Parameters:**
- `start` (obrigatório) - Data inicial (YYYY-MM-DD)
- `end` (obrigatório) - Data final

**Response 200 OK:**
```json
{
  "startDate": "2025-01-01",
  "endDate": "2025-12-31",
  "summary": {
    "totalInvested": 50000.00,
    "currentValue": 55000.00,
    "realizedProfitLoss": 500.00,
    "unrealizedProfitLoss": 4500.00,
    "totalYields": 1500.00,
    "totalFees": 150.00
  },
  "transactions": [
    {
      "date": "2025-01-15",
      "type": "Buy",
      "asset": "PETR4",
      "quantity": 100.0,
      "price": 32.50,
      "amount": -3260.00,
      "description": "Compra inicial"
    }
  ]
}
```

---

## ?? Códigos de Erro

### 400 Bad Request
```json
{
  "message": "Erro de validação",
  "errors": {
    "Quantity": ["Quantidade deve ser maior que zero"],
    "Price": ["Preço deve ser maior que zero"]
  }
}
```

### 401 Unauthorized
```json
{
  "message": "Token inválido ou expirado"
}
```

### 403 Forbidden
```json
{
  "message": "Você não tem permissão para acessar este recurso"
}
```

### 404 Not Found
```json
{
  "message": "Ativo de investimento não encontrado"
}
```

### 500 Internal Server Error
```json
{
  "message": "Erro interno do servidor",
  "error": "NullReferenceException: Object reference not set..."
}
```

---

## ?? Rate Limiting

### Limites
- **Geral:** 100 requisições/minuto por usuário
- **Update Prices:** 1 requisição/minuto
- **Reports:** 10 requisições/minuto

### Headers de Resposta
```
X-RateLimit-Limit: 100
X-RateLimit-Remaining: 95
X-RateLimit-Reset: 1613216400
```

### Erro 429 Too Many Requests
```json
{
  "message": "Limite de requisições excedido",
  "retryAfter": 60
}
```

---

## ?? Notas Importantes

### Preço Médio Ponderado
- Calculado automaticamente em compras
- **Não muda** em vendas
- Inclui taxas no cálculo

### Soft Delete
- Registros deletados não são removidos
- Campo `IsDeleted` marcado como `true`
- Não aparecem em listagens normais

### Transações Vinculadas
- Toda operação de investimento cria duas transações:
  1. `InvestmentTransaction` (histórico do ativo)
  2. `Transaction` regular (impacta saldo da conta)
- Vinculadas via `LinkedTransactionId`

### Atualização de Cotações
- Automática: 3x/dia (12h, 15h, 18h)
- Manual: via endpoint `/update-prices`
- Cache: 15 minutos
- Somente ativos com `ticker` configurado

---

## ?? Testando a API

### Usando cURL

```bash
# Login
TOKEN=$(curl -X POST "https://localhost:5001/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password"}' | jq -r '.token')

# Listar ativos
curl -X GET "https://localhost:5001/api/investment-assets" \
  -H "Authorization: Bearer $TOKEN"

# Criar ativo
curl -X POST "https://localhost:5001/api/investment-assets" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "accountId": "acc_123",
    "assetType": 0,
    "name": "Petrobras PN",
    "ticker": "PETR4",
    "initialQuantity": 100,
    "initialPrice": 32.50,
    "initialFees": 10.00
  }'
```

### Usando Postman

1. Importe a collection: `docs/postman/investment-api.json`
2. Configure environment com:
   - `base_url`: https://localhost:5001
   - `token`: {seu-token-jwt}

### Usando Swagger

Acesse: https://localhost:5001/swagger

---

## ?? Suporte

- **GitHub Issues:** https://github.com/lgfauth/money-manager/issues
- **Email:** api@moneymanager.com
- **Status da API:** https://status.moneymanager.com

---

**Versão da API:** 1.0.0
**Última atualização:** 13/02/2025
