using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusDeck.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPrivacySettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrivacySettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    ContextType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    Tier = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 1),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrivacySettings", x => x.Id);
                });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrivacySettings");
        }
    }
}
