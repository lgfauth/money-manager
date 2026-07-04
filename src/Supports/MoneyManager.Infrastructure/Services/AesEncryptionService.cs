using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using MoneyManager.Application.Services;

namespace MoneyManager.Infrastructure.Services;

public class AesEncryptionService : IEncryptionService
{
    private readonly byte[] _key;
    private readonly byte[] _iv;

    public AesEncryptionService(IConfiguration configuration)
    {
        var raw = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException(
                "Encryption:Key não configurada. Defina a variável de ambiente Encryption__Key neste serviço.");

        // Placeholders dos appsettings ("strong_text_for_secret_here", "${Encryption__Key}") indicam
        // que a env var Encryption__Key não foi definida no serviço (Railway) — falhar com mensagem clara.
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(raw);
        }
        catch (FormatException)
        {
            throw new InvalidOperationException(
                "Encryption:Key inválida ou não configurada (valor atual não é Base64 — provavelmente o placeholder do appsettings). " +
                "Defina a variável de ambiente Encryption__Key neste serviço com o MESMO valor usado na API " +
                "(gere com 'openssl rand -base64 48').");
        }

        if (bytes.Length < 48)
            throw new InvalidOperationException("Encryption:Key inválida. Gere com 'openssl rand -base64 48'.");

        _key = bytes[..32];
        _iv = bytes[32..48];
    }

    public string Encrypt(string plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var encryptor = aes.CreateEncryptor();
        var input = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var encrypted = encryptor.TransformFinalBlock(input, 0, input.Length);
        return Convert.ToBase64String(encrypted);
    }

    public string Decrypt(string ciphertext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV = _iv;

        using var decryptor = aes.CreateDecryptor();
        var input = Convert.FromBase64String(ciphertext);
        var decrypted = decryptor.TransformFinalBlock(input, 0, input.Length);
        return System.Text.Encoding.UTF8.GetString(decrypted);
    }
}
