using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusDeck.Persistence.Migrations
{
    public partial class AddWorkspaceSnapshots : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BrowserSessions",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EndedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeviceId = table.Column<string>(type: "text", nullable: false),
                    TabsJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BrowserSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkspaceSnapshots",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    WindowLayoutJson = table.Column<string>(type: "text", nullable: false),
                    BrowserSessionId = table.Column<string>(type: "text", nullable: true),
                    ActiveNoteId = table.Column<string>(type: "text", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true)
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WorkspaceSnapshots");

            migrationBuilder.DropTable(
                name: "BrowserSessions");
        }
    }
}
