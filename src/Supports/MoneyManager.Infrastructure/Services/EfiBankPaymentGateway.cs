using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoneyManager.Application.Services;

namespace MoneyManager.Infrastructure.Services;

public class EfiBankPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<EfiBankPaymentGateway> _logger;
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _baseUrl;
    private readonly string _subscriptionAmountBrl;

    private const string TokenCacheKey = "efi_access_token";

    public string ProviderName => "efi";

    public EfiBankPaymentGateway(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        IMemoryCache cache,
        ILogger<EfiBankPaymentGateway> logger)
    {
        _httpClient = httpClientFactory.CreateClient("efiBank");
        _cache = cache;
        _logger = logger;

        var s = configuration.GetSection("Efi");

        _clientId = s["ClientId"]
            ?? throw new InvalidOperationException("Efi:ClientId não configurado. Defina a variável de ambiente Efi__ClientId.");
        _clientSecret = s["ClientSecret"]
            ?? throw new InvalidOperationException("Efi:ClientSecret não configurado. Defina a variável de ambiente Efi__ClientSecret.");

        _subscriptionAmountBrl = s["SubscriptionAmountBrl"] ?? "19.90";

        var isSandbox = string.Equals(s["IsSandbox"], "true", StringComparison.OrdinalIgnoreCase);
        _baseUrl = isSandbox
            ? "https://pix-h.api.efipay.com.br"
            : "https://pix.api.efipay.com.br";
    }

    public async Task<CreateSubscriptionGatewayResult> CreateSubscriptionAsync(CreateSubscriptionGatewayRequest request)
    {
        var token = await GetAccessTokenAsync();

        // Data inicial é amanhã — garante tempo de aprovação pelo pagador antes da primeira cobrança
        var dataInicial = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd");

        var body = new
        {
            vinculo = new
            {
                contrato = $"premium-{request.UserId}",
                devedor = new
                {
                    cpf = request.PayerCpf,
                    nome = request.PayerName
                },
                objeto = "Assinatura MoneyManager Premium — acesso completo ao plano premium"
            },
            calendario = new
            {
                dataInicial,
                periodicidade = "MENSAL"
            },
            valor = new
            {
                valorRec = _subscriptionAmountBrl
            },
            politicaRetentativa = "PERMITE_3R_7D"
        };

        using var createReq = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/v2/rec");
        createReq.Headers.Add("Authorization", $"Bearer {token}");
        createReq.Content = JsonContent.Create(body);

        var response = await _httpClient.SendAsync(createReq);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogError("Efí Bank erro ao criar recorrência: {StatusCode} — {Body}",
                (int)response.StatusCode, errorBody);
            throw new InvalidOperationException(
                $"Efí Bank recusou criação da recorrência: HTTP {(int)response.StatusCode}");
        }

        var result = await response.Content.ReadFromJsonAsync<JsonElement>();
        var idRec = result.GetProperty("idRec").GetString()
            ?? throw new InvalidOperationException("Efí Bank não retornou idRec");

        var paymentUrl = await GetAuthorizationUrlAsync(idRec, token);

        _logger.LogInformation("Recorrência Pix Automático criada com sucesso — idRec: {IdRec}", idRec);

        return new CreateSubscriptionGatewayResult
        {
            ExternalSubscriptionId = idRec,
            PaymentUrl = paymentUrl
        };
    }

    public async Task CancelSubscriptionAsync(string externalSubscriptionId)
    {
        var token = await GetAccessTokenAsync();

        // Cancela a recorrência via PATCH /v2/rec/{idRec}.
        // O pagador também pode cancelar independentemente pelo app do seu banco.
        var body = new { status = "CANCELADA" };

        using var req = new HttpRequestMessage(HttpMethod.Patch,
            $"{_baseUrl}/v2/rec/{externalSubscriptionId}");
        req.Headers.Add("Authorization", $"Bearer {token}");
        req.Content = JsonContent.Create(body);

        var response = await _httpClient.SendAsync(req);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Efí Bank não confirmou cancelamento da recorrência {IdRec}: {StatusCode} — {Body}",
                externalSubscriptionId, (int)response.StatusCode, errorBody);
            // Não propaga exceção — nosso sistema já marcou como cancelada.
        }
        else
        {
            _logger.LogInformation("Recorrência Pix Automático cancelada no gateway — idRec: {IdRec}",
                externalSubscriptionId);
        }
    }

    public Task<WebhookValidationResult> ValidateAndParseWebhookAsync(
        string rawPayload,
        IDictionary<string, string> headers)
    {
        // Validação de origem via mTLS é responsabilidade do reverse proxy (Railway / nginx).
        // Aqui validamos apenas a estrutura do payload JSON recebido.

        if (string.IsNullOrWhiteSpace(rawPayload))
        {
            _logger.LogWarning("Webhook Efí recebido com payload vazio");
            return Task.FromResult(new WebhookValidationResult { IsValid = false });
        }

        JsonElement json;
        try
        {
            json = JsonSerializer.Deserialize<JsonElement>(rawPayload);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Webhook Efí com JSON inválido");
            return Task.FromResult(new WebhookValidationResult { IsValid = false });
        }

        // Evento de recorrência — path /rec enviado por PUT /v2/webhookrec
        if (json.TryGetProperty("recs", out var recs) && recs.GetArrayLength() > 0)
            return Task.FromResult(ParseRecEvent(recs[0]));

        // Evento de cobrança — path /cobr enviado por PUT /v2/webhookcobr
        if (json.TryGetProperty("cobsr", out var cobsr) && cobsr.GetArrayLength() > 0)
            return Task.FromResult(ParseCobrEvent(cobsr[0]));

        _logger.LogWarning("Webhook Efí — estrutura de payload não reconhecida");
        return Task.FromResult(new WebhookValidationResult { IsValid = false, EventType = WebhookEventType.Unknown });
    }

    // --- Helpers privados ---

    private async Task<string> GetAccessTokenAsync(CancellationToken ct = default)
    {
        if (_cache.TryGetValue(TokenCacheKey, out string? cached) && cached is not null)
            return cached;

        // Autenticação OAuth2 client_credentials com Basic Auth (clientId:clientSecret em base64)
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_clientId}:{_clientSecret}"));

        using var req = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/oauth/token");
        req.Headers.Add("Authorization", $"Basic {credentials}");
        req.Content = new StringContent(
            """{"grant_type":"client_credentials"}""",
            Encoding.UTF8,
            "application/json");

        var response = await _httpClient.SendAsync(req, ct);

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("Falha na autenticação OAuth2 Efí Bank: {StatusCode} — {Body}",
                (int)response.StatusCode, error);
            throw new InvalidOperationException(
                $"Autenticação Efí Bank falhou: HTTP {(int)response.StatusCode}");
        }

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var token = body.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("Efí Bank não retornou access_token");
        var expiresIn = body.TryGetProperty("expires_in", out var exp) ? exp.GetInt32() : 3600;

        // Cache com margem de 5 minutos para evitar uso de token prestes a expirar
        _cache.Set(TokenCacheKey, token, TimeSpan.FromSeconds(expiresIn - 300));

        _logger.LogInformation("Token OAuth2 Efí Bank obtido com sucesso — expira em {ExpiresIn}s", expiresIn);
        return token;
    }

    private async Task<string> GetAuthorizationUrlAsync(string idRec, string token)
    {
        // Busca os detalhes da recorrência criada para extrair o pixCopiaECola do QR code de autorização
        using var req = new HttpRequestMessage(HttpMethod.Get, $"{_baseUrl}/v2/rec/{idRec}");
        req.Headers.Add("Authorization", $"Bearer {token}");

        var response = await _httpClient.SendAsync(req);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Não foi possível buscar detalhes da recorrência {IdRec}", idRec);
            return BuildFallbackUrl(idRec);
        }

        var rec = await response.Content.ReadFromJsonAsync<JsonElement>();

        // Extrai pixCopiaECola do objeto loc (location/QR code da autorização)
        if (rec.TryGetProperty("loc", out var loc) &&
            loc.TryGetProperty("pixCopiaECola", out var copiaECola))
        {
            var valor = copiaECola.GetString();
            if (!string.IsNullOrEmpty(valor))
                return valor;
        }

        return BuildFallbackUrl(idRec);
    }

    private static string BuildFallbackUrl(string idRec)
        // URL de fallback: o frontend exibe instruções para o pagador autorizar pelo app do banco
        => $"pix-automatico://autorizacao/{idRec}";

    private WebhookValidationResult ParseRecEvent(JsonElement rec)
    {
        var idRec = GetString(rec, "idRec");
        var status = GetString(rec, "status");

        // Status de recorrência: APROVADA = pagador autorizou; CANCELADA = encerrada
        var eventType = status switch
        {
            "APROVADA" => WebhookEventType.PaymentConfirmed,
            "CANCELADA" => WebhookEventType.SubscriptionCancelled,
            _ => WebhookEventType.Unknown
        };

        _logger.LogInformation("Webhook Efí rec — idRec: {IdRec} status: {Status} → {Event}",
            idRec, status, eventType);

        return new WebhookValidationResult
        {
            IsValid = true,
            EventType = eventType,
            ExternalSubscriptionId = idRec,
            PeriodStart = DateTime.UtcNow,
            PeriodEnd = DateTime.UtcNow.AddMonths(1)
        };
    }

    private WebhookValidationResult ParseCobrEvent(JsonElement cobr)
    {
        var idRec = GetString(cobr, "idRec");
        var status = GetString(cobr, "status");

        // Status de cobrança paga: CONCLUIDA / LIQUIDADA; falha: EXPIRADA / CANCELADA
        var eventType = status switch
        {
            "CONCLUIDA" or "LIQUIDADA" => WebhookEventType.PaymentConfirmed,
            "EXPIRADA" or "CANCELADA" => WebhookEventType.PaymentFailed,
            _ => WebhookEventType.Unknown
        };

        // Tenta extrair datas da última tentativa de liquidação
        DateTime? periodStart = null;
        DateTime? periodEnd = null;

        if (cobr.TryGetProperty("tentativas", out var tentativas) && tentativas.GetArrayLength() > 0)
        {
            var ultima = tentativas[tentativas.GetArrayLength() - 1];
            if (ultima.TryGetProperty("dataLiquidacao", out var dataLiq) &&
                DateTime.TryParse(dataLiq.GetString(), out var paidAt))
            {
                periodStart = paidAt;
                periodEnd = paidAt.AddMonths(1);
            }
        }

        _logger.LogInformation("Webhook Efí cobr — idRec: {IdRec} status: {Status} → {Event}",
            idRec, status, eventType);

        return new WebhookValidationResult
        {
            IsValid = true,
            EventType = eventType,
            ExternalSubscriptionId = idRec,
            PeriodStart = periodStart ?? DateTime.UtcNow,
            PeriodEnd = periodEnd ?? DateTime.UtcNow.AddMonths(1)
        };
    }

    private static string GetString(JsonElement element, string propertyName)
        => element.TryGetProperty(propertyName, out var prop) ? prop.GetString() ?? "" : "";
}
