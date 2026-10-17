using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class DropRedundantGameRoundResultParticipantFk : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_game_round_results_game_participants_participant_id",
            table: "game_round_results");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddForeignKey(
            name: "FK_game_round_results_game_participants_participant_id",
            table: "game_round_results",
            column: "participant_id",
            principalTable: "game_participants",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }
}