using FocusDeck.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Server.Services.Auth;

public class TokenPruningService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<TokenPruningService> _logger;

    public TokenPruningService(IServiceProvider services, ILogger<TokenPruningService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();

                // Delete expired revoked access tokens
                var now = DateTime.UtcNow;
                var expiredRevoked = await db.RevokedAccessTokens
                    .Where(r => r.ExpiresUtc <= now)
                    .ToListAsync(stoppingToken);
                if (expiredRevoked.Count > 0)
                {
                    db.RevokedAccessTokens.RemoveRange(expiredRevoked);
                }

                // Optionally delete expired refresh tokens
                var expiredRefresh = await db.RefreshTokens
                    .Where(t => t.ExpiresUtc <= now)
                    .ToListAsync(stoppingToken);
                if (expiredRefresh.Count > 0)
                {
                    db.RefreshTokens.RemoveRange(expiredRefresh);
                }

                // Delete expired pairing sessions (past ExpiresAt)
                var expiredPairings = await db.PairingSessions
                    .Where(p => p.ExpiresAt <= now)
                    .ToListAsync(stoppingToken);
                if (expiredPairings.Count > 0)
                {
                    db.PairingSessions.RemoveRange(expiredPairings);
                }

                if (expiredRevoked.Count > 0 || expiredRefresh.Count > 0 || expiredPairings.Count > 0)
                {
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Pruned tokens: revoked={Revoked}, refresh={Refresh}, pairings={Pairings}", expiredRevoked.Count, expiredRefresh.Count, expiredPairings.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Token pruning failed");
            }

            try
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
            catch (TaskCanceledException) { }
        }
    }
}
