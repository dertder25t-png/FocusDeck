using FocusDeck.Domain.Entities;
using FocusDeck.Domain.Entities.Automations;
using FocusDeck.Domain.Entities.Sync;
using FocusDeck.Domain.Entities.Remote;
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
    public DbSet<Course> Courses { get; set; }
    public DbSet<Lecture> Lectures { get; set; }
    public DbSet<ReviewPlan> ReviewPlans { get; set; }
    public DbSet<ReviewSession> ReviewSessions { get; set; }

    // Sync tables
    public DbSet<DeviceRegistration> DeviceRegistrations { get; set; }
    public DbSet<SyncTransaction> SyncTransactions { get; set; }
    public DbSet<SyncChange> SyncChanges { get; set; }
    public DbSet<SyncMetadata> SyncMetadata { get; set; }
    public DbSet<SyncVersion> SyncVersions { get; set; }

    // Remote control tables
    public DbSet<DeviceLink> DeviceLinks { get; set; }
    public DbSet<RemoteAction> RemoteActions { get; set; }

    // Focus session tables
    public DbSet<FocusSession> FocusSessions { get; set; }
    public DbSet<FocusPolicyTemplate> FocusPolicyTemplates { get; set; }

    // Multi-tenancy tables
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<OrgUser> OrgUsers { get; set; }
    public DbSet<Invite> Invites { get; set; }

    // Note suggestions
    public DbSet<NoteSuggestion> NoteSuggestions { get; set; }

    // Design projects
    public DbSet<DesignProject> DesignProjects { get; set; }
    public DbSet<DesignIdea> DesignIdeas { get; set; }

    // Context aggregation snapshots
    public DbSet<FocusDeck.Domain.Entities.StudentContext> StudentContexts { get; set; }

    // Auth / PAKE
    public DbSet<FocusDeck.Domain.Entities.Auth.PakeCredential> PakeCredentials { get; set; }
    public DbSet<FocusDeck.Domain.Entities.Auth.KeyVault> KeyVaults { get; set; }
    public DbSet<FocusDeck.Domain.Entities.Auth.PairingSession> PairingSessions { get; set; }
    public DbSet<FocusDeck.Domain.Entities.Auth.RevokedAccessToken> RevokedAccessTokens { get; set; }
    public DbSet<FocusDeck.Domain.Entities.Auth.AuthEventLog> AuthEventLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the Configurations folder
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AutomationDbContext).Assembly);
    }
}
