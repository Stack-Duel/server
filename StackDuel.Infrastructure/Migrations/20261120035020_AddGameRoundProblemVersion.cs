using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddGameRoundProblemVersion : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "problem_version_id",
            table: "game_rounds",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_game_rounds_problem_version_id",
            table: "game_rounds",
            column: "problem_version_id");

        migrationBuilder.AddForeignKey(
            name: "FK_game_rounds_problem_versions_problem_version_id",
            table: "game_rounds",
            column: "problem_version_id",
            principalTable: "problem_versions",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.Sql(
            """
            update game_rounds r
            set problem_version_id = p.published_version_id
            from problems p
            where p.id = r.problem_id and p.published_version_id is not null;
            """
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_game_rounds_problem_versions_problem_version_id",
            table: "game_rounds");

        migrationBuilder.DropIndex(
            name: "IX_game_rounds_problem_version_id",
            table: "game_rounds");

        migrationBuilder.DropColumn(
            name: "problem_version_id",
            table: "game_rounds");
    }
}