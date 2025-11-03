using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FocusDeck.Persistence;

/// <summary>
/// Design-time factory for creating AutomationDbContext instances during migrations
/// </summary>
public class AutomationDbContextFactory : IDesignTimeDbContextFactory<AutomationDbContext>
{
    public AutomationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AutomationDbContext>();
        
        // Use SQLite for design-time (migrations)
        // Connection string doesn't need to be real for migration generation
        optionsBuilder.UseSqlite("Data Source=focusdeck.db");

        return new AutomationDbContext(optionsBuilder.Options);
    }
}
