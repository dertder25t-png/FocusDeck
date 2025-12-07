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
            // Configuration for testing environment
            var testConfig = new Dictionary<string, string?>
            {
                ["Authentication:Google:ClientId"] = "test-client-id",
                ["Authentication:Google:ClientSecret"] = "test-client-secret"
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
