using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matbiz.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPdfTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PdfTemplate",
                table: "BrandingSettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PdfTemplate",
                table: "BrandingSettings");
        }
    }
}
