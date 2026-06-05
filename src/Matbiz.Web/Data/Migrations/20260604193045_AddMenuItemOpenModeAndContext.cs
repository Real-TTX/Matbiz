using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matbiz.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMenuItemOpenModeAndContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OpenInNewTab",
                table: "CustomMenuItems");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "CustomMenuItems",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500);

            migrationBuilder.AddColumn<int>(
                name: "Context",
                table: "CustomMenuItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "OpenMode",
                table: "CustomMenuItems",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Context",
                table: "CustomMenuItems");

            migrationBuilder.DropColumn(
                name: "OpenMode",
                table: "CustomMenuItems");

            migrationBuilder.AlterColumn<string>(
                name: "Url",
                table: "CustomMenuItems",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<bool>(
                name: "OpenInNewTab",
                table: "CustomMenuItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }
    }
}
