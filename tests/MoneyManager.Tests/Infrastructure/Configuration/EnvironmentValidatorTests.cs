using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MoneyManager.Infrastructure.Configuration;
using MoneyManager.Observability;
using NSubstitute;
using Xunit;

namespace MoneyManager.Tests.Infrastructure.Configuration;

public class EnvironmentValidatorTests
{
    private static readonly string ValidEncryptionKey = Convert.ToBase64String(new byte[48]);
    private const string ValidSecret = "0123456789abcdef0123456789abcdef";

    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    private static Dictionary<string, string?> ValidWorkerValues() => new()
    {
        ["MongoDB:ConnectionString"] = "mongodb://localhost:27017",
        ["MongoDB:DatabaseName"] = "MoneyManager",
        ["Encryption:Key"] = ValidEncryptionKey,
        ["Anthropic:ApiKey"] = "sk-ant-test"
    };

    // --- IsPlaceholder ---

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("${Anthropic__ApiKey}")]
    [InlineData("strong_text_for_secret_here")]
    [InlineData("mongo_connection_string+here")]
    [InlineData("change-this-admin-secret-key-with-at-least-32-characters")]
    [InlineData("CHANGE_ME")]
    public void IsPlaceholder_WhenValueIsEmptyOrAppsettingsExample_ShouldReturnTrue(string? value)
    {
        // Act / Assert
        Assert.True(EnvironmentValidator.IsPlaceholder(value));
    }

    [Theory]
    [InlineData("sk-ant-api03-abc")]
    [InlineData("mongodb+srv://user:pass@cluster/db")]
    public void IsPlaceholder_WhenValueIsReal_ShouldReturnFalse(string value)
    {
        // Act / Assert
        Assert.False(EnvironmentValidator.IsPlaceholder(value));
    }

    // --- Validate ---

    [Fact]
    public void Validate_WhenAllWorkerVariablesAreValid_ShouldBeHealthy()
    {
        // Act
        var report = EnvironmentValidator.Validate("Worker", Config(ValidWorkerValues()), EnvironmentRequirements.Worker);

        // Assert
        Assert.Empty(report.Issues);
        Assert.Equal("healthy", report.Status);
        Assert.Equal(EnvironmentRequirements.Worker.Count, report.CheckedCount);
    }

    [Fact]
    public void Validate_WhenAnthropicKeyIsPlaceholder_ShouldBeDegradedWithFeatureIssue()
    {
        // Arrange — cenário do incidente: Anthropic__ApiKey não definida no Worker
        var values = ValidWorkerValues();
        values["Anthropic:ApiKey"] = "${Anthropic__ApiKey}";

        // Act
        var report = EnvironmentValidator.Validate("Worker", Config(values), EnvironmentRequirements.Worker);

        // Assert
        var issue = Assert.Single(report.Issues);
        Assert.Equal("Anthropic__ApiKey", issue.EnvironmentVariable);
        Assert.Equal(EnvironmentRequirementLevel.Feature, issue.Level);
        Assert.Contains("placeholder", issue.Problem);
        Assert.False(report.HasCriticalIssues);
        Assert.Equal("degraded", report.Status);
    }

    [Fact]
    public void Validate_WhenMongoConnectionStringMissing_ShouldBeUnhealthy()
    {
        // Arrange
        var values = ValidWorkerValues();
        values["MongoDB:ConnectionString"] = "";

        // Act
        var report = EnvironmentValidator.Validate("Worker", Config(values), EnvironmentRequirements.Worker);

        // Assert
        var issue = Assert.Single(report.Issues);
        Assert.Equal("MongoDB__ConnectionString", issue.EnvironmentVariable);
        Assert.Equal(EnvironmentRequirementLevel.Critical, issue.Level);
        Assert.Equal("unhealthy", report.Status);
    }

    [Theory]
    [InlineData("nao-e-base64!!")]
    [InlineData("AAAA")]
    public void Validate_WhenEncryptionKeyIsInvalid_ShouldReportIssue(string key)
    {
        // Arrange
        var values = ValidWorkerValues();
        values["Encryption:Key"] = key;

        // Act
        var report = EnvironmentValidator.Validate("Worker", Config(values), EnvironmentRequirements.Worker);

        // Assert
        var issue = Assert.Single(report.Issues);
        Assert.Equal("Encryption__Key", issue.EnvironmentVariable);
        Assert.Contains("openssl rand -base64 48", issue.Problem);
    }

    [Fact]
    public void Validate_WhenJwtSecretIsTooShort_ShouldReportCriticalIssue()
    {
        // Arrange
        var values = new Dictionary<string, string?>
        {
            ["MongoDB:ConnectionString"] = "mongodb://localhost:27017",
            ["MongoDB:DatabaseName"] = "MoneyManager",
            ["Jwt:SecretKey"] = "curta"
        };

        // Act
        var report = EnvironmentValidator.Validate("Api", Config(values), EnvironmentRequirements.OperationalApi);

        // Assert
        var jwt = Assert.Single(report.Issues, i => i.EnvironmentVariable == "Jwt__SecretKey");
        Assert.Equal(EnvironmentRequirementLevel.Critical, jwt.Level);
        Assert.Contains("32", jwt.Problem);
    }

    [Fact]
    public void Validate_WhenFirstKeyIsEmpty_ShouldUseFallbackKey()
    {
        // Arrange — ADMIN_AUTH_SECRET ausente, AdminAuth:SecretKey válida (mesma prioridade do Program.cs)
        var values = new Dictionary<string, string?>
        {
            ["MongoDB:ConnectionString"] = "mongodb://localhost:27017",
            ["MongoDB:DatabaseName"] = "MoneyManager",
            ["AdminAuth:SecretKey"] = ValidSecret
        };

        // Act
        var report = EnvironmentValidator.Validate("Backoffice", Config(values), EnvironmentRequirements.BackofficeApi);

        // Assert
        Assert.Empty(report.Issues);
    }

    [Fact]
    public void Validate_WhenBackofficeUsesDefaultAdminSecret_ShouldReportCriticalIssue()
    {
        // Arrange — segredo de exemplo do appsettings.json permitiria forjar tokens administrativos
        var values = new Dictionary<string, string?>
        {
            ["MongoDB:ConnectionString"] = "mongodb://localhost:27017",
            ["MongoDB:DatabaseName"] = "MoneyManager",
            ["AdminAuth:SecretKey"] = "change-this-admin-secret-key-with-at-least-32-characters"
        };

        // Act
        var report = EnvironmentValidator.Validate("Backoffice", Config(values), EnvironmentRequirements.BackofficeApi);

        // Assert
        var issue = Assert.Single(report.Issues);
        Assert.Equal("ADMIN_AUTH_SECRET", issue.EnvironmentVariable);
        Assert.True(report.HasCriticalIssues);
    }

    [Fact]
    public void Validate_HealthPayload_ShouldNeverContainConfiguredValues()
    {
        // Arrange
        var values = ValidWorkerValues();
        values["Encryption:Key"] = "valor-secreto-invalido";

        // Act
        var report = EnvironmentValidator.Validate("Worker", Config(values), EnvironmentRequirements.Worker);
        var payload = System.Text.Json.JsonSerializer.Serialize(report.ToHealthPayload());

        // Assert
        Assert.Contains("Encryption__Key", payload);
        Assert.DoesNotContain("valor-secreto-invalido", payload);
    }

    // --- ReportOrThrow ---

    private static (IServiceProvider Services, IProcessLogger ProcessLogger) CreateServices()
    {
        var processLogger = Substitute.For<IProcessLogger>();
        var services = new ServiceCollection()
            .AddSingleton<ILoggerFactory>(NullLoggerFactory.Instance)
            .AddScoped(_ => processLogger)
            .BuildServiceProvider();
        return (services, processLogger);
    }

    [Fact]
    public void ReportOrThrow_WhenHealthy_ShouldNotThrowNorWriteProcessLog()
    {
        // Arrange
        var (services, processLogger) = CreateServices();
        var report = EnvironmentValidator.Validate("Worker", Config(ValidWorkerValues()), EnvironmentRequirements.Worker);

        // Act
        EnvironmentValidator.ReportOrThrow(report, services);

        // Assert
        processLogger.DidNotReceiveWithAnyArgs().Start(default!);
    }

    [Fact]
    public void ReportOrThrow_WhenDegraded_ShouldWriteFailedProcessLogWithoutThrowing()
    {
        // Arrange
        var (services, processLogger) = CreateServices();
        var values = ValidWorkerValues();
        values["Anthropic:ApiKey"] = "${Anthropic__ApiKey}";
        var report = EnvironmentValidator.Validate("Worker", Config(values), EnvironmentRequirements.Worker);

        // Act
        EnvironmentValidator.ReportOrThrow(report, services);

        // Assert
        processLogger.Received(1).Start(EnvironmentValidator.ProcessName, Arg.Any<Dictionary<string, object?>?>());
        processLogger.Received(1).AddError(
            Arg.Is<string>(m => m.Contains("Anthropic__ApiKey")), null, Arg.Any<Dictionary<string, object?>?>());
        processLogger.Received(1).Finish(false);
    }

    [Fact]
    public void ReportOrThrow_WhenCritical_ShouldThrowNamingTheVariable()
    {
        // Arrange
        var (services, processLogger) = CreateServices();
        var values = ValidWorkerValues();
        values["MongoDB:DatabaseName"] = null;
        var report = EnvironmentValidator.Validate("Worker", Config(values), EnvironmentRequirements.Worker);

        // Act
        var ex = Assert.Throws<InvalidOperationException>(() => EnvironmentValidator.ReportOrThrow(report, services));

        // Assert
        Assert.Contains("MongoDB__DatabaseName", ex.Message);
        processLogger.Received(1).Finish(false);
    }

    [Fact]
    public void ReportOrThrow_WhenCriticalButFailOnCriticalDisabled_ShouldOnlyLog()
    {
        // Arrange — desenvolvimento local: appsettings com valores de exemplo não derrubam a subida
        var (services, processLogger) = CreateServices();
        var values = ValidWorkerValues();
        values["MongoDB:DatabaseName"] = null;
        var report = EnvironmentValidator.Validate("Worker", Config(values), EnvironmentRequirements.Worker);

        // Act
        EnvironmentValidator.ReportOrThrow(report, services, failOnCritical: false);

        // Assert
        processLogger.Received(1).Finish(false);
    }
}
