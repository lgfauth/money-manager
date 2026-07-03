using Microsoft.Extensions.Caching.Memory;
using Xunit;
using MoneyManager.Application.Services;

namespace MoneyManager.Tests.Application.Services;

public class TokenBlacklistServiceTests : IDisposable
{
    private readonly MemoryCache _cache;
    private readonly TokenBlacklistService _service;

    public TokenBlacklistServiceTests()
    {
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new TokenBlacklistService(_cache);
    }

    public void Dispose() => _cache.Dispose();

    [Fact]
    public void Revoke_WithFutureExpiry_ShouldMarkTokenAsRevoked()
    {
        _service.Revoke("jti-123", DateTime.UtcNow.AddMinutes(30));

        Assert.True(_service.IsRevoked("jti-123"));
    }

    [Fact]
    public void Revoke_WithPastExpiry_ShouldNotStoreToken()
    {
        _service.Revoke("jti-expired", DateTime.UtcNow.AddMinutes(-5));

        Assert.False(_service.IsRevoked("jti-expired"));
    }

    [Fact]
    public void IsRevoked_WithUnknownJti_ShouldReturnFalse()
    {
        Assert.False(_service.IsRevoked("jti-never-revoked"));
    }

    [Fact]
    public void Revoke_ShouldNotAffectOtherTokens()
    {
        _service.Revoke("jti-a", DateTime.UtcNow.AddMinutes(30));

        Assert.True(_service.IsRevoked("jti-a"));
        Assert.False(_service.IsRevoked("jti-b"));
    }
}
