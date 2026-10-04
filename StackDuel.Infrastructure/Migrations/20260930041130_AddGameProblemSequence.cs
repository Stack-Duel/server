using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddGameProblemSequence : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "game_problems",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                position = table.Column<int>(type: "integer", nullable: false),
                problem_id = table.Column<Guid>(type: "uuid", nullable: false),
                game_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_game_problems", x => x.id);
                table.ForeignKey(
                    name: "FK_game_problems_games_game_id",
                    column: x => x.game_id,
                    principalTable: "games",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_game_problems_problems_problem_id",
                    column: x => x.problem_id,
                    principalTable: "problems",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_game_problems_game_id_position",
            table: "game_problems",
            columns: new[] { "game_id", "position" },
            unique: true
        );

        migrationBuilder.CreateIndex(name: "IX_game_problems_problem_id", table: "game_problems", column: "problem_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "game_problems");
    }
}