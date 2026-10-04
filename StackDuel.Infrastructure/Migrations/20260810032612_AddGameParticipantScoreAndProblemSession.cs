using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddGameParticipantScoreAndProblemSession : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "problem_session",
            table: "game_participants",
            type: "jsonb",
            nullable: true
        );

        migrationBuilder.AddColumn<int>(
            name: "score",
            table: "game_participants",
            type: "integer",
            nullable: false,
            defaultValue: 0
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "problem_session", table: "game_participants");

        migrationBuilder.DropColumn(name: "score", table: "game_participants");
    }
}