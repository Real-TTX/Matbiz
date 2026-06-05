using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matbiz.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomMenu : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CustomMenuItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    IconClass = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    OpenInNewTab = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomMenuItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomMenuItemDepartments",
                columns: table => new
                {
                    CustomMenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomMenuItemDepartments", x => new { x.CustomMenuItemId, x.DepartmentId });
                    table.ForeignKey(
                        name: "FK_CustomMenuItemDepartments_CustomMenuItems_CustomMenuItemId",
                        column: x => x.CustomMenuItemId,
                        principalTable: "CustomMenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomMenuItemDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CustomMenuItemTeams",
                columns: table => new
                {
                    CustomMenuItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomMenuItemTeams", x => new { x.CustomMenuItemId, x.TeamId });
                    table.ForeignKey(
                        name: "FK_CustomMenuItemTeams_CustomMenuItems_CustomMenuItemId",
                        column: x => x.CustomMenuItemId,
                        principalTable: "CustomMenuItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomMenuItemTeams_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomMenuItemDepartments_DepartmentId",
                table: "CustomMenuItemDepartments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomMenuItemTeams_TeamId",
                table: "CustomMenuItemTeams",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomMenuItemDepartments");

            migrationBuilder.DropTable(
                name: "CustomMenuItemTeams");

            migrationBuilder.DropTable(
                name: "CustomMenuItems");
        }
    }
}
