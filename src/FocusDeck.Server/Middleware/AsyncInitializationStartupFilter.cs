using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FocusDeck.Persistence;
using FocusDeck.Shared.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;

namespace FocusDeck.Server.Middleware;

public sealed class AsyncInitializationStartupFilter : IStartupFilter
{
    private readonly ILogger<AsyncInitializationStartupFilter> _logger;

    public AsyncInitializationStartupFilter(ILogger<AsyncInitializationStartupFilter> logger)
    {
        _logger = logger;
    }

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            var lifetime = app.ApplicationServices.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStarted.Register(() =>
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await InitializeDatabaseAsync(app.ApplicationServices, lifetime);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogCritical(ex, "Database initialization failed during startup");
                        lifetime.StopApplication();
                    }
                });
            });

            next(app);
        };
    }

    private static async Task InitializeDatabaseAsync(IServiceProvider services, IHostApplicationLifetime lifetime)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AsyncInitializationStartupFilter>>();

        var migrationPolicy = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retry => TimeSpan.FromSeconds(Math.Pow(2, retry)),
                onRetry: (ex, delay) => logger.LogWarning(ex, "Migration attempt failed, retrying in {Delay}", delay));

        await migrationPolicy.ExecuteAsync(() => db.Database.MigrateAsync());
        await BackfillPakeSaltsAsync(db, logger);
    }

    private static async Task BackfillPakeSaltsAsync(AutomationDbContext db, ILogger logger)
    {
        var rows = await db.PakeCredentials
            .Where(p => string.IsNullOrWhiteSpace(p.SaltBase64) && !string.IsNullOrWhiteSpace(p.KdfParametersJson))
            .ToListAsync();

        if (rows.Count == 0)
        {
            return;
        }

        logger.LogInformation("Backfilling {Count} PAKE credential(s) with salt from KDF metadata", rows.Count);

        foreach (var row in rows)
        {
            try
            {
                var kdf = JsonSerializer.Deserialize<SrpKdfParameters>(row.KdfParametersJson!);
                if (kdf?.SaltBase64 != null)
                {
                    row.SaltBase64 = kdf.SaltBase64;
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to parse KDF parameters for user {UserId}", row.UserId);
            }
        }

        await db.SaveChangesAsync();
    }
}
