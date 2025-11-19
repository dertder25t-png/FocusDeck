using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusDeck.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddContextVectors : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ContextVectors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SnapshotId = table.Column<Guid>(type: "TEXT", nullable: false),
                    VectorData = table.Column<byte[]>(type: "BLOB", nullable: false),
                    Dimensions = table.Column<int>(type: "INTEGER", nullable: false),
                    ModelName = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_ContextVectors_SnapshotId",
                table: "ContextVectors",
                column: "SnapshotId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ContextVectors");
        }
    }
}
