using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddGameJoinCode : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "join_code",
            table: "games",
            type: "character varying(7)",
            maxLength: 7,
            nullable: false,
            defaultValue: ""
        );

        // Backfill existing rows with a code derived from their id so the column can be made
        // unique below — pre-existing games predate join codes and were never meant to be
        // joined this way, so a deterministic (not cryptographically random) code is fine here.
        migrationBuilder.Sql("UPDATE games SET join_code = UPPER(SUBSTRING(REPLACE(id::text, '-', ''), 1, 7));");

        migrationBuilder.CreateIndex(name: "IX_games_join_code", table: "games", column: "join_code", unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_games_join_code", table: "games");

        migrationBuilder.DropColumn(name: "join_code", table: "games");
    }
}