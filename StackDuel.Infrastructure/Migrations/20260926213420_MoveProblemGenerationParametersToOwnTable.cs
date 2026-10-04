using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class MoveProblemGenerationParametersToOwnTable : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "generation_output_value_type", table: "problems");

        migrationBuilder.DropColumn(name: "generation_parameters", table: "problems");

        migrationBuilder.DropColumn(name: "generation_seed", table: "problems");

        migrationBuilder.DropColumn(name: "generation_target_case_count", table: "problems");

        migrationBuilder.CreateTable(
            name: "problem_generation_parameters",
            columns: table => new
            {
                ProblemId = table.Column<Guid>(type: "uuid", nullable: false),
                seed = table.Column<int>(type: "integer", nullable: false),
                parameters = table.Column<string>(type: "jsonb", nullable: false),
                output_value_type = table.Column<string>(
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: false
                ),
                target_case_count = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_problem_generation_parameters", x => x.ProblemId);
                table.ForeignKey(
                    name: "FK_problem_generation_parameters_problems_ProblemId",
                    column: x => x.ProblemId,
                    principalTable: "problems",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "problem_generation_parameters");

        migrationBuilder.AddColumn<string>(
            name: "generation_output_value_type",
            table: "problems",
            type: "character varying(100)",
            maxLength: 100,
            nullable: true
        );

        migrationBuilder.AddColumn<string>(
            name: "generation_parameters",
            table: "problems",
            type: "jsonb",
            nullable: false,
            defaultValue: "[]"
        );

        migrationBuilder.AddColumn<int>(name: "generation_seed", table: "problems", type: "integer", nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "generation_target_case_count",
            table: "problems",
            type: "integer",
            nullable: true
        );
    }
}