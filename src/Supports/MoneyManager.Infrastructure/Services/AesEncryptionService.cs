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
        var encryptionKey = configuration["Encryption:Key"]
            ?? throw new InvalidOperationException("Encryption:Key não configurada.");
        var keyBytes = Convert.FromBase64String(encryptionKey);
        _key = keyBytes[..32]; // AES-256
        _iv  = keyBytes[32..48];
    }

    public string Encrypt(string plaintext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV  = _iv;
        using var encryptor = aes.CreateEncryptor();
        var bytes = System.Text.Encoding.UTF8.GetBytes(plaintext);
        var encrypted = encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        return Convert.ToBase64String(encrypted);
    }

    public string Decrypt(string ciphertext)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.IV  = _iv;
        using var decryptor = aes.CreateDecryptor();
        var bytes = Convert.FromBase64String(ciphertext);
        var decrypted = decryptor.TransformFinalBlock(bytes, 0, bytes.Length);
        return System.Text.Encoding.UTF8.GetString(decrypted);
    }
}
