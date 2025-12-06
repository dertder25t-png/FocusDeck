using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace FocusDeck.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivitySignals",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SignalType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SignalValue = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CapturedAtUtc = table.Column<string>(type: "text", nullable: false),
                    SourceApp = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    MetadataJson = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivitySignals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Assets",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    SizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    StoragePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    UploadedAt = table.Column<string>(type: "text", nullable: false),
                    UploadedBy = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthEventLogs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    UserId = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: true),
                    OccurredAtUtc = table.Column<string>(type: "text", nullable: false),
                    IsSuccess = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    RemoteIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    DeviceId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    DeviceName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    UserAgent = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthEventLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutomationExecutions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AutomationId = table.Column<string>(type: "text", nullable: false),
                    ExecutedAt = table.Column<string>(type: "text", nullable: false),
                    Success = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    TriggerData = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationExecutions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AutomationProposals",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    YamlDefinition = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ConfidenceScore = table.Column<float>(type: "real", nullable: false),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutomationProposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Automations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    IsEnabled = table.Column<int>(type: "integer", nullable: false),
                    YamlDefinition = table.Column<string>(type: "text", nullable: false),
                    Trigger = table.Column<string>(type: "TEXT", nullable: false),
                    Actions = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<string>(type: "text", nullable: false),
                    LastRunAt = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Automations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BrowserSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    EndedAt = table.Column<string>(type: "text", nullable: true),
                    DeviceId = table.Column<string>(type: "text", nullable: false),
                    TabsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrowserSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CalendarSources",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Provider = table.Column<string>(type: "text", nullable: false),
                    ExternalId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    TokenExpiry = table.Column<string>(type: "text", nullable: true),
                    SyncToken = table.Column<string>(type: "text", nullable: true),
                    LastSync = table.Column<string>(type: "text", nullable: false),
                    IsPrimary = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalendarSources", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConnectedServices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Service = table.Column<int>(type: "integer", nullable: false),
                    AccessToken = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    RefreshToken = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ExpiresAt = table.Column<string>(type: "text", nullable: true),
                    ConnectedAt = table.Column<string>(type: "text", nullable: false),
                    MetadataJson = table.Column<string>(type: "text", nullable: true),
                    IsConfigured = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConnectedServices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ContextSnapshots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Metadata_DeviceName = table.Column<string>(type: "text", nullable: true),
                    Metadata_OperatingSystem = table.Column<string>(type: "text", nullable: true),
                    VectorizationState = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContextSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseIndex",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Code = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Keywords = table.Column<List<string>>(type: "text[]", nullable: false),
                    SchedulePattern = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseIndex", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Courses",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Instructor = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Courses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DesignProjects",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    GoalsText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Vibes = table.Column<string>(type: "text", nullable: false),
                    RequirementsText = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    BrandKeywords = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesignProjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceJobs",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TargetDeviceId = table.Column<string>(type: "text", nullable: false),
                    JobType = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ResultJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    CompletedAt = table.Column<string>(type: "text", nullable: true),
                    ExpiresAt = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceJobs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceLinks",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DeviceType = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CapabilitiesJson = table.Column<string>(type: "text", nullable: false),
                    LastSeenUtc = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceLinks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DeviceRegistrations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DeviceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Platform = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RegisteredAt = table.Column<string>(type: "text", nullable: false),
                    LastSyncAt = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<int>(type: "integer", nullable: false),
                    AppVersion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeviceRegistrations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FocusPolicyTemplates",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Strict = table.Column<int>(type: "integer", nullable: false),
                    AutoBreak = table.Column<int>(type: "integer", nullable: false),
                    AutoDim = table.Column<int>(type: "integer", nullable: false),
                    NotifyPhone = table.Column<int>(type: "integer", nullable: false),
                    TargetDurationMinutes = table.Column<int>(type: "integer", nullable: true),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FocusPolicyTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FocusSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    StartTime = table.Column<string>(type: "text", nullable: false),
                    EndTime = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Policy = table.Column<string>(type: "text", nullable: false),
                    Signals = table.Column<string>(type: "text", nullable: false),
                    DistractionsCount = table.Column<int>(type: "integer", nullable: false),
                    LastRecoverySuggestionAt = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FocusSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JarvisRuns",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    EntryPoint = table.Column<string>(type: "text", nullable: false),
                    InputPayloadJson = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JarvisRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JarvisWorkflowRuns",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    WorkflowId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedAtUtc = table.Column<string>(type: "text", nullable: false),
                    StartedAtUtc = table.Column<string>(type: "text", nullable: true),
                    CompletedAtUtc = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LogSummary = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    JobId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JarvisWorkflowRuns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KeyVaults",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    VaultDataBase64 = table.Column<string>(type: "text", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    CipherSuite = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false, defaultValue: "AES-256-GCM"),
                    KdfMetadataJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KeyVaults", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Notes",
                columns: table => new
                {
                    Id = table.Column<string>(type: "TEXT", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Tags = table.Column<string>(type: "TEXT", nullable: false),
                    Color = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    IsPinned = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedDate = table.Column<string>(type: "text", nullable: false),
                    LastModified = table.Column<string>(type: "text", nullable: true),
                    Bookmarks = table.Column<string>(type: "TEXT", nullable: false),
                    CitationStyle = table.Column<string>(type: "text", nullable: true),
                    CourseId = table.Column<string>(type: "text", nullable: true),
                    EventId = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PairingSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    Code = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<string>(type: "text", nullable: false),
                    SourceDeviceId = table.Column<string>(type: "text", nullable: true),
                    TargetDeviceId = table.Column<string>(type: "text", nullable: true),
                    VaultDataBase64 = table.Column<string>(type: "text", nullable: true),
                    VaultKdfMetadataJson = table.Column<string>(type: "text", nullable: true),
                    VaultCipherSuite = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PairingSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PakeCredentials",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    SaltBase64 = table.Column<string>(type: "text", nullable: true),
                    VerifierBase64 = table.Column<string>(type: "text", nullable: true),
                    Algorithm = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ModulusHex = table.Column<string>(type: "text", nullable: false),
                    Generator = table.Column<int>(type: "integer", nullable: false),
                    KdfParametersJson = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PakeCredentials", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "PrivacySettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContextType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<int>(type: "integer", nullable: false),
                    Tier = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                    UpdatedAt = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacySettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Projects",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    RepoSlug = table.Column<string>(type: "text", nullable: true),
                    SortingMode = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Projects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ClientFingerprint = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DeviceName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    DevicePlatform = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IssuedUtc = table.Column<string>(type: "text", nullable: false),
                    ExpiresUtc = table.Column<string>(type: "text", nullable: false),
                    LastAccessUtc = table.Column<string>(type: "text", nullable: true),
                    RevokedUtc = table.Column<string>(type: "text", nullable: true),
                    ReplacedByTokenHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RemoteActions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    CompletedAt = table.Column<string>(type: "text", nullable: true),
                    Success = table.Column<int>(type: "integer", nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemoteActions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ReviewPlans",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TargetEntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    EntityType = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    CompletedAt = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RevokedAccessTokens",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Jti = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    UserId = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    RevokedAt = table.Column<string>(type: "text", nullable: false),
                    ExpiresUtc = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RevokedAccessTokens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ServiceConfigurations",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ServiceName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ClientId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ClientSecret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ApiKey = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AdditionalConfig = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceConfigurations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentContexts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Timestamp = table.Column<string>(type: "text", nullable: false),
                    FocusedAppName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    FocusedWindowTitle = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ActivityIntensity = table.Column<int>(type: "integer", nullable: false),
                    IsIdle = table.Column<int>(type: "integer", nullable: false),
                    OpenContextsJson = table.Column<string>(type: "text", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentContexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentWellnessMetrics",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CapturedAtUtc = table.Column<string>(type: "text", nullable: false),
                    HoursWorked = table.Column<double>(type: "double precision", nullable: false),
                    BreakFrequency = table.Column<double>(type: "double precision", nullable: false),
                    QualityScore = table.Column<double>(type: "double precision", nullable: false),
                    SleepHours = table.Column<double>(type: "double precision", nullable: false),
                    IsUnsustainable = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentWellnessMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudySessions",
                columns: table => new
                {
                    SessionId = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<string>(type: "text", nullable: false),
                    EndTime = table.Column<string>(type: "text", nullable: true),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    SessionNotes = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<string>(type: "text", nullable: false),
                    FocusRate = table.Column<int>(type: "integer", nullable: true),
                    BreaksCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    BreakDurationMinutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    Category = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudySessions", x => x.SessionId);
                });

            migrationBuilder.CreateTable(
                name: "SyncChanges",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TransactionId = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<int>(type: "integer", nullable: false),
                    EntityId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Operation = table.Column<int>(type: "integer", nullable: false),
                    DataJson = table.Column<string>(type: "text", nullable: false),
                    ChangedAt = table.Column<string>(type: "text", nullable: false),
                    ChangeVersion = table.Column<long>(type: "bigint", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncChanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncMetadata",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LastSyncVersion = table.Column<long>(type: "bigint", nullable: false),
                    LastSyncTime = table.Column<string>(type: "text", nullable: false),
                    EntityVersions = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncMetadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncTransactions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DeviceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Timestamp = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncTransactions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SyncVersions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SyncVersions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantAudits",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    EntityType = table.Column<string>(type: "text", nullable: false),
                    EntityId = table.Column<string>(type: "text", nullable: false),
                    Action = table.Column<string>(type: "text", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantAudits", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TenantUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Picture = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    LastLoginAt = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSettings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    GoogleApiKey = table.Column<string>(type: "text", nullable: true),
                    CanvasApiToken = table.Column<string>(type: "text", nullable: true),
                    HomeAssistantUrl = table.Column<string>(type: "text", nullable: true),
                    HomeAssistantToken = table.Column<string>(type: "text", nullable: true),
                    OpenAiKey = table.Column<string>(type: "text", nullable: true),
                    AnthropicKey = table.Column<string>(type: "text", nullable: true),
                    UpdatedAt = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSettings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventCache",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CalendarSourceId = table.Column<string>(type: "text", nullable: false),
                    ExternalEventId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    StartTime = table.Column<string>(type: "text", nullable: false),
                    EndTime = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Location = table.Column<string>(type: "text", nullable: true),
                    IsAllDay = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventCache", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EventCache_CalendarSources_CalendarSourceId",
                        column: x => x.CalendarSourceId,
                        principalTable: "CalendarSources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContextSlices",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    SnapshotId = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Data = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContextSlices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContextSlices_ContextSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "ContextSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ContextVectors",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    SnapshotId = table.Column<string>(type: "text", nullable: false),
                    VectorData = table.Column<byte[]>(type: "bytea", nullable: false),
                    Dimensions = table.Column<int>(type: "integer", nullable: false),
                    ModelName = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContextVectors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContextVectors_ContextSnapshots_SnapshotId",
                        column: x => x.SnapshotId,
                        principalTable: "ContextSnapshots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Lectures",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    CourseId = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    RecordedAt = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<string>(type: "text", nullable: true),
                    CreatedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    AudioAssetId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TranscriptionText = table.Column<string>(type: "text", nullable: true),
                    SummaryText = table.Column<string>(type: "text", nullable: true),
                    GeneratedNoteId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    DurationSeconds = table.Column<int>(type: "integer", nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Lectures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Lectures_Assets_AudioAssetId",
                        column: x => x.AudioAssetId,
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Lectures_Courses_CourseId",
                        column: x => x.CourseId,
                        principalTable: "Courses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DesignIdeas",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ProjectId = table.Column<string>(type: "text", nullable: false),
                    Type = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: false),
                    AssetId = table.Column<string>(type: "text", nullable: true),
                    Score = table.Column<double>(type: "double precision", nullable: true),
                    IsPinned = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DesignIdeas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DesignIdeas_DesignProjects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "DesignProjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "JarvisRunSteps",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    RunId = table.Column<string>(type: "text", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false),
                    StepType = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestJson = table.Column<string>(type: "jsonb", nullable: true),
                    ResponseJson = table.Column<string>(type: "jsonb", nullable: true),
                    ErrorJson = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JarvisRunSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JarvisRunSteps_JarvisRuns_RunId",
                        column: x => x.RunId,
                        principalTable: "JarvisRuns",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AcademicSources",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Author = table.Column<string>(type: "text", nullable: false),
                    Publisher = table.Column<string>(type: "text", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Doi = table.Column<string>(type: "text", nullable: false),
                    NoteId = table.Column<string>(type: "TEXT", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicSources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AcademicSources_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NoteSuggestions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    NoteId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    ContentMarkdown = table.Column<string>(type: "text", nullable: false),
                    Source = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Confidence = table.Column<double>(type: "double precision", nullable: false),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    AcceptedAt = table.Column<string>(type: "text", nullable: true),
                    AcceptedBy = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NoteSuggestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NoteSuggestions_Notes_NoteId",
                        column: x => x.NoteId,
                        principalTable: "Notes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CapturedItems",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Content = table.Column<string>(type: "text", nullable: true),
                    Summary = table.Column<string>(type: "text", nullable: true),
                    TagsJson = table.Column<string>(type: "text", nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ProjectId = table.Column<string>(type: "text", nullable: true),
                    SuggestedProjectId = table.Column<string>(type: "text", nullable: true),
                    SuggestionConfidence = table.Column<double>(type: "double precision", nullable: true),
                    SuggestionReason = table.Column<string>(type: "text", nullable: true),
                    CapturedAt = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CapturedItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CapturedItems_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ProjectResources",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ProjectId = table.Column<string>(type: "text", nullable: false),
                    ResourceType = table.Column<int>(type: "integer", nullable: false),
                    ResourceValue = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectResources", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProjectResources_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceSnapshots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    WindowLayoutJson = table.Column<string>(type: "text", nullable: false),
                    BrowserSessionId = table.Column<string>(type: "text", nullable: true),
                    ActiveNoteId = table.Column<string>(type: "TEXT", nullable: true),
                    ProjectId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkspaceSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkspaceSnapshots_BrowserSessions_BrowserSessionId",
                        column: x => x.BrowserSessionId,
                        principalTable: "BrowserSessions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkspaceSnapshots_Notes_ActiveNoteId",
                        column: x => x.ActiveNoteId,
                        principalTable: "Notes",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_WorkspaceSnapshots_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalTable: "Projects",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ReviewSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    ReviewPlanId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ScheduledDate = table.Column<string>(type: "text", nullable: false),
                    CompletedDate = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    TenantId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReviewSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ReviewSessions_ReviewPlans_ReviewPlanId",
                        column: x => x.ReviewPlanId,
                        principalTable: "ReviewPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenantInvites",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<string>(type: "text", nullable: false),
                    ExpiresAt = table.Column<string>(type: "text", nullable: false),
                    AcceptedAt = table.Column<string>(type: "text", nullable: true),
                    AcceptedByUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenantInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TenantInvites_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserTenants",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<string>(type: "text", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<int>(type: "integer", nullable: false),
                    JoinedAt = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserTenants", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserTenants_TenantUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "TenantUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserTenants_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AcademicSources_NoteId",
                table: "AcademicSources",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivitySignals_CapturedAtUtc",
                table: "ActivitySignals",
                column: "CapturedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ActivitySignals_TenantId",
                table: "ActivitySignals",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_UploadedAt",
                table: "Assets",
                column: "UploadedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Assets_UploadedBy",
                table: "Assets",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AuthEventLogs_EventType",
                table: "AuthEventLogs",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AuthEventLogs_OccurredAtUtc",
                table: "AuthEventLogs",
                column: "OccurredAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_AuthEventLogs_UserId",
                table: "AuthEventLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationExecutions_AutomationId",
                table: "AutomationExecutions",
                column: "AutomationId");

            migrationBuilder.CreateIndex(
                name: "IX_AutomationExecutions_AutomationId_ExecutedAt",
                table: "AutomationExecutions",
                columns: new[] { "AutomationId", "ExecutedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_AutomationExecutions_ExecutedAt",
                table: "AutomationExecutions",
                column: "ExecutedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Automations_CreatedAt",
                table: "Automations",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Automations_IsEnabled",
                table: "Automations",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_CapturedItems_ProjectId",
                table: "CapturedItems",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedServices_Service",
                table: "ConnectedServices",
                column: "Service");

            migrationBuilder.CreateIndex(
                name: "IX_ConnectedServices_UserId",
                table: "ConnectedServices",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ContextSlices_SnapshotId",
                table: "ContextSlices",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_ContextVectors_SnapshotId",
                table: "ContextVectors",
                column: "SnapshotId");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_Code",
                table: "Courses",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_Courses_CreatedAt",
                table: "Courses",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DesignIdeas_CreatedAt",
                table: "DesignIdeas",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DesignIdeas_ProjectId",
                table: "DesignIdeas",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_DesignIdeas_ProjectId_IsPinned",
                table: "DesignIdeas",
                columns: new[] { "ProjectId", "IsPinned" });

            migrationBuilder.CreateIndex(
                name: "IX_DesignProjects_CreatedAt",
                table: "DesignProjects",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_DesignProjects_UserId",
                table: "DesignProjects",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceLinks_UserId",
                table: "DeviceLinks",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceLinks_UserId_DeviceType",
                table: "DeviceLinks",
                columns: new[] { "UserId", "DeviceType" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceRegistrations_DeviceId",
                table: "DeviceRegistrations",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_DeviceRegistrations_DeviceId_UserId",
                table: "DeviceRegistrations",
                columns: new[] { "DeviceId", "UserId" });

            migrationBuilder.CreateIndex(
                name: "IX_DeviceRegistrations_UserId",
                table: "DeviceRegistrations",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_EventCache_CalendarSourceId",
                table: "EventCache",
                column: "CalendarSourceId");

            migrationBuilder.CreateIndex(
                name: "IX_FocusSessions_StartTime",
                table: "FocusSessions",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_FocusSessions_Status",
                table: "FocusSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FocusSessions_UserId",
                table: "FocusSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_FocusSessions_UserId_Status",
                table: "FocusSessions",
                columns: new[] { "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_JarvisRunSteps_RunId",
                table: "JarvisRunSteps",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_JarvisWorkflowRuns_RequestedAtUtc",
                table: "JarvisWorkflowRuns",
                column: "RequestedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_JarvisWorkflowRuns_Status",
                table: "JarvisWorkflowRuns",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_JarvisWorkflowRuns_TenantId",
                table: "JarvisWorkflowRuns",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_JarvisWorkflowRuns_WorkflowId",
                table: "JarvisWorkflowRuns",
                column: "WorkflowId");

            migrationBuilder.CreateIndex(
                name: "IX_Lectures_AudioAssetId",
                table: "Lectures",
                column: "AudioAssetId");

            migrationBuilder.CreateIndex(
                name: "IX_Lectures_CourseId",
                table: "Lectures",
                column: "CourseId");

            migrationBuilder.CreateIndex(
                name: "IX_Lectures_RecordedAt",
                table: "Lectures",
                column: "RecordedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Lectures_Status",
                table: "Lectures",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_CreatedDate",
                table: "Notes",
                column: "CreatedDate");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_IsPinned",
                table: "Notes",
                column: "IsPinned");

            migrationBuilder.CreateIndex(
                name: "IX_Notes_LastModified",
                table: "Notes",
                column: "LastModified");

            migrationBuilder.CreateIndex(
                name: "IX_NoteSuggestions_CreatedAt",
                table: "NoteSuggestions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_NoteSuggestions_NoteId",
                table: "NoteSuggestions",
                column: "NoteId");

            migrationBuilder.CreateIndex(
                name: "IX_PairingSessions_Code",
                table: "PairingSessions",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_PairingSessions_ExpiresAt",
                table: "PairingSessions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_PairingSessions_UserId_Code_Status",
                table: "PairingSessions",
                columns: new[] { "UserId", "Code", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_PrivacySettings_TenantId",
                table: "PrivacySettings",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_PrivacySettings_TenantId_UserId_ContextType",
                table: "PrivacySettings",
                columns: new[] { "TenantId", "UserId", "ContextType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PrivacySettings_UserId",
                table: "PrivacySettings",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectResources_ProjectId",
                table: "ProjectResources",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_DeviceId",
                table: "RefreshTokens",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_ExpiresUtc",
                table: "RefreshTokens",
                column: "ExpiresUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_RevokedUtc",
                table: "RefreshTokens",
                column: "RevokedUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenHash",
                table: "RefreshTokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UserId",
                table: "RefreshTokens",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RemoteActions_CreatedAt",
                table: "RemoteActions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RemoteActions_UserId",
                table: "RemoteActions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_RemoteActions_UserId_CompletedAt",
                table: "RemoteActions",
                columns: new[] { "UserId", "CompletedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ReviewPlans_CreatedAt",
                table: "ReviewPlans",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewPlans_TargetEntityId",
                table: "ReviewPlans",
                column: "TargetEntityId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewPlans_UserId",
                table: "ReviewPlans",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewSessions_ReviewPlanId",
                table: "ReviewSessions",
                column: "ReviewPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewSessions_ScheduledDate",
                table: "ReviewSessions",
                column: "ScheduledDate");

            migrationBuilder.CreateIndex(
                name: "IX_ReviewSessions_Status",
                table: "ReviewSessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RevokedAccessTokens_ExpiresUtc",
                table: "RevokedAccessTokens",
                column: "ExpiresUtc");

            migrationBuilder.CreateIndex(
                name: "IX_RevokedAccessTokens_Jti",
                table: "RevokedAccessTokens",
                column: "Jti",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceConfigurations_ServiceName",
                table: "ServiceConfigurations",
                column: "ServiceName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentContexts_User_Timestamp",
                table: "StudentContexts",
                columns: new[] { "UserId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_StudentWellnessMetrics_CapturedAtUtc",
                table: "StudentWellnessMetrics",
                column: "CapturedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_StudentWellnessMetrics_TenantId",
                table: "StudentWellnessMetrics",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentWellnessMetrics_UserId",
                table: "StudentWellnessMetrics",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_Category",
                table: "StudySessions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_StartTime",
                table: "StudySessions",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_StudySessions_Status",
                table: "StudySessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_SyncChanges_ChangeVersion",
                table: "SyncChanges",
                column: "ChangeVersion");

            migrationBuilder.CreateIndex(
                name: "IX_SyncChanges_EntityId",
                table: "SyncChanges",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncChanges_EntityType_EntityId",
                table: "SyncChanges",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_SyncChanges_TransactionId",
                table: "SyncChanges",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncMetadata_DeviceId",
                table: "SyncMetadata",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SyncTransactions_DeviceId",
                table: "SyncTransactions",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_SyncTransactions_Timestamp",
                table: "SyncTransactions",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvites_TenantId",
                table: "TenantInvites",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_TenantInvites_Token",
                table: "TenantInvites",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Slug",
                table: "Tenants",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TenantUsers_Email",
                table: "TenantUsers",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTenants_TenantId_UserId",
                table: "UserTenants",
                columns: new[] { "TenantId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserTenants_UserId",
                table: "UserTenants",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceSnapshots_ActiveNoteId",
                table: "WorkspaceSnapshots",
                column: "ActiveNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceSnapshots_BrowserSessionId",
                table: "WorkspaceSnapshots",
                column: "BrowserSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkspaceSnapshots_ProjectId",
                table: "WorkspaceSnapshots",
                column: "ProjectId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicSources");

            migrationBuilder.DropTable(
                name: "ActivitySignals");

            migrationBuilder.DropTable(
                name: "AuthEventLogs");

            migrationBuilder.DropTable(
                name: "AutomationExecutions");

            migrationBuilder.DropTable(
                name: "AutomationProposals");

            migrationBuilder.DropTable(
                name: "Automations");

            migrationBuilder.DropTable(
                name: "CapturedItems");

            migrationBuilder.DropTable(
                name: "ConnectedServices");

            migrationBuilder.DropTable(
                name: "ContextSlices");

            migrationBuilder.DropTable(
                name: "ContextVectors");

            migrationBuilder.DropTable(
                name: "CourseIndex");

            migrationBuilder.DropTable(
                name: "DesignIdeas");

            migrationBuilder.DropTable(
                name: "DeviceJobs");

            migrationBuilder.DropTable(
                name: "DeviceLinks");

            migrationBuilder.DropTable(
                name: "DeviceRegistrations");

            migrationBuilder.DropTable(
                name: "EventCache");

            migrationBuilder.DropTable(
                name: "FocusPolicyTemplates");

            migrationBuilder.DropTable(
                name: "FocusSessions");

            migrationBuilder.DropTable(
                name: "JarvisRunSteps");

            migrationBuilder.DropTable(
                name: "JarvisWorkflowRuns");

            migrationBuilder.DropTable(
                name: "KeyVaults");

            migrationBuilder.DropTable(
                name: "Lectures");

            migrationBuilder.DropTable(
                name: "NoteSuggestions");

            migrationBuilder.DropTable(
                name: "PairingSessions");

            migrationBuilder.DropTable(
                name: "PakeCredentials");

            migrationBuilder.DropTable(
                name: "PrivacySettings");

            migrationBuilder.DropTable(
                name: "ProjectResources");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "RemoteActions");

            migrationBuilder.DropTable(
                name: "ReviewSessions");

            migrationBuilder.DropTable(
                name: "RevokedAccessTokens");

            migrationBuilder.DropTable(
                name: "ServiceConfigurations");

            migrationBuilder.DropTable(
                name: "StudentContexts");

            migrationBuilder.DropTable(
                name: "StudentWellnessMetrics");

            migrationBuilder.DropTable(
                name: "StudySessions");

            migrationBuilder.DropTable(
                name: "SyncChanges");

            migrationBuilder.DropTable(
                name: "SyncMetadata");

            migrationBuilder.DropTable(
                name: "SyncTransactions");

            migrationBuilder.DropTable(
                name: "SyncVersions");

            migrationBuilder.DropTable(
                name: "TenantAudits");

            migrationBuilder.DropTable(
                name: "TenantInvites");

            migrationBuilder.DropTable(
                name: "UserSettings");

            migrationBuilder.DropTable(
                name: "UserTenants");

            migrationBuilder.DropTable(
                name: "WorkspaceSnapshots");

            migrationBuilder.DropTable(
                name: "ContextSnapshots");

            migrationBuilder.DropTable(
                name: "DesignProjects");

            migrationBuilder.DropTable(
                name: "CalendarSources");

            migrationBuilder.DropTable(
                name: "JarvisRuns");

            migrationBuilder.DropTable(
                name: "Assets");

            migrationBuilder.DropTable(
                name: "Courses");

            migrationBuilder.DropTable(
                name: "ReviewPlans");

            migrationBuilder.DropTable(
                name: "TenantUsers");

            migrationBuilder.DropTable(
                name: "Tenants");

            migrationBuilder.DropTable(
                name: "BrowserSessions");

            migrationBuilder.DropTable(
                name: "Notes");

            migrationBuilder.DropTable(
                name: "Projects");
        }
    }
}
