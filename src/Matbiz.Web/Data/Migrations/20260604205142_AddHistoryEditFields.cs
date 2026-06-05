using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matbiz.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoryEditFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EditedAt",
                table: "CustomerHistoryEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditedByUserId",
                table: "CustomerHistoryEntries",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EditedAt",
                table: "CompanyHistoryEntries",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EditedByUserId",
                table: "CompanyHistoryEntries",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EditedAt",
                table: "CustomerHistoryEntries");

            migrationBuilder.DropColumn(
                name: "EditedByUserId",
                table: "CustomerHistoryEntries");

            migrationBuilder.DropColumn(
                name: "EditedAt",
                table: "CompanyHistoryEntries");

            migrationBuilder.DropColumn(
                name: "EditedByUserId",
                table: "CompanyHistoryEntries");
        }
    }
}
