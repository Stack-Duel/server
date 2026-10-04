using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddPositionToProblemPoolItems : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "position",
            table: "problem_pool_problems",
            type: "integer",
            nullable: false,
            defaultValue: 0
        );

        // Backfill existing rows so current membership order (by insertion time) is preserved
        // instead of every row defaulting to position 0.
        migrationBuilder.Sql(
            """
            UPDATE problem_pool_problems AS ppp
            SET position = ranked.position
            FROM (
                SELECT id, ROW_NUMBER() OVER (PARTITION BY pool_id ORDER BY added_at) - 1 AS position
                FROM problem_pool_problems
            ) AS ranked
            WHERE ppp.id = ranked.id;
            """
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "position", table: "problem_pool_problems");
    }
}