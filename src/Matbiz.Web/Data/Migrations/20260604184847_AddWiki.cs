using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matbiz.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWiki : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WikiPages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Slug = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ContentMarkdown = table.Column<string>(type: "text", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedByUserId = table.Column<string>(type: "text", nullable: true),
                    UpdatedByUserId = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiPages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WikiPageDepartments",
                columns: table => new
                {
                    WikiPageId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiPageDepartments", x => new { x.WikiPageId, x.DepartmentId });
                    table.ForeignKey(
                        name: "FK_WikiPageDepartments_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WikiPageDepartments_WikiPages_WikiPageId",
                        column: x => x.WikiPageId,
                        principalTable: "WikiPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WikiPageEditors",
                columns: table => new
                {
                    WikiPageId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiPageEditors", x => new { x.WikiPageId, x.UserId });
                    table.ForeignKey(
                        name: "FK_WikiPageEditors_WikiPages_WikiPageId",
                        column: x => x.WikiPageId,
                        principalTable: "WikiPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WikiPageTeams",
                columns: table => new
                {
                    WikiPageId = table.Column<Guid>(type: "uuid", nullable: false),
                    TeamId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WikiPageTeams", x => new { x.WikiPageId, x.TeamId });
                    table.ForeignKey(
                        name: "FK_WikiPageTeams_Teams_TeamId",
                        column: x => x.TeamId,
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_WikiPageTeams_WikiPages_WikiPageId",
                        column: x => x.WikiPageId,
                        principalTable: "WikiPages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WikiPageDepartments_DepartmentId",
                table: "WikiPageDepartments",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WikiPageEditors_UserId",
                table: "WikiPageEditors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WikiPages_Slug",
                table: "WikiPages",
                column: "Slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WikiPageTeams_TeamId",
                table: "WikiPageTeams",
                column: "TeamId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WikiPageDepartments");

            migrationBuilder.DropTable(
                name: "WikiPageEditors");

            migrationBuilder.DropTable(
                name: "WikiPageTeams");

            migrationBuilder.DropTable(
                name: "WikiPages");
        }
    }
}
