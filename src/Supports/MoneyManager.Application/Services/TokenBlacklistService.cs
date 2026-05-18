using Microsoft.Extensions.Caching.Memory;

namespace MoneyManager.Application.Services;

public interface ITokenBlacklistService
{
    void Revoke(string jti, DateTime expiry);
    bool IsRevoked(string jti);
}

public class TokenBlacklistService : ITokenBlacklistService
{
    private readonly IMemoryCache _cache;

    public TokenBlacklistService(IMemoryCache cache)
    {
        _cache = cache;
    }

    public void Revoke(string jti, DateTime expiry)
    {
        var ttl = expiry - DateTime.UtcNow;
        if (ttl > TimeSpan.Zero)
            _cache.Set($"bl:{jti}", true, ttl);
    }

    public bool IsRevoked(string jti) =>
        _cache.TryGetValue($"bl:{jti}", out _);
}
