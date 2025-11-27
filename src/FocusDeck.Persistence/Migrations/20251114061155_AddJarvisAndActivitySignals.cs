using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusDeck.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddJarvisAndActivitySignals : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ActivitySignals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SignalType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    SignalValue = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    SourceApp = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    MetadataJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivitySignals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JarvisWorkflowRuns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    WorkflowId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    RequestedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "TEXT", nullable: true),
                    TenantId = table.Column<Guid>(type: "TEXT", nullable: false),
                    RequestedByUserId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    LogSummary = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    JobId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JarvisWorkflowRuns", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivitySignals_CapturedAtUtc",
                table: "ActivitySignals",
                column: "CapturedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_ActivitySignals_TenantId",
                table: "ActivitySignals",
                column: "TenantId");

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivitySignals");

            migrationBuilder.DropTable(
                name: "JarvisWorkflowRuns");
        }
    }
}
