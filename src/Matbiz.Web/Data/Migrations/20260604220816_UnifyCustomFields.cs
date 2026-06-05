using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Matbiz.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class UnifyCustomFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // === Schritt 1: Article-Bild-Spalten anlegen (rein additiv) ===
            migrationBuilder.AddColumn<byte[]>(
                name: "ImageBytes",
                table: "Articles",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageContentType",
                table: "Articles",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ImageVersion",
                table: "Articles",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // === Schritt 2: EntityType auf CustomFieldSections (Default 0=Contact) ===
            migrationBuilder.DropIndex(
                name: "IX_CustomFieldSections_Name",
                table: "CustomFieldSections");

            migrationBuilder.AddColumn<int>(
                name: "EntityType",
                table: "CustomFieldSections",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // === Schritt 3: FK von alten CustomerFieldDefinitions auf CustomFieldSections droppen,
            //                damit die Sections nicht von der DropTable-Kaskade mitgerissen werden ===
            migrationBuilder.Sql(@"
                ALTER TABLE ""CustomerFieldDefinitions""
                    DROP CONSTRAINT IF EXISTS ""FK_CustomerFieldDefinitions_CustomFieldSections_SectionId"";
            ");

            migrationBuilder.CreateTable(
                name: "CustomFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<int>(type: "integer", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    Options = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomFieldDefinitions_CustomFieldSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "CustomFieldSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CustomFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<int>(type: "integer", nullable: false),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomFieldValues_CustomFieldDefinitions_FieldDefinitionId",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CustomFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldSections_EntityType_Name",
                table: "CustomFieldSections",
                columns: new[] { "EntityType", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldDefinitions_EntityType_Key",
                table: "CustomFieldDefinitions",
                columns: new[] { "EntityType", "Key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldDefinitions_SectionId",
                table: "CustomFieldDefinitions",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldValues_EntityType_EntityId_FieldDefinitionId",
                table: "CustomFieldValues",
                columns: new[] { "EntityType", "EntityId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldValues_FieldDefinitionId",
                table: "CustomFieldValues",
                column: "FieldDefinitionId");

            // === Schritt 4: Daten übernehmen ===
            // Contact-Felder (EntityType = 0) aus den alten Customer-Tabellen kopieren.
            // Article-Tabellen waren leer (gerade erst erstellt) — kein Datentransfer nötig.
            migrationBuilder.Sql(@"
                INSERT INTO ""CustomFieldDefinitions""
                    (""Id"", ""EntityType"", ""Key"", ""Label"", ""Type"", ""Required"", ""SortOrder"", ""SectionId"", ""Options"", ""CreatedAt"")
                SELECT ""Id"", 0, ""Key"", ""Label"", ""Type"", ""Required"", ""SortOrder"", ""SectionId"", ""Options"", ""CreatedAt""
                FROM ""CustomerFieldDefinitions"";

                INSERT INTO ""CustomFieldValues""
                    (""Id"", ""EntityType"", ""EntityId"", ""FieldDefinitionId"", ""Value"")
                SELECT ""Id"", 0, ""CustomerId"", ""FieldDefinitionId"", ""Value""
                FROM ""CustomerFieldValues"";
            ");

            // === Schritt 5: Alte Tabellen wegräumen ===
            migrationBuilder.DropTable(name: "ArticleCustomFieldValues");
            migrationBuilder.DropTable(name: "CustomerFieldValues");
            migrationBuilder.DropTable(name: "ArticleFieldDefinitions");
            migrationBuilder.DropTable(name: "CustomerFieldDefinitions");
            migrationBuilder.DropTable(name: "ArticleFieldSections");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CustomFieldValues");

            migrationBuilder.DropTable(
                name: "CustomFieldDefinitions");

            migrationBuilder.DropIndex(
                name: "IX_CustomFieldSections_EntityType_Name",
                table: "CustomFieldSections");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "CustomFieldSections");

            migrationBuilder.DropColumn(
                name: "ImageBytes",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "ImageContentType",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "ImageVersion",
                table: "Articles");

            migrationBuilder.CreateTable(
                name: "ArticleFieldSections",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleFieldSections", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CustomerFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Options = table.Column<string>(type: "text", nullable: true),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerFieldDefinitions_CustomFieldSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "CustomFieldSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "ArticleFieldDefinitions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SectionId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Options = table.Column<string>(type: "text", nullable: true),
                    Required = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleFieldDefinitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleFieldDefinitions_ArticleFieldSections_SectionId",
                        column: x => x.SectionId,
                        principalTable: "ArticleFieldSections",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "CustomerFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CustomerFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CustomerFieldValues_CustomerFieldDefinitions_FieldDefinitio~",
                        column: x => x.FieldDefinitionId,
                        principalTable: "CustomerFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CustomerFieldValues_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArticleCustomFieldValues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ArticleId = table.Column<Guid>(type: "uuid", nullable: false),
                    FieldDefinitionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleCustomFieldValues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleCustomFieldValues_ArticleFieldDefinitions_FieldDefin~",
                        column: x => x.FieldDefinitionId,
                        principalTable: "ArticleFieldDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleCustomFieldValues_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CustomFieldSections_Name",
                table: "CustomFieldSections",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleCustomFieldValues_ArticleId_FieldDefinitionId",
                table: "ArticleCustomFieldValues",
                columns: new[] { "ArticleId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleCustomFieldValues_FieldDefinitionId",
                table: "ArticleCustomFieldValues",
                column: "FieldDefinitionId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleFieldDefinitions_Key",
                table: "ArticleFieldDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleFieldDefinitions_SectionId",
                table: "ArticleFieldDefinitions",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFieldDefinitions_Key",
                table: "CustomerFieldDefinitions",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFieldDefinitions_SectionId",
                table: "CustomerFieldDefinitions",
                column: "SectionId");

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFieldValues_CustomerId_FieldDefinitionId",
                table: "CustomerFieldValues",
                columns: new[] { "CustomerId", "FieldDefinitionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerFieldValues_FieldDefinitionId",
                table: "CustomerFieldValues",
                column: "FieldDefinitionId");
        }
    }
}
