using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matbiz.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDatevSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DebitorAccount",
                table: "Customers",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DebitorAccount",
                table: "Companies",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ChartOfAccounts",
                table: "BrandingSettings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "NextDebitorNumber",
                table: "BrandingSettings",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DebitorAccount",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "DebitorAccount",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "ChartOfAccounts",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "NextDebitorNumber",
                table: "BrandingSettings");
        }
    }
}
