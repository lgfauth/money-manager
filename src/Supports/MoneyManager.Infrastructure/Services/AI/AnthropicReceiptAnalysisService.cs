using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using MoneyManager.Application.DTOs.Response;
using MoneyManager.Application.Services;

namespace MoneyManager.Infrastructure.Services.AI;

public class AnthropicReceiptAnalysisService : IReceiptAnalysisService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private const string AnthropicApiUrl = "https://api.anthropic.com/v1/messages";
    private const string AnthropicVersion = "2023-06-01";
    private const string DefaultModel = "claude-opus-4-5-20251101";
    private const int MaxTokens = 1024;

    private const string AnalysisPrompt = """
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
        """;

    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;

    public AnthropicReceiptAnalysisService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _httpClient = httpClientFactory.CreateClient("anthropic");
        _apiKey = configuration["Anthropic:ApiKey"] ?? string.Empty;
        _model = configuration["Anthropic:Model"] ?? DefaultModel;
    }

    public async Task<ReceiptAnalysisResultDto> AnalyzeAsync(string fileBase64, string mimeType)
    {
        var requestBody = new
        {
            model = _model,
            max_tokens = MaxTokens,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new
                        {
                            type = "image",
                            source = new
                            {
                                type = "base64",
                                media_type = mimeType,
                                data = fileBase64
                            }
                        },
                        new
                        {
                            type = "text",
                            text = AnalysisPrompt
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(requestBody, JsonOptions);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, AnthropicApiUrl);
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", AnthropicVersion);
        request.Content = content;

        HttpResponseMessage response;
        try
        {
            response = await _httpClient.SendAsync(request);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Serviço de análise indisponível.", ex);
        }

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Falha na extração do comprovante. Status: {response.StatusCode}. Detalhes: {errorBody}");
        }

        var responseBody = await response.Content.ReadAsStringAsync();

        // Extrair o texto da resposta da Anthropic
        string extractedText;
        try
        {
            using var doc = JsonDocument.Parse(responseBody);
            extractedText = doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Falha ao processar a resposta do serviço de análise.", ex);
        }

        // Extrair apenas o JSON da resposta (remover possível markdown)
        var jsonStart = extractedText.IndexOf('{');
        var jsonEnd = extractedText.LastIndexOf('}');
        if (jsonStart < 0 || jsonEnd < 0)
            throw new InvalidOperationException("Resposta do serviço não contém dados estruturados.");

        var resultJson = extractedText[jsonStart..(jsonEnd + 1)];

        try
        {
            using var resultDoc = JsonDocument.Parse(resultJson);
            var root = resultDoc.RootElement;

            var todayStr = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

            var dateStr = root.TryGetProperty("date", out var dateProp) ? dateProp.GetString() ?? todayStr : todayStr;
            if (!DateOnly.TryParseExact(dateStr, "yyyy-MM-dd", out var date))
                date = DateOnly.FromDateTime(DateTime.UtcNow);

            return new ReceiptAnalysisResultDto(
                Description: root.TryGetProperty("description", out var desc) ? desc.GetString() ?? string.Empty : string.Empty,
                Amount: root.TryGetProperty("amount", out var amount) ? amount.GetDecimal() : 0,
                Date: date,
                TransactionType: root.TryGetProperty("transactionType", out var type) ? type.GetString() ?? "expense" : "expense",
                CategoryHint: root.TryGetProperty("categoryHint", out var cat) && cat.ValueKind != JsonValueKind.Null ? cat.GetString() : null,
                PaymentMethod: root.TryGetProperty("paymentMethod", out var pm) && pm.ValueKind != JsonValueKind.Null ? pm.GetString() : null,
                Installments: root.TryGetProperty("installments", out var inst) && inst.ValueKind != JsonValueKind.Null ? inst.GetInt32() : null,
                Notes: root.TryGetProperty("notes", out var notes) && notes.ValueKind != JsonValueKind.Null ? notes.GetString() : null,
                Confidence: root.TryGetProperty("confidence", out var conf) ? conf.GetDecimal() : 0
            );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Falha ao interpretar os dados extraídos do comprovante.", ex);
        }
    }
}
