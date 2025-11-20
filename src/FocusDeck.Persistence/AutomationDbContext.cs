using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using FocusDeck.Domain.Entities;
using FocusDeck.Domain.Entities.Automations;
using FocusDeck.Domain.Entities.Auth;
using FocusDeck.Domain.Entities.Remote;
using FocusDeck.Domain.Entities.Sync;
using FocusDeck.Domain.Entities.Context;
using FocusDeck.Domain.Entities.Jarvis;
using FocusDeck.SharedKernel.Auditing;
using FocusDeck.SharedKernel.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace FocusDeck.Persistence;

public class AutomationDbContext : DbContext
{
    private readonly ICurrentTenant _currentTenant;
    private readonly IAuditActorProvider? _actorProvider;
    // Tables that persist DateTime values using native timestamp columns in PostgreSQL.
    private static readonly IReadOnlyDictionary<Type, HashSet<string>> NativeTimestampProperties =
        new Dictionary<Type, HashSet<string>>
        {
            [typeof(PakeCredential)] = new HashSet<string>(StringComparer.Ordinal)
            {
                nameof(PakeCredential.CreatedAt),
                nameof(PakeCredential.UpdatedAt)
            }
        };

    public AutomationDbContext(DbContextOptions<AutomationDbContext> options, ICurrentTenant? currentTenant = null, IAuditActorProvider? actorProvider = null)
        : base(options)
    {
        _currentTenant = currentTenant ?? NullCurrentTenant.Instance;
        _actorProvider = actorProvider;
    }
    public DbSet<ConnectedService> ConnectedServices { get; set; }
    public DbSet<Automation> Automations { get; set; }
    public DbSet<AutomationProposal> AutomationProposals { get; set; }
    public DbSet<AutomationExecution> AutomationExecutions { get; set; }
    public DbSet<ServiceConfiguration> ServiceConfigurations { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<StudySession> StudySessions { get; set; }
    public DbSet<RefreshToken> RefreshTokens { get; set; }
    public DbSet<Asset> Assets { get; set; }
    public DbSet<Course> Courses { get; set; }
    public DbSet<Lecture> Lectures { get; set; }
    public DbSet<ReviewPlan> ReviewPlans { get; set; }
    public DbSet<ReviewSession> ReviewSessions { get; set; }
    public DbSet<TodoItem> TodoItems { get; set; }

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

    // Browser Bridge & Memory Vault
    public DbSet<Project> Projects { get; set; }
    public DbSet<CapturedItem> CapturedItems { get; set; }

    // Context aggregation snapshots
    public DbSet<FocusDeck.Domain.Entities.StudentContext> StudentContexts { get; set; }

    // Wellness metrics / Burnout detection
    public DbSet<StudentWellnessMetrics> StudentWellnessMetrics { get; set; }

    // Activity signals (Phase 4 – Activity Detection)
    public DbSet<ActivitySignal> ActivitySignals { get; set; }

    // Privacy and consent settings
    public DbSet<PrivacySetting> PrivacySettings { get; set; }

    // Jarvis workflow runs
    public DbSet<JarvisWorkflowRun> JarvisWorkflowRuns { get; set; }

    // Context Snapshot
    public DbSet<ContextSnapshot> ContextSnapshots { get; set; }
    public DbSet<ContextSlice> ContextSlices { get; set; }
    public DbSet<ContextVector> ContextVectors { get; set; }

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

        modelBuilder.Entity<ContextSlice>()
            .HasOne<ContextSnapshot>()
            .WithMany(s => s.Slices)
            .HasForeignKey(s => s.SnapshotId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ContextVector>()
            .HasOne(v => v.Snapshot)
            .WithMany()
            .HasForeignKey(v => v.SnapshotId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CapturedItem>()
            .HasOne(c => c.Project)
            .WithMany()
            .HasForeignKey(c => c.ProjectId)
            .OnDelete(DeleteBehavior.SetNull);

        if (Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) == true)
        {
            ApplyLegacyDatabaseCompatibilityConverters(modelBuilder);
        }
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
        => _actorProvider?.GetActorId();

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

    private static bool ShouldUseNativeTimestamp(IMutableProperty property)
    {
        var declaringType = property.DeclaringType?.ClrType;
        if (declaringType == null)
        {
            return false;
        }

        return NativeTimestampProperties.TryGetValue(declaringType, out var props) && props.Contains(property.Name);
    }

    private static void ApplyLegacyDatabaseCompatibilityConverters(ModelBuilder modelBuilder)
    {
        var boolConverter = new ValueConverter<bool, int>(
            v => v ? 1 : 0,
            v => v == 1);

        var nullableBoolConverter = new ValueConverter<bool?, int?>(
            v => v.HasValue ? (v.Value ? 1 : 0) : null,
            v => v.HasValue ? v.Value == 1 : (bool?)null);

        var guidConverter = new ValueConverter<Guid, string>(
            v => v.ToString(),
            v => string.IsNullOrWhiteSpace(v) ? Guid.Empty : Guid.Parse(v));

        var nullableGuidConverter = new ValueConverter<Guid?, string?>(
            v => v.HasValue ? v.Value.ToString() : null,
            v => string.IsNullOrWhiteSpace(v) ? (Guid?)null : Guid.Parse(v));

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(bool))
                {
                    property.SetValueConverter(boolConverter);
                    property.SetProviderClrType(typeof(int));
                }
                else if (property.ClrType == typeof(bool?))
                {
                    property.SetValueConverter(nullableBoolConverter);
                    property.SetProviderClrType(typeof(int?));
                }
                else if (property.ClrType == typeof(Guid))
                {
                    property.SetValueConverter(guidConverter);
                    property.SetProviderClrType(typeof(string));
                }
                else if (property.ClrType == typeof(Guid?))
                {
                    property.SetValueConverter(nullableGuidConverter);
                    property.SetProviderClrType(typeof(string));
                }
                else if (property.ClrType == typeof(DateTime))
                {
                    if (ShouldUseNativeTimestamp(property))
                    {
                        continue;
                    }

                    property.SetValueConverter(new ValueConverter<DateTime, string>(
                        v => v.ToUniversalTime().ToString("O"),
                        v => string.IsNullOrWhiteSpace(v) ? DateTime.MinValue : DateTime.Parse(v, null, DateTimeStyles.RoundtripKind)));
                    property.SetProviderClrType(typeof(string));
                }
                else if (property.ClrType == typeof(DateTime?))
                {
                    if (ShouldUseNativeTimestamp(property))
                    {
                        continue;
                    }

                    property.SetValueConverter(new ValueConverter<DateTime?, string?>(
                        v => v.HasValue ? v.Value.ToUniversalTime().ToString("O") : null,
                        v => string.IsNullOrWhiteSpace(v) ? (DateTime?)null : DateTime.Parse(v, null, DateTimeStyles.RoundtripKind)));
                    property.SetProviderClrType(typeof(string));
                }
            }
        }
    }
}
