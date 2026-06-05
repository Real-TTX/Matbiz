using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matbiz.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomFieldSections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SectionId",
                table: "CustomerFieldDefinitions",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CustomFieldSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldSections", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFieldDefinitions_SectionId",
                table: "CustomerFieldDefinitions",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldSections_Name",
                table: "CustomFieldSections",
                column: "Name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_CustomerFieldDefinitions_CustomFieldSections_SectionId",
                table: "CustomerFieldDefinitions",
                column: "SectionId",
                principalTable: "CustomFieldSections",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CustomerFieldDefinitions_CustomFieldSections_SectionId",
                table: "CustomerFieldDefinitions");

            migrationBuilder.DropTable(
                name: "CustomFieldSections");

            migrationBuilder.DropIndex(
                name: "IX_CustomerFieldDefinitions_SectionId",
                table: "CustomerFieldDefinitions");

            migrationBuilder.DropColumn(
                name: "SectionId",
                table: "CustomerFieldDefinitions");
        }
    }
}
