using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddGamesAndSoloRush : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "game_modes",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                is_built_in = table.Column<bool>(type: "boolean", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_game_modes", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "game_mode_time_options",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                duration_seconds = table.Column<int>(type: "integer", nullable: false),
                is_default = table.Column<bool>(type: "boolean", nullable: false),
                game_mode_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_game_mode_time_options", x => x.id);
                table.ForeignKey(
                    name: "FK_game_mode_time_options_game_modes_game_mode_id",
                    column: x => x.game_mode_id,
                    principalTable: "game_modes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "games",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                lobby_id = table.Column<Guid>(type: "uuid", nullable: true),
                game_mode_id = table.Column<Guid>(type: "uuid", nullable: false),
                time_limit_in_seconds = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                ended_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_games", x => x.id);
                table.ForeignKey(
                    name: "FK_games_game_modes_game_mode_id",
                    column: x => x.game_mode_id,
                    principalTable: "game_modes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "game_participants",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                seat_no = table.Column<int>(type: "integer", nullable: false),
                joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                game_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_game_participants", x => x.id);
                table.ForeignKey(
                    name: "FK_game_participants_games_game_id",
                    column: x => x.game_id,
                    principalTable: "games",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_game_participants_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_game_mode_time_options_game_mode_id_duration_seconds",
            table: "game_mode_time_options",
            columns: new[] { "game_mode_id", "duration_seconds" },
            unique: true
        );

        migrationBuilder.CreateIndex(name: "IX_game_modes_key", table: "game_modes", column: "key", unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_game_participants_game_id_seat_no",
            table: "game_participants",
            columns: new[] { "game_id", "seat_no" },
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_game_participants_game_id_user_id",
            table: "game_participants",
            columns: new[] { "game_id", "user_id" },
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_game_participants_user_id",
            table: "game_participants",
            column: "user_id"
        );

        migrationBuilder.CreateIndex(name: "IX_games_game_mode_id", table: "games", column: "game_mode_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "game_mode_time_options");

        migrationBuilder.DropTable(name: "game_participants");

        migrationBuilder.DropTable(name: "games");

        migrationBuilder.DropTable(name: "game_modes");
    }
}