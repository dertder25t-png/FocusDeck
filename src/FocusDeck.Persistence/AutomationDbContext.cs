using FocusDeck.Domain.Entities;
using FocusDeck.Domain.Entities.Automations;
using FocusDeck.Domain.Entities.Sync;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Persistence;

public class AutomationDbContext : DbContext
{
    public AutomationDbContext(DbContextOptions<AutomationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Automation> Automations { get; set; }
    public DbSet<AutomationExecution> AutomationExecutions { get; set; }
    public DbSet<ConnectedService> ConnectedServices { get; set; }
    public DbSet<ServiceConfiguration> ServiceConfigurations { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<StudySession> StudySessions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Asset> Assets { get; set; }

    // Sync tables
    public DbSet<DeviceRegistration> DeviceRegistrations { get; set; }
    public DbSet<SyncTransaction> SyncTransactions { get; set; }
    public DbSet<SyncChange> SyncChanges { get; set; }
    public DbSet<SyncMetadata> SyncMetadata { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the Configurations folder
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AutomationDbContext).Assembly);
    }
}
