using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FocusDeck.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCapturedItemSummaries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Summary",
                table: "CapturedItems",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TagsJson",
                table: "CapturedItems",
                type: "text",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Summary",
                table: "CapturedItems");

            migrationBuilder.DropColumn(
                name: "TagsJson",
                table: "CapturedItems");
        }
    }
}
