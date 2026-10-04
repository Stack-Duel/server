using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddPlayerRatings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "player_solving_ratings",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                rating = table.Column<int>(type: "integer", nullable: false),
                problems_solved = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_player_solving_ratings", x => x.id);
                table.ForeignKey(
                    name: "FK_player_solving_ratings_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "seasons",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                period_start = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                period_end = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_seasons", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "player_solved_problems",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                problem_id = table.Column<Guid>(type: "uuid", nullable: false),
                solved_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                player_solving_rating_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_player_solved_problems", x => x.id);
                table.ForeignKey(
                    name: "FK_player_solved_problems_player_solving_ratings_player_solvin~",
                    column: x => x.player_solving_rating_id,
                    principalTable: "player_solving_ratings",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_player_solved_problems_problems_problem_id",
                    column: x => x.problem_id,
                    principalTable: "problems",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "player_mode_ratings",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                game_mode_id = table.Column<Guid>(type: "uuid", nullable: false),
                season_id = table.Column<Guid>(type: "uuid", nullable: false),
                rating = table.Column<int>(type: "integer", nullable: false),
                games_played = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
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
            name: "IX_player_solved_problems_player_solving_rating_id_problem_id",
            table: "player_solved_problems",
            columns: new[] { "player_solving_rating_id", "problem_id" },
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_player_solved_problems_problem_id",
            table: "player_solved_problems",
            column: "problem_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_player_solving_ratings_user_id",
            table: "player_solving_ratings",
            column: "user_id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_seasons_period_start",
            table: "seasons",
            column: "period_start",
            unique: true
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "player_mode_ratings");

        migrationBuilder.DropTable(name: "player_solved_problems");

        migrationBuilder.DropTable(name: "seasons");

        migrationBuilder.DropTable(name: "player_solving_ratings");
    }
}