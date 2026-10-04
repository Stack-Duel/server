using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddGameModePlayerLimits : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "max_players",
            table: "game_modes",
            type: "integer",
            nullable: false,
            defaultValue: 0
        );

        migrationBuilder.AddColumn<int>(
            name: "min_players",
            table: "game_modes",
            type: "integer",
            nullable: false,
            defaultValue: 0
        );

        // Backfill Solo Rush as a single-player mode
        migrationBuilder.Sql("UPDATE game_modes SET min_players = 1, max_players = 1 WHERE key = 'solo_rush'");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "max_players", table: "game_modes");

        migrationBuilder.DropColumn(name: "min_players", table: "game_modes");
    }
}