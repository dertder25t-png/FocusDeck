using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusDeck.Persistence.Migrations
{
    public partial class BackfillPakeSaltFromKdf : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Different SQL for providers: SQLite, PostgreSQL, SQL Server
            if (ActiveProvider.Contains("Sqlite"))
            {
                // SQLite: use json_extract
                migrationBuilder.Sql(@"
                    UPDATE PakeCredentials
                    SET SaltBase64 = json_extract(KdfParametersJson, '$.salt')
                    WHERE (SaltBase64 IS NULL OR SaltBase64 = '')
                      AND KdfParametersJson IS NOT NULL
                      AND json_extract(KdfParametersJson, '$.salt') IS NOT NULL;
                ");
            }
            else if (ActiveProvider.Contains("Npgsql"))
            {
                // PostgreSQL: use ->> operator
                migrationBuilder.Sql(@"
                    UPDATE ""PakeCredentials""
                    SET ""SaltBase64"" = (KdfParametersJson->>'salt')
                    WHERE (""SaltBase64"" IS NULL OR ""SaltBase64"" = '')
                      AND KdfParametersJson IS NOT NULL
                      AND (KdfParametersJson->>'salt') IS NOT NULL;
                ");
            }
            else if (ActiveProvider.Contains("SqlServer"))
            {
                // SQL Server: JSON_VALUE
                migrationBuilder.Sql(@"
                    UPDATE PakeCredentials
                    SET SaltBase64 = JSON_VALUE(KdfParametersJson, '$.salt')
                    WHERE (SaltBase64 IS NULL OR SaltBase64 = '')
                      AND KdfParametersJson IS NOT NULL
                      AND JSON_VALUE(KdfParametersJson, '$.salt') IS NOT NULL;
                ");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No-op: don't remove backfilled data when rolling back migrations
        }
    }
}
