using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusDeck.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnsureUniquePakeUser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // UserId is already PK, so it has a unique index.
            // However, to satisfy the requirement for an explicit unique index via migration:
            migrationBuilder.CreateIndex(
                name: "IX_PakeCredentials_UserId",
                table: "PakeCredentials",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PakeCredentials_UserId",
                table: "PakeCredentials");
        }
    }
}
