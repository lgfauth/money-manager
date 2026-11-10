using System.Text;

namespace MoneyManager.Infrastructure.Configuration;

/// <summary>
/// Variáveis de ambiente exigidas por serviço. Ao criar uma integração ou segredo novo, inclua-o aqui
/// para que a ausência apareça na inicialização e no /health.
/// </summary>
public static class EnvironmentRequirements
{
    private const EnvironmentRequirementLevel Critical = EnvironmentRequirementLevel.Critical;
    private const EnvironmentRequirementLevel Feature = EnvironmentRequirementLevel.Feature;

    private static readonly EnvironmentRequirement MongoConnectionString =
        new(["MongoDB:ConnectionString"], "MongoDB__ConnectionString", Critical, "Banco de dados");

    private static readonly EnvironmentRequirement MongoDatabaseName =
        new(["MongoDB:DatabaseName"], "MongoDB__DatabaseName", Critical, "Banco de dados");

    private static readonly EnvironmentRequirement EncryptionKey =
        new(["Encryption:Key"], "Encryption__Key", Feature, "Conexão bancária (Banco MCP)", ValidateEncryptionKey);

    private static readonly EnvironmentRequirement AnthropicApiKey =
        new(["Anthropic:ApiKey"], "Anthropic__ApiKey", Feature, "Leitura por IA (comprovantes e faturas importadas)");

    public static IReadOnlyList<EnvironmentRequirement> OperationalApi { get; } =
    [
        MongoConnectionString,
        MongoDatabaseName,
        new(["Jwt:SecretKey"], "Jwt__SecretKey", Critical, "Autenticação de usuários", v => ValidateMinBytes(v, 32)),
        EncryptionKey,
        AnthropicApiKey,
        new(["Efi:ClientId"], "Efi__ClientId", Feature, "Assinatura Premium (Efí Bank)"),
        new(["Efi:ClientSecret"], "Efi__ClientSecret", Feature, "Assinatura Premium (Efí Bank)"),
        new(["Efi:CertificatePath"], "Efi__CertificatePath", Feature, "Assinatura Premium (Efí Bank)", ValidateFileExists)
    ];

    public static IReadOnlyList<EnvironmentRequirement> BackofficeApi { get; } =
    [
        MongoConnectionString,
        MongoDatabaseName,
        // Mesma prioridade do Program.cs: a env var ADMIN_AUTH_SECRET vence o appsettings
        new(["ADMIN_AUTH_SECRET", "AdminAuth:SecretKey"], "ADMIN_AUTH_SECRET", Critical, "Autenticação do Backoffice", v => ValidateMinBytes(v, 32))
    ];

    public static IReadOnlyList<EnvironmentRequirement> Worker { get; } =
    [
        MongoConnectionString,
        MongoDatabaseName,
        EncryptionKey,
        AnthropicApiKey
    ];

    internal static string? ValidateMinBytes(string value, int minBytes) =>
        Encoding.UTF8.GetByteCount(value) < minBytes
            ? $"precisa ter ao menos {minBytes} caracteres"
            : null;

    // Mesma regra do AesEncryptionService: Base64 com ao menos 48 bytes (chave + IV)
    internal static string? ValidateEncryptionKey(string value)
    {
        try
        {
            return Convert.FromBase64String(value).Length < 48
                ? "é curta demais (gere com 'openssl rand -base64 48')"
                : null;
        }
        catch (FormatException)
        {
            return "não é Base64 válido (gere com 'openssl rand -base64 48')";
        }
    }

    internal static string? ValidateFileExists(string value) =>
        File.Exists(value) ? null : "aponta para um arquivo que não existe";
}
