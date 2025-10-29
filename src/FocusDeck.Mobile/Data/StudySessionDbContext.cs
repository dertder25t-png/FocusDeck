using Microsoft.EntityFrameworkCore;
using FocusDeck.Shared.Models;

namespace FocusDeck.Mobile.Data;

/// <summary>
/// Database context for FocusDeck Mobile application.
/// Manages StudySession entities and database operations using Entity Framework Core with SQLite.
/// </summary>
public class StudySessionDbContext : DbContext
{
    /// <summary>
    /// Gets or sets the DbSet of study sessions.
    /// </summary>
    public DbSet<StudySession> StudySessions { get; set; } = null!;

    /// <summary>
    /// Gets the path to the SQLite database file.
    /// Database is stored in the application's local data directory.
    /// </summary>
    private static string DbPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) ?? 
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "FocusDeck",
        "focusdeck.db"
    );

    /// <summary>
    /// Configures the database connection and model options.
    /// Uses SQLite with the database file stored in LocalApplicationData.
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        // Ensure the database directory exists
        var dir = Path.GetDirectoryName(DbPath);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // Configure SQLite connection with journaling and caching settings
        options.UseSqlite($"Data Source={DbPath}", sqliteOptions =>
        {
            sqliteOptions.CommandTimeout(30);
        });

        // Enable lazy loading for navigation properties
        options.UseLazyLoadingProxies();
    }

    /// <summary>
    /// Configures the model schema and relationships.
    /// Sets up StudySession entity with constraints and defaults.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure StudySession entity
        modelBuilder.Entity<StudySession>(entity =>
        {
            // Primary key
            entity.HasKey(e => e.SessionId);

            // Column configurations
            entity.Property(e => e.SessionId)
                .HasDefaultValueSql("(lower(hex(randomblob(16))))")
                .ValueGeneratedOnAdd();

            entity.Property(e => e.StartTime)
                .IsRequired()
                .HasColumnType("datetime");

            entity.Property(e => e.EndTime)
                .HasColumnType("datetime");

            entity.Property(e => e.DurationMinutes)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.SessionNotes)
                .HasColumnType("text")
                .IsRequired(false);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasConversion<int>()
                .HasDefaultValue(SessionStatus.Active);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.FocusRate)
                .IsRequired(false)
                .HasDefaultValue(null);

            entity.Property(e => e.BreaksCount)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.BreakDurationMinutes)
                .IsRequired()
                .HasDefaultValue(0);

            entity.Property(e => e.Category)
                .HasColumnType("text")
                .IsRequired(false);

            // Indexes for common queries
            entity.HasIndex(e => e.StartTime)
                .HasDatabaseName("IX_StudySessions_StartTime");

            entity.HasIndex(e => e.Status)
                .HasDatabaseName("IX_StudySessions_Status");

            entity.HasIndex(e => e.CreatedAt)
                .HasDatabaseName("IX_StudySessions_CreatedAt");

            // Table name configuration
            entity.ToTable("StudySessions");
        });
    }

    /// <summary>
    /// Creates or migrates the database schema.
    /// Should be called once during application initialization.
    /// </summary>
    public async Task InitializeDatabaseAsync()
    {
        try
        {
            // Create the database if it doesn't exist
            await Database.EnsureCreatedAsync();

            // Apply pending migrations
            await Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database initialization error: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets the database file path for diagnostic purposes.
    /// </summary>
    public static string GetDatabasePath() => DbPath;
}
