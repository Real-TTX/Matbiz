using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matbiz.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNavMenuLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NavMenuLayouts",
                columns: table => new
                {
                    EntryKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LabelOverride = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SectionOverride = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SortOrderOverride = table.Column<int>(type: "integer", nullable: true),
                    IsHidden = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NavMenuLayouts", x => x.EntryKey);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NavMenuLayouts");
        }
    }
}
