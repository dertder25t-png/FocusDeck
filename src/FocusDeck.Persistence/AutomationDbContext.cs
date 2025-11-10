using System;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading;
using FocusDeck.Domain.Entities;
using FocusDeck.Domain.Entities.Automations;
using FocusDeck.Domain.Entities.Remote;
using FocusDeck.Domain.Entities.Sync;
using FocusDeck.SharedKernel.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace FocusDeck.Persistence;

public class AutomationDbContext : DbContext
{
    private readonly ICurrentTenant _currentTenant;
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public AutomationDbContext(DbContextOptions<AutomationDbContext> options, ICurrentTenant? currentTenant = null, IHttpContextAccessor? httpContextAccessor = null)
        : base(options)
    {
        _currentTenant = currentTenant ?? NullCurrentTenant.Instance;
        _httpContextAccessor = httpContextAccessor;
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
    public DbSet<Tenant> Tenants { get; set; }
    public DbSet<TenantUser> TenantUsers { get; set; }
    public DbSet<UserTenant> UserTenants { get; set; }
    public DbSet<TenantInvite> TenantInvites { get; set; }

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
    public DbSet<TenantAudit> TenantAudits { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from the Configurations folder
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AutomationDbContext).Assembly);

        ApplyTenantQueryFilters(modelBuilder);
    }

    public override int SaveChanges()
    {
        ApplyTenantStamps();
        AuditTenantEntries();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyTenantStamps();
        AuditTenantEntries();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void ApplyTenantStamps()
    {
        if (!_currentTenant.HasTenant)
        {
            return;
        }

        foreach (var entry in ChangeTracker.Entries<IMustHaveTenant>())
        {
            if (entry.State == EntityState.Added && entry.Entity.TenantId == Guid.Empty)
            {
                entry.Entity.TenantId = _currentTenant.TenantId!.Value;
            }
        }
    }
    private void AuditTenantEntries()
    {
        var entries = ChangeTracker.Entries()
            .Where(e => e.Entity is IMustHaveTenant && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in entries)
        {
            var tenantEntry = (IMustHaveTenant)entry.Entity;
            var entityId = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey())?.CurrentValue?.ToString() ?? Guid.NewGuid().ToString();

            var audit = new TenantAudit
            {
                Id = Guid.NewGuid(),
                TenantId = tenantEntry.TenantId,
                EntityType = entry.Entity.GetType().Name,
                EntityId = entityId,
                Action = entry.State switch
                {
                    EntityState.Added => "Created",
                    EntityState.Modified => "Updated",
                    EntityState.Deleted => "Deleted",
                    _ => "Unknown"
                },
                Timestamp = DateTime.UtcNow,
                ActorId = GetActorId()
            };

            TenantAudits.Add(audit);
        }
    }

    private string? GetActorId()
    {
        return _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    private void ApplyTenantQueryFilters(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(IMustHaveTenant).IsAssignableFrom(entityType.ClrType))
            {
                var methodInfo = typeof(AutomationDbContext)
                    .GetMethod(nameof(SetTenantQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(entityType.ClrType);

                methodInfo.Invoke(this, new object[] { modelBuilder });
            }
        }
    }

    private void SetTenantQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, IMustHaveTenant
    {
        modelBuilder.Entity<TEntity>()
            .HasQueryFilter(entity => !_currentTenant.HasTenant || entity.TenantId == _currentTenant.TenantId);
    }
}
