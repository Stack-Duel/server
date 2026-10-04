using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class MigrateOldDatabase2 : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "languages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                slug = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_languages", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "language_versions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                judge0_id = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                version = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                language_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_language_versions", x => x.id);
                table.ForeignKey(
                    name: "FK_language_versions_languages_language_id",
                    column: x => x.language_id,
                    principalTable: "languages",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_language_versions_language_id",
            table: "language_versions",
            column: "language_id"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "language_versions");

        migrationBuilder.DropTable(name: "languages");
    }
}