using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddGameTrackLanguages : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "game_track_languages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                language_id = table.Column<Guid>(type: "uuid", nullable: false),
                game_track_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_game_track_languages", x => x.id);
                table.ForeignKey(
                    name: "FK_game_track_languages_game_tracks_game_track_id",
                    column: x => x.game_track_id,
                    principalTable: "game_tracks",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_game_track_languages_languages_language_id",
                    column: x => x.language_id,
                    principalTable: "languages",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_game_track_languages_game_track_id_language_id",
            table: "game_track_languages",
            columns: new[] { "game_track_id", "language_id" },
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_game_track_languages_language_id",
            table: "game_track_languages",
            column: "language_id"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "game_track_languages");
    }
}