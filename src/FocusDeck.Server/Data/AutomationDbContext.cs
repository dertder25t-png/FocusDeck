using Microsoft.EntityFrameworkCore;
using FocusDeck.Shared.Models.Automations;
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
