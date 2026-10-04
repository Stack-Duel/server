using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddLeaderboards : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "leaderboards",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                game_mode_id = table.Column<Guid>(type: "uuid", nullable: false),
                time_limit_in_seconds = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_leaderboards", x => x.id);
                table.ForeignKey(
                    name: "FK_leaderboards_game_modes_game_mode_id",
                    column: x => x.game_mode_id,
                    principalTable: "game_modes",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "leaderboard_participants",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                high_score = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                leaderboard_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_leaderboard_participants", x => x.id);
                table.ForeignKey(
                    name: "FK_leaderboard_participants_leaderboards_leaderboard_id",
                    column: x => x.leaderboard_id,
                    principalTable: "leaderboards",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_leaderboard_participants_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_leaderboard_participants_leaderboard_id_user_id",
            table: "leaderboard_participants",
            columns: new[] { "leaderboard_id", "user_id" },
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_leaderboard_participants_user_id",
            table: "leaderboard_participants",
            column: "user_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_leaderboards_game_mode_id_time_limit_in_seconds",
            table: "leaderboards",
            columns: new[] { "game_mode_id", "time_limit_in_seconds" },
            unique: true
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "leaderboard_participants");

        migrationBuilder.DropTable(name: "leaderboards");
    }
}