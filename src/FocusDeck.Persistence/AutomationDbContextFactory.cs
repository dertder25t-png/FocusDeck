using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.IO;

namespace FocusDeck.Persistence;

/// <summary>
/// Design-time factory for creating AutomationDbContext instances during migrations
/// </summary>
public class AutomationDbContextFactory : IDesignTimeDbContextFactory<AutomationDbContext>
{
    public AutomationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AutomationDbContext>();
        // Choose provider from env var or fallback to SQLite
        // Prefer FD_MIGRATIONS_CONNECTION, then ConnectionStrings__DefaultConnection
        var cs = Environment.GetEnvironmentVariable("FD_MIGRATIONS_CONNECTION")
                 ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

        if (string.IsNullOrWhiteSpace(cs))
        {
            var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "data", "focusdeck.db");
            cs = $"Data Source={dbPath}";
        }

        if (cs.Contains("Host=", StringComparison.OrdinalIgnoreCase) || cs.Contains("Server=", StringComparison.OrdinalIgnoreCase))
        {
            optionsBuilder.UseNpgsql(cs);
        }
        else
        {
            optionsBuilder.UseSqlite(cs);
        }

        return new AutomationDbContext(optionsBuilder.Options);
    }
}
