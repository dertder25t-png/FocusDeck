using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using FocusDeck.Persistence;
using FocusDeck.Server.Services.Auth;
using Hangfire;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FocusDeck.Server.Tests;

/// <summary>
/// WebApplicationFactory that shares a single SQLite connection for every test.
/// Ensures migrations run on the same connection that the application uses.
/// </summary>
public sealed class FocusDeckWebApplicationFactory : WebApplicationFactory<TestServerProgram>
{
    private DbConnection? _connection;

    protected override IHostBuilder CreateHostBuilder()
    {
        if (_connection == null)
        {
            _connection = new SqliteConnection("DataSource=file:memdb?mode=memory&cache=shared");
            _connection.Open();
        }

        var builder = TestServerProgram.CreateHostBuilder(Array.Empty<string>());
        builder.UseEnvironment("Testing");

        var contentRoot = Environment.GetEnvironmentVariable("SERVER_CONTENT_ROOT");
        if (string.IsNullOrEmpty(contentRoot) || !Directory.Exists(contentRoot))
        {
            // Fallback for local development
            contentRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/FocusDeck.Server"));
            if (!Directory.Exists(contentRoot))
            {
                throw new DirectoryNotFoundException(
                    $"The content root path was not found. Please set the SERVER_CONTENT_ROOT environment variable. Fallback path was: {contentRoot}");
            }
        }
        builder.UseContentRoot(contentRoot);

        builder.ConfigureAppConfiguration((context, config) =>
        {
            // IMPORTANT: Add test JWT config with high priority so it overrides any environment-specific config
            // This includes test overrides that might set a different environment
            var testConfig = new Dictionary<string, string?>
            {
                ["Jwt:PrimaryKey"] = "test-key-for-testing-purposes-min-32-chars-long",
                ["Jwt:SecondaryKey"] = "test-secondary-key-for-rotation-debug",
                ["Jwt:Issuer"] = "FocusDeckDev",
                ["Jwt:Audience"] = "FocusDeckClients",
                ["Jwt:KeyRotationInterval"] = "90.00:00:00"
            };
            // Add as the last source so it takes precedence
            config.AddInMemoryCollection(testConfig);
        });
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AutomationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AutomationDbContext>(options =>
            {
                options.UseSqlite(_connection!);
            });

            var backgroundJobDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IBackgroundJobClient));

            if (backgroundJobDescriptor != null)
            {
                services.Remove(backgroundJobDescriptor);
            }

            services.AddSingleton<IBackgroundJobClient, ImmediateBackgroundJobClient>();

        });

        return builder;
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        
        // Migrate database
        var db = scope.ServiceProvider.GetRequiredService<AutomationDbContext>();
        db.Database.Migrate();
        
        // Warm up JWT signing key provider to ensure keys are cached before tests run
        try
        {
            var keyProvider = scope.ServiceProvider.GetRequiredService<IJwtSigningKeyProvider>();
            // Clear any stale cache from previous test instances
            keyProvider.InvalidateCache();
            
            var keys = keyProvider.GetValidationKeys();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<FocusDeckWebApplicationFactory>>();
            
            if (!keys.Any())
            {
                logger.LogCritical("CRITICAL: No JWT validation keys were loaded during test initialization!");
                throw new InvalidOperationException("JWT validation keys are empty. Test configuration may not have been applied.");
            }
            
            logger.LogInformation("Successfully warmed up JWT key provider with {KeyCount} keys", keys.Count());
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<FocusDeckWebApplicationFactory>>();
            logger.LogError(ex, "CRITICAL: Failed to warm up JWT key provider");
            throw;
        }
        
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.Dispose();
            _connection = null;
        }

        base.Dispose(disposing);
    }
}
