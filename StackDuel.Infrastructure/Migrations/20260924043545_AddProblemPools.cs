using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddProblemPools : Migration
{
    private const string AllProblemsPoolId = "44444444-4444-4444-4444-444444444444";

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "problem_pools",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_problem_pools", x => x.id);
            }
        );

        migrationBuilder.CreateIndex(name: "IX_problem_pools_key", table: "problem_pools", column: "key", unique: true);

        migrationBuilder.CreateTable(
            name: "problem_pool_problems",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                problem_id = table.Column<Guid>(type: "uuid", nullable: false),
                pool_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_problem_pool_problems", x => x.id);
                table.ForeignKey(
                    name: "FK_problem_pool_problems_problem_pools_pool_id",
                    column: x => x.pool_id,
                    principalTable: "problem_pools",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_problem_pool_problems_problems_problem_id",
                    column: x => x.problem_id,
                    principalTable: "problems",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_problem_pool_problems_pool_id_problem_id",
            table: "problem_pool_problems",
            columns: new[] { "pool_id", "problem_id" },
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_problem_pool_problems_problem_id",
            table: "problem_pool_problems",
            column: "problem_id"
        );

        // Seeds the catch-all pool every pre-existing game mode and game gets backfilled to
        // below, and populates it with every problem that already exists — mirrors the
        // AddTracks migration backfilling pre-existing games into "general-purpose".
        migrationBuilder.Sql(
            $"""
            INSERT INTO problem_pools (id, key, name, description, created_at)
            VALUES (
                '{AllProblemsPoolId}',
                'all-problems',
                'All Problems',
                'Every published problem. Used as the default pool for built-in game modes.',
                now());
            """
        );

        migrationBuilder.Sql(
            $"""
            INSERT INTO problem_pool_problems (id, pool_id, problem_id)
            SELECT gen_random_uuid(), '{AllProblemsPoolId}', id
            FROM problems;
            """
        );

        migrationBuilder.AddColumn<Guid>(name: "pool_id", table: "games", type: "uuid", nullable: true);

        migrationBuilder.Sql($"UPDATE games SET pool_id = '{AllProblemsPoolId}' WHERE pool_id IS NULL");

        migrationBuilder.AlterColumn<Guid>(
            name: "pool_id",
            table: "games",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true
        );

        migrationBuilder.AddColumn<Guid>(name: "default_pool_id", table: "game_modes", type: "uuid", nullable: true);

        migrationBuilder.Sql(
            $"UPDATE game_modes SET default_pool_id = '{AllProblemsPoolId}' WHERE default_pool_id IS NULL"
        );

        migrationBuilder.AlterColumn<Guid>(
            name: "default_pool_id",
            table: "game_modes",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true
        );

        migrationBuilder.CreateIndex(name: "IX_games_pool_id", table: "games", column: "pool_id");

        migrationBuilder.CreateIndex(
            name: "IX_game_modes_default_pool_id",
            table: "game_modes",
            column: "default_pool_id"
        );

        migrationBuilder.AddForeignKey(
            name: "FK_game_modes_problem_pools_default_pool_id",
            table: "game_modes",
            column: "default_pool_id",
            principalTable: "problem_pools",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict
        );

        migrationBuilder.AddForeignKey(
            name: "FK_games_problem_pools_pool_id",
            table: "games",
            column: "pool_id",
            principalTable: "problem_pools",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_game_modes_problem_pools_default_pool_id", table: "game_modes");

        migrationBuilder.DropForeignKey(name: "FK_games_problem_pools_pool_id", table: "games");

        migrationBuilder.DropTable(name: "problem_pool_problems");

        migrationBuilder.DropTable(name: "problem_pools");

        migrationBuilder.DropIndex(name: "IX_games_pool_id", table: "games");

        migrationBuilder.DropIndex(name: "IX_game_modes_default_pool_id", table: "game_modes");

        migrationBuilder.DropColumn(name: "pool_id", table: "games");

        migrationBuilder.DropColumn(name: "default_pool_id", table: "game_modes");
    }
}