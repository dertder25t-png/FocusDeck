using Microsoft.EntityFrameworkCore;
using FocusDeck.Shared.Models;
using FocusDeck.Shared.Models.Automations;
using FocusDeck.Shared.Models.Sync;
using FocusDeck.Server.Models;
using System.Text.Json;

namespace FocusDeck.Server.Data;

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
    
    // Sync tables
    public DbSet<DeviceRegistration> DeviceRegistrations { get; set; }
    public DbSet<SyncTransaction> SyncTransactions { get; set; }
    public DbSet<SyncChange> SyncChanges { get; set; }
    public DbSet<SyncMetadata> SyncMetadata { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Automation
        modelBuilder.Entity<Automation>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.IsEnabled).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            // Store trigger and actions as JSON
            entity.Property(e => e.Trigger)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<AutomationTrigger>(v, (JsonSerializerOptions?)null)!)
                .HasColumnType("TEXT");
            
            entity.Property(e => e.Actions)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<AutomationAction>>(v, (JsonSerializerOptions?)null)!)
                .HasColumnType("TEXT");
            
            entity.HasIndex(e => e.IsEnabled);
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure AutomationExecution
        modelBuilder.Entity<AutomationExecution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.AutomationId).IsRequired();
            entity.Property(e => e.ExecutedAt).IsRequired();
            entity.Property(e => e.Success).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            entity.Property(e => e.DurationMs).IsRequired();
            entity.Property(e => e.TriggerData).HasMaxLength(1000);
            
            entity.HasIndex(e => e.AutomationId);
            entity.HasIndex(e => e.ExecutedAt);
            entity.HasIndex(e => new { e.AutomationId, e.ExecutedAt });
        });

        // Configure ConnectedService
        modelBuilder.Entity<ConnectedService>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Service).IsRequired();
            entity.Property(e => e.AccessToken).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.RefreshToken).IsRequired().HasMaxLength(1000);
            entity.Property(e => e.ExpiresAt);
            entity.Property(e => e.ConnectedAt).IsRequired();
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(200);
            
            entity.HasIndex(e => e.Service);
            entity.HasIndex(e => e.UserId);
        });

        // Configure ServiceConfiguration
        modelBuilder.Entity<ServiceConfiguration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ServiceName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ClientId).HasMaxLength(500);
            entity.Property(e => e.ClientSecret).HasMaxLength(500);
            entity.Property(e => e.ApiKey).HasMaxLength(500);
            entity.Property(e => e.AdditionalConfig);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            
            entity.HasIndex(e => e.ServiceName).IsUnique();
        });

        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnType("TEXT");
            entity.Property(e => e.Title).IsRequired().HasMaxLength(300);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Color).HasMaxLength(32);
            entity.Property(e => e.IsPinned).HasDefaultValue(false);
            entity.Property(e => e.CreatedDate).IsRequired();
            entity.Property(e => e.LastModified);
            entity.Property(e => e.Tags)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("TEXT");
            entity.Property(e => e.Bookmarks)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<NoteBookmark>>(v, (JsonSerializerOptions?)null) ?? new List<NoteBookmark>())
                .HasColumnType("TEXT");

            entity.HasIndex(e => e.IsPinned);
            entity.HasIndex(e => e.CreatedDate);
            entity.HasIndex(e => e.LastModified);
        });

        modelBuilder.Entity<StudySession>(entity =>
        {
            entity.HasKey(e => e.SessionId);
            entity.Property(e => e.SessionId).ValueGeneratedNever();
            entity.Property(e => e.StartTime).IsRequired();
            entity.Property(e => e.EndTime);
            entity.Property(e => e.DurationMinutes).IsRequired();
            entity.Property(e => e.SessionNotes);
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
            entity.Property(e => e.FocusRate);
            entity.Property(e => e.BreaksCount).HasDefaultValue(0);
            entity.Property(e => e.BreakDurationMinutes).HasDefaultValue(0);
            entity.Property(e => e.Category).HasMaxLength(120);

            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Category);
        });

        // Configure DeviceRegistration
        modelBuilder.Entity<DeviceRegistration>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DeviceName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Platform).IsRequired();
            entity.Property(e => e.UserId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.RegisteredAt).IsRequired();
            entity.Property(e => e.LastSyncAt).IsRequired();
            entity.Property(e => e.IsActive).IsRequired();
            entity.Property(e => e.AppVersion).HasMaxLength(50);
            
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => new { e.DeviceId, e.UserId });
        });

        // Configure SyncTransaction
        modelBuilder.Entity<SyncTransaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Timestamp).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.ErrorMessage).HasMaxLength(2000);
            
            entity.Ignore(e => e.Changes); // Changes stored separately in SyncChanges table
            
            entity.HasIndex(e => e.DeviceId);
            entity.HasIndex(e => e.Timestamp);
        });

        // Configure SyncChange
        modelBuilder.Entity<SyncChange>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TransactionId).IsRequired();
            entity.Property(e => e.EntityType).IsRequired();
            entity.Property(e => e.EntityId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Operation).IsRequired();
            entity.Property(e => e.DataJson).IsRequired();
            entity.Property(e => e.ChangedAt).IsRequired();
            entity.Property(e => e.ChangeVersion).IsRequired();
            
            entity.HasIndex(e => e.TransactionId);
            entity.HasIndex(e => e.EntityId);
            entity.HasIndex(e => e.ChangeVersion);
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
        });

        // Configure SyncMetadata
        modelBuilder.Entity<SyncMetadata>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.DeviceId).IsRequired().HasMaxLength(200);
            entity.Property(e => e.LastSyncVersion).IsRequired();
            entity.Property(e => e.LastSyncTime).IsRequired();
            entity.Property(e => e.EntityVersions)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<SyncEntityType, long>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<SyncEntityType, long>())
                .HasColumnType("TEXT");
            
            entity.HasIndex(e => e.DeviceId).IsUnique();
        });
    }
}

/// <summary>
/// Tracks execution history for automations
/// </summary>
public class AutomationExecution
{
    public int Id { get; set; }
    public Guid AutomationId { get; set; }
    public DateTime ExecutedAt { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long DurationMs { get; set; }
    public string? TriggerData { get; set; }
}
