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
            ?? throw new InvalidOperationException("Encryption:Key não configurada.");

        var bytes = Convert.FromBase64String(raw);
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
