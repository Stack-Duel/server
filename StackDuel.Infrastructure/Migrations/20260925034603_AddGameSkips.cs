using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddGameSkips : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "skips_enabled",
            table: "games",
            type: "boolean",
            nullable: false,
            defaultValue: true
        );

        migrationBuilder.AddColumn<int>(
            name: "skips_remaining",
            table: "game_participants",
            type: "integer",
            nullable: false,
            defaultValue: 3
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "skips_enabled", table: "games");

        migrationBuilder.DropColumn(name: "skips_remaining", table: "game_participants");
    }
}