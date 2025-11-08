using System.Numerics;
using Microsoft.Extensions.Caching.Memory;

namespace FocusDeck.Server.Services.Auth;

public interface ISrpSessionCache
{
    SrpServerSession Store(
        string userId,
        byte[] salt,
        BigInteger verifier,
        BigInteger clientPublic,
        BigInteger serverSecret,
        BigInteger serverPublic,
        BigInteger scramble,
        string? clientId,
        string? deviceName,
        string? devicePlatform);
    bool TryGet(Guid sessionId, out SrpServerSession? session);
    void Remove(Guid sessionId);
}

public sealed record SrpServerSession(
    Guid SessionId,
    string UserId,
    byte[] Salt,
    BigInteger Verifier,
    BigInteger ClientPublic,
    BigInteger ServerSecret,
    BigInteger ServerPublic,
    BigInteger Scramble,
    string? ClientId,
    string? DeviceName,
    string? DevicePlatform,
    DateTimeOffset ExpiresAt
);

public sealed class SrpSessionCache : ISrpSessionCache
{
    private readonly IMemoryCache _cache;

    public SrpSessionCache(IMemoryCache cache)
    {
        _cache = cache;
    }

    public SrpServerSession Store(
        string userId,
        byte[] salt,
        BigInteger verifier,
        BigInteger clientPublic,
        BigInteger serverSecret,
        BigInteger serverPublic,
        BigInteger scramble,
        string? clientId,
        string? deviceName,
        string? devicePlatform)
    {
        var sessionId = Guid.NewGuid();
        var record = new SrpServerSession(
            sessionId,
            userId,
            salt,
            verifier,
            clientPublic,
            serverSecret,
            serverPublic,
            scramble,
            clientId,
            deviceName,
            devicePlatform,
            DateTimeOffset.UtcNow.AddMinutes(5));
        var options = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        };

        _cache.Set(sessionId, record, options);
        return record;
    }

    public bool TryGet(Guid sessionId, out SrpServerSession? session)
    {
        if (_cache.TryGetValue(sessionId, out SrpServerSession? existing) && existing is not null && existing.ExpiresAt > DateTimeOffset.UtcNow)
        {
            session = existing;
            return true;
        }

        session = null;
        return false;
    }

    public void Remove(Guid sessionId)
    {
        _cache.Remove(sessionId);
    }
}
