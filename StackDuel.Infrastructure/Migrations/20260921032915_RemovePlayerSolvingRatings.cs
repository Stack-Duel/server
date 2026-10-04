using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class RemovePlayerSolvingRatings : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "player_solved_problems");

        migrationBuilder.DropTable(name: "player_solving_ratings");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "player_solving_ratings",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                problems_solved = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                rating = table.Column<int>(type: "integer", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
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
    }
}