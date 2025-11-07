using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusDeck.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialAuthMigration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent initial create for authentication-related tables (works on SQLite/PostgreSQL)
            // Ensure tables exist before creating indexes so the migration can be applied on a fresh DB
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS PakeCredentials (
                    UserId TEXT PRIMARY KEY,
                    SaltBase64 TEXT NOT NULL,
                    VerifierBase64 TEXT NOT NULL,
                    Algorithm TEXT NOT NULL,
                    ModulusHex TEXT NOT NULL,
                    Generator INTEGER NOT NULL,
                    KdfParametersJson TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS KeyVaults (
                    UserId TEXT PRIMARY KEY,
                    VaultDataBase64 TEXT NOT NULL,
                    Version INTEGER NOT NULL DEFAULT 1,
                    CipherSuite TEXT NOT NULL DEFAULT 'AES-256-GCM',
                    KdfMetadataJson TEXT,
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL
                );
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS PairingSessions (
                    Id TEXT PRIMARY KEY,
                    UserId TEXT NOT NULL,
                    Code TEXT NOT NULL,
                    Status INTEGER NOT NULL,
                    CreatedAt TEXT NOT NULL,
                    ExpiresAt TEXT NOT NULL,
                    SourceDeviceId TEXT,
                    TargetDeviceId TEXT,
                    VaultDataBase64 TEXT,
                    VaultKdfMetadataJson TEXT,
                    VaultCipherSuite TEXT
                );
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS RevokedAccessTokens (
                    Id TEXT PRIMARY KEY,
                    Jti TEXT NOT NULL,
                    UserId TEXT NOT NULL,
                    RevokedAt TEXT NOT NULL,
                    ExpiresUtc TEXT NOT NULL
                );
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS RefreshTokens (
                    Id TEXT PRIMARY KEY,
                    UserId TEXT NOT NULL,
                    TokenHash TEXT NOT NULL,
                    ClientFingerprint TEXT NOT NULL,
                    DeviceId TEXT,
                    DeviceName TEXT,
                    DevicePlatform TEXT,
                    IssuedUtc TEXT NOT NULL,
                    ExpiresUtc TEXT NOT NULL,
                    LastAccessUtc TEXT,
                    RevokedUtc TEXT,
                    ReplacedByTokenHash TEXT
                );
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS AuthEventLogs (
                    Id TEXT PRIMARY KEY,
                    EventType TEXT NOT NULL,
                    UserId TEXT,
                    OccurredAtUtc TEXT NOT NULL,
                    IsSuccess INTEGER NOT NULL,
                    FailureReason TEXT,
                    RemoteIp TEXT,
                    DeviceId TEXT,
                    DeviceName TEXT,
                    UserAgent TEXT,
                    MetadataJson TEXT
                );
            ");

            // Indexes (idempotent)
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS IX_RevokedAccessTokens_Jti ON RevokedAccessTokens (Jti);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_RevokedAccessTokens_ExpiresUtc ON RevokedAccessTokens (ExpiresUtc);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_PairingSessions_UserId_Code_Status ON PairingSessions (UserId, Code, Status);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_PairingSessions_Code ON PairingSessions (Code);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_PairingSessions_ExpiresAt ON PairingSessions (ExpiresAt);");
            migrationBuilder.Sql("CREATE UNIQUE INDEX IF NOT EXISTS IX_RefreshTokens_TokenHash ON RefreshTokens (TokenHash);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_RefreshTokens_UserId ON RefreshTokens (UserId);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_RefreshTokens_ExpiresUtc ON RefreshTokens (ExpiresUtc);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_RefreshTokens_RevokedUtc ON RefreshTokens (RevokedUtc);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_RefreshTokens_DeviceId ON RefreshTokens (DeviceId);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_AuthEventLogs_UserId ON AuthEventLogs (UserId);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_AuthEventLogs_EventType ON AuthEventLogs (EventType);");
            migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS IX_AuthEventLogs_OccurredAtUtc ON AuthEventLogs (OccurredAtUtc);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_RevokedAccessTokens_ExpiresUtc;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_RevokedAccessTokens_Jti;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_PairingSessions_Code;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_PairingSessions_ExpiresAt;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_PairingSessions_UserId_Code_Status;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_RefreshTokens_TokenHash;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_RefreshTokens_UserId;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_RefreshTokens_ExpiresUtc;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_RefreshTokens_RevokedUtc;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_RefreshTokens_DeviceId;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_AuthEventLogs_UserId;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_AuthEventLogs_EventType;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS IX_AuthEventLogs_OccurredAtUtc;");
        }
    }
}
