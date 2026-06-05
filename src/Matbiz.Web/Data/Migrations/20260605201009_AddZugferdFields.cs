using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matbiz.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddZugferdFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BuyerOrderNumber",
                table: "Documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerReference",
                table: "Documents",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerVatIdSnapshot",
                table: "Documents",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ContractNumber",
                table: "Documents",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrencyCode",
                table: "Documents",
                type: "character varying(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTime>(
                name: "ServiceDate",
                table: "Documents",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatCategoryCode",
                table: "DocumentPositions",
                type: "character varying(4)",
                maxLength: 4,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "VatId",
                table: "Customers",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyerReference",
                table: "Companies",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatId",
                table: "Companies",
                type: "character varying(30)",
                maxLength: 30,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationCourt",
                table: "BrandingSettings",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "BrandingSettings",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyerOrderNumber",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "BuyerReference",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "BuyerVatIdSnapshot",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ContractNumber",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "CurrencyCode",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "ServiceDate",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "VatCategoryCode",
                table: "DocumentPositions");

            migrationBuilder.DropColumn(
                name: "VatId",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "BuyerReference",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "VatId",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "RegistrationCourt",
                table: "BrandingSettings");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "BrandingSettings");
        }
    }
}
