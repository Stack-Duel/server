using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class RemovePlayerModeRatingsAndSeasons : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "player_mode_ratings");

        migrationBuilder.DropTable(name: "seasons");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "seasons",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_seasons", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "player_mode_ratings",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                game_mode_id = table.Column<Guid>(type: "uuid", nullable: false),
                games_played = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                rating = table.Column<int>(type: "integer", nullable: false),
                season_id = table.Column<Guid>(type: "uuid", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_player_mode_ratings", x => x.id);
                table.ForeignKey(
                    name: "FK_player_mode_ratings_game_modes_game_mode_id",
                    column: x => x.game_mode_id,
                    principalTable: "game_modes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
                table.ForeignKey(
                    name: "FK_player_mode_ratings_seasons_season_id",
                    column: x => x.season_id,
                    principalTable: "seasons",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
                table.ForeignKey(
                    name: "FK_player_mode_ratings_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_player_mode_ratings_game_mode_id_season_id_rating",
            table: "player_mode_ratings",
            columns: new[] { "game_mode_id", "season_id", "rating" }
        );

        migrationBuilder.CreateIndex(
            name: "IX_player_mode_ratings_season_id",
            table: "player_mode_ratings",
            column: "season_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_player_mode_ratings_user_id_game_mode_id_season_id",
            table: "player_mode_ratings",
            columns: new[] { "user_id", "game_mode_id", "season_id" },
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_seasons_period_start",
            table: "seasons",
            column: "period_start",
            unique: true
        );
    }
}