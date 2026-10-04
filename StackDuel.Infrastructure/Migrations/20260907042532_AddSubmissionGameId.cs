using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddSubmissionGameId : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "game_id", table: "submissions", type: "uuid", nullable: true);

        migrationBuilder.CreateIndex(name: "IX_submissions_game_id", table: "submissions", column: "game_id");

        migrationBuilder.AddForeignKey(
            name: "FK_submissions_games_game_id",
            table: "submissions",
            column: "game_id",
            principalTable: "games",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_submissions_games_game_id", table: "submissions");

        migrationBuilder.DropIndex(name: "IX_submissions_game_id", table: "submissions");

        migrationBuilder.DropColumn(name: "game_id", table: "submissions");
    }
}