using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matbiz.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddArticlesAndCompanyData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "BrandingSettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Bic",
                table: "BrandingSettings",
                type: "character varying(11)",
                maxLength: 11,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyCity",
                table: "BrandingSettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyCountry",
                table: "BrandingSettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyEmail",
                table: "BrandingSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyLegalName",
                table: "BrandingSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyPhone",
                table: "BrandingSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyPostalCode",
                table: "BrandingSettings",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyStreet",
                table: "BrandingSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanyWebsite",
                table: "BrandingSettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultPaymentTerms",
                table: "BrandingSettings",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Iban",
                table: "BrandingSettings",
                type: "character varying(34)",
                maxLength: 34,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ManagingDirector",
                table: "BrandingSettings",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PdfFooterText",
                table: "BrandingSettings",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TaxNumber",
                table: "BrandingSettings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatId",
                table: "BrandingSettings",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "NumberRanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Prefix = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IncludeYear = table.Column<bool>(type: "boolean", nullable: false),
                    Separator = table.Column<string>(type: "character varying(5)", maxLength: 5, nullable: false),
                    Digits = table.Column<int>(type: "integer", nullable: false),
                    CurrentValue = table.Column<int>(type: "integer", nullable: false),
                    CurrentYear = table.Column<int>(type: "integer", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NumberRanges", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Percent = table.Column<decimal>(type: "numeric(6,3)", precision: 6, scale: 3, nullable: false),
                    IsDefault = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Articles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Number = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    NetPrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PurchasePrice = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    TaxRateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Articles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Articles_TaxRates_TaxRateId",
                        column: x => x.TaxRateId,
                        principalTable: "TaxRates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Articles_Number",
                table: "Articles",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Articles_TaxRateId",
                table: "Articles",
                column: "TaxRateId");

            migrationBuilder.CreateIndex(
                name: "IX_NumberRanges_Key",
                table: "NumberRanges",
                column: "Key",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Articles");

            migrationBuilder.DropTable(
                name: "NumberRanges");

            migrationBuilder.DropTable(
                name: "TaxRates");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "Bic",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "CompanyCity",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "CompanyCountry",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "CompanyEmail",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "CompanyLegalName",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "CompanyPhone",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "CompanyPostalCode",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "CompanyStreet",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "CompanyWebsite",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "DefaultPaymentTerms",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "Iban",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "ManagingDirector",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "PdfFooterText",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "TaxNumber",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "VatId",
                table: "BrandingSettings");
        }
    }
}
