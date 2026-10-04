using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddUserLanguagePreferences : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "user_language_preferences",
            columns: table => new
            {
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                language_id = table.Column<Guid>(type: "uuid", nullable: false),
                position = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_language_preferences", x => new { x.user_id, x.language_id });
                table.ForeignKey(
                    name: "FK_user_language_preferences_languages_language_id",
                    column: x => x.language_id,
                    principalTable: "languages",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_user_language_preferences_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_user_language_preferences_language_id",
            table: "user_language_preferences",
            column: "language_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_user_language_preferences_user_id_position",
            table: "user_language_preferences",
            columns: new[] { "user_id", "position" },
            unique: true
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "user_language_preferences");
    }
}