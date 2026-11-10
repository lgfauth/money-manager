using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MoneyManager.Observability;

namespace MoneyManager.Infrastructure.Configuration;

/// <summary>
/// Gravidade de uma variável de ambiente ausente ou inválida.
/// </summary>
public enum EnvironmentRequirementLevel
{
    /// <summary>O serviço não funciona sem ela: a inicialização é interrompida.</summary>
    Critical,

    /// <summary>Só uma funcionalidade para: o serviço sobe, mas fica "degraded" no /health e no log.</summary>
    Feature
}

/// <summary>
/// Uma configuração exigida pelo serviço. <paramref name="Keys"/> lista as chaves de configuração na
/// ordem de prioridade usada pelo código (a primeira com valor vence), por exemplo
/// <c>["ADMIN_AUTH_SECRET", "AdminAuth:SecretKey"]</c>. <paramref name="Validate"/> recebe o valor já
/// preenchido e devolve a descrição do problema, ou null quando o valor é válido.
/// </summary>
public sealed record EnvironmentRequirement(
    IReadOnlyList<string> Keys,
    string EnvironmentVariable,
    EnvironmentRequirementLevel Level,
    string Feature,
    Func<string, string?>? Validate = null);

/// <summary>
/// Problema encontrado numa variável. Nunca contém o valor configurado, só o nome e a causa.
/// </summary>
public sealed record EnvironmentIssue(
    string EnvironmentVariable,
    EnvironmentRequirementLevel Level,
    string Feature,
    string Problem);

public sealed class EnvironmentValidationReport
{
    public EnvironmentValidationReport(string serviceName, int checkedCount, IReadOnlyList<EnvironmentIssue> issues)
    {
        ServiceName = serviceName;
        CheckedCount = checkedCount;
        Issues = issues;
        CheckedAtUtc = DateTime.UtcNow;
    }

    public string ServiceName { get; }
    public int CheckedCount { get; }
    public IReadOnlyList<EnvironmentIssue> Issues { get; }
    public DateTime CheckedAtUtc { get; }

    public bool HasCriticalIssues => Issues.Any(i => i.Level == EnvironmentRequirementLevel.Critical);

    /// <summary>"healthy", "degraded" (funcionalidade parada) ou "unhealthy" (variável crítica com problema).</summary>
    public string Status => HasCriticalIssues ? "unhealthy" : Issues.Count > 0 ? "degraded" : "healthy";

    /// <summary>Resumo seguro para o endpoint /health: só nomes de variáveis e causas.</summary>
    public object ToHealthPayload() => new
    {
        status = Status,
        checkedAt = CheckedAtUtc,
        @checked = CheckedCount,
        issues = Issues.Select(i => new
        {
            variable = i.EnvironmentVariable,
            level = i.Level.ToString(),
            feature = i.Feature,
            problem = i.Problem
        })
    };
}

/// <summary>
/// Verificação das variáveis de ambiente na inicialização das APIs e do Worker. Detecta variável
/// ausente, vazia, com placeholder dos appsettings (ex.: "${Anthropic__ApiKey}", que o .NET não
/// expande) ou com formato inválido, e torna o problema visível sem investigação: log na subida,
/// registro no ProcessLog (observabilidade do Backoffice) e status no /health.
/// </summary>
public static class EnvironmentValidator
{
    public const string ProcessName = "EnvironmentValidation";

    // Valores de exemplo dos appsettings que indicam que a variável real não foi definida
    private static readonly string[] PlaceholderMarkers =
    [
        "strong_text_for_secret_here",
        "mongo_connection_string",
        "change-this-",
        "CHANGE_ME"
    ];

    public static bool IsPlaceholder(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true;

        var trimmed = value.Trim();
        if (trimmed.StartsWith("${", StringComparison.Ordinal) && trimmed.EndsWith('}'))
            return true;

        return PlaceholderMarkers.Any(m => trimmed.Contains(m, StringComparison.OrdinalIgnoreCase));
    }

    public static EnvironmentValidationReport Validate(
        string serviceName,
        IConfiguration configuration,
        IReadOnlyList<EnvironmentRequirement> requirements)
    {
        var issues = new List<EnvironmentIssue>();

        foreach (var requirement in requirements)
        {
            var value = requirement.Keys
                .Select(key => configuration[key])
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

            string? problem;
            if (string.IsNullOrWhiteSpace(value))
                problem = "não definida ou vazia";
            else if (IsPlaceholder(value))
                problem = "contém o placeholder do appsettings (a variável não foi definida neste serviço)";
            else
                problem = requirement.Validate?.Invoke(value);

            if (problem is not null)
                issues.Add(new EnvironmentIssue(requirement.EnvironmentVariable, requirement.Level, requirement.Feature, problem));
        }

        return new EnvironmentValidationReport(serviceName, requirements.Count, issues);
    }

    /// <summary>
    /// Registra o resultado da verificação e interrompe a inicialização se houver problema crítico
    /// (quando <paramref name="failOnCritical"/> é true; em desenvolvimento local os appsettings usam
    /// valores de exemplo, então só registra). Com problemas, grava também um ProcessLog
    /// "EnvironmentValidation" com falha, que aparece na observabilidade do Backoffice nos serviços
    /// que persistem ProcessLog.
    /// </summary>
    public static void ReportOrThrow(EnvironmentValidationReport report, IServiceProvider services, bool failOnCritical = true)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger(ProcessName);

        if (report.Issues.Count == 0)
        {
            logger.LogInformation(
                "[EnvCheck] {Service}: {Count} variáveis de ambiente verificadas, nenhum problema",
                report.ServiceName, report.CheckedCount);
            return;
        }

        foreach (var issue in report.Issues)
        {
            logger.Log(
                issue.Level == EnvironmentRequirementLevel.Critical ? LogLevel.Critical : LogLevel.Error,
                "[EnvCheck] {Service}: variável {Variable} {Problem}. Afeta: {Feature} ({Level})",
                report.ServiceName, issue.EnvironmentVariable, issue.Problem, issue.Feature, issue.Level);
        }

        WriteProcessLog(report, services, logger);

        if (report.HasCriticalIssues && failOnCritical)
        {
            var critical = report.Issues
                .Where(i => i.Level == EnvironmentRequirementLevel.Critical)
                .Select(i => $"{i.EnvironmentVariable} ({i.Problem})");

            throw new InvalidOperationException(
                $"[EnvCheck] {report.ServiceName} não pode iniciar: variáveis de ambiente críticas com problema: " +
                string.Join("; ", critical) + ". Defina-as nas variáveis do serviço (Railway) e faça o redeploy.");
        }

        logger.LogWarning(
            "[EnvCheck] {Service} iniciou DEGRADADO: {Count} variável(is) com problema. Veja os erros [EnvCheck] acima (nas APIs, também em GET /health)",
            report.ServiceName, report.Issues.Count);
    }

    private static void WriteProcessLog(EnvironmentValidationReport report, IServiceProvider services, ILogger logger)
    {
        try
        {
            using var scope = services.CreateScope();
            var processLogger = scope.ServiceProvider.GetService<IProcessLogger>();
            if (processLogger is null)
                return;

            processLogger.Start(ProcessName, new Dictionary<string, object?>
            {
                ["source"] = report.ServiceName,
                ["status"] = report.Status
            });

            foreach (var issue in report.Issues)
            {
                processLogger.AddError($"Variável {issue.EnvironmentVariable} {issue.Problem}", data: new Dictionary<string, object?>
                {
                    ["variable"] = issue.EnvironmentVariable,
                    ["level"] = issue.Level.ToString(),
                    ["feature"] = issue.Feature
                });
            }

            processLogger.Finish(success: false);
        }
        catch (Exception ex)
        {
            // Mongo indisponível etc.: o log de console acima já registrou os problemas
            logger.LogWarning(ex, "[EnvCheck] Não foi possível gravar o ProcessLog da verificação de ambiente");
        }
    }
}
