using FocusDeck.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Hangfire;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;

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
        var baseDir = AppContext.BaseDirectory ?? Directory.GetCurrentDirectory();
        var contentRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", "..", "src", "FocusDeck.Server"));
        builder.UseContentRoot(contentRoot);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = "test-key-for-testing-purposes-min-32-chars-long",
                ["Jwt:Issuer"] = "FocusDeckDev",
                ["Jwt:Audience"] = "FocusDeckClients"
            });
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
