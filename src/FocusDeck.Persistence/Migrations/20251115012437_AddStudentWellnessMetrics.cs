using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusDeck.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStudentWellnessMetrics : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<bool>(
                name: "IsPinned",
                table: "Notes",
                type: "INTEGER",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldDefaultValue: 0);

            migrationBuilder.CreateTable(
                name: "StudentWellnessMetrics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    HoursWorked = table.Column<double>(type: "REAL", nullable: false),
                    BreakFrequency = table.Column<double>(type: "REAL", nullable: false),
                    QualityScore = table.Column<double>(type: "REAL", nullable: false),
                    SleepHours = table.Column<double>(type: "REAL", nullable: false),
                    IsUnsustainable = table.Column<bool>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentWellnessMetrics", x => x.Id);
                });

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StudentWellnessMetrics");

            migrationBuilder.AlterColumn<int>(
                name: "IsPinned",
                table: "Notes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(bool),
                oldType: "INTEGER",
                oldDefaultValue: false);
        }
    }
}
