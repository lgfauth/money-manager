using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using NSubstitute;
using Xunit;
using MoneyManager.Infrastructure.Services.AI;

namespace MoneyManager.Tests.Infrastructure.Services;

public class AnthropicReceiptAnalysisServiceTests
{
    private const string ReceiptJson =
        "{\"description\":\"Mercado X\",\"amount\":123.45,\"date\":\"2026-11-04\",\"transactionType\":\"expense\",\"categoryHint\":\"Alimentação\",\"paymentMethod\":null,\"installments\":null,\"notes\":null,\"confidence\":0.9}";

    private static AnthropicReceiptAnalysisService CreateService(string responseBody, HttpStatusCode status = HttpStatusCode.OK)
        => CreateService(new StubHandler(status, responseBody));

    private static AnthropicReceiptAnalysisService CreateService(StubHandler handler)
    {
        var factory = Substitute.For<IHttpClientFactory>();
        factory.CreateClient("anthropic").Returns(new HttpClient(handler));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Anthropic:ApiKey"] = "test-key",
                ["Anthropic:Model"] = "claude-haiku-5-5"
            })
            .Build();

        return new AnthropicReceiptAnalysisService(factory, configuration);
    }

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    [Fact]
    public async Task AnalyzeAsync_WhenResponseStartsWithThinkingBlock_ShouldReadTextBlock()
    {
        // Arrange
        var body = $"{{\"content\":[{{\"type\":\"thinking\",\"thinking\":\"\",\"signature\":\"sig\"}},{{\"type\":\"text\",\"text\":\"{Escape(ReceiptJson)}\"}}],\"stop_reason\":\"end_turn\"}}";
        var service = CreateService(body);

        // Act
        var result = await service.AnalyzeAsync("base64", "image/png");

        // Assert
        Assert.Equal("Mercado X", result.Description);
        Assert.Equal(123.45m, result.Amount);
        Assert.Equal(new DateOnly(2026, 11, 4), result.Date);
        Assert.Equal("Alimentação", result.CategoryHint);
    }

    [Fact]
    public async Task AnalyzeAsync_WithOnlyTextBlock_ShouldParseResult()
    {
        // Arrange
        var body = $"{{\"content\":[{{\"type\":\"text\",\"text\":\"{Escape(ReceiptJson)}\"}}],\"stop_reason\":\"end_turn\"}}";
        var service = CreateService(body);

        // Act
        var result = await service.AnalyzeAsync("base64", "image/png");

        // Assert
        Assert.Equal("expense", result.TransactionType);
        Assert.Equal(0.9m, result.Confidence);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenTruncatedDuringThinking_ShouldThrowFailure()
    {
        // Arrange
        var body = "{\"content\":[{\"type\":\"thinking\",\"thinking\":\"\",\"signature\":\"sig\"}],\"stop_reason\":\"max_tokens\"}";
        var service = CreateService(body);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AnalyzeAsync("base64", "image/png"));
        Assert.Contains("Falha", ex.Message);
    }

    [Fact]
    public async Task AnalyzeAsync_WhenRefused_ShouldThrowFailure()
    {
        // Arrange
        var body = "{\"content\":[],\"stop_reason\":\"refusal\"}";
        var service = CreateService(body);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.AnalyzeAsync("base64", "image/png"));
        Assert.Contains("recusou", ex.Message);
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldSendRequestWithThinkingDisabledAndLowMaxTokens()
    {
        // Arrange
        var body = $"{{\"content\":[{{\"type\":\"text\",\"text\":\"{Escape(ReceiptJson)}\"}}],\"stop_reason\":\"end_turn\"}}";
        var handler = new StubHandler(HttpStatusCode.OK, body);
        var service = CreateService(handler);

        // Act
        await service.AnalyzeAsync("base64", "image/png");

        // Assert
        using var request = JsonDocument.Parse(handler.LastRequestBody!);
        Assert.Equal("disabled", request.RootElement.GetProperty("thinking").GetProperty("type").GetString());
        Assert.Equal(1024, request.RootElement.GetProperty("max_tokens").GetInt32());
    }

    [Fact]
    public async Task AnalyzeAsync_ShouldSendTodaysDateInPrompt()
    {
        // Arrange
        var body = $"{{\"content\":[{{\"type\":\"text\",\"text\":\"{Escape(ReceiptJson)}\"}}],\"stop_reason\":\"end_turn\"}}";
        var handler = new StubHandler(HttpStatusCode.OK, body);
        var service = CreateService(handler);
        var today = DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

        // Act
        await service.AnalyzeAsync("base64", "image/png");

        // Assert
        using var request = JsonDocument.Parse(handler.LastRequestBody!);
        var prompt = request.RootElement.GetProperty("messages")[0].GetProperty("content")[1].GetProperty("text").GetString();
        Assert.Contains($"Today's date is {today}.", prompt);
    }

    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(status)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
        }
    }
}
