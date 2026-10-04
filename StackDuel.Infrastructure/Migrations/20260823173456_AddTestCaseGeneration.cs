using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddTestCaseGeneration : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "generation_case_index",
            table: "test_cases",
            type: "integer",
            nullable: true
        );

        migrationBuilder.AddColumn<Guid>(name: "generation_spec_id", table: "test_cases", type: "uuid", nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "source",
            table: "test_cases",
            type: "integer",
            nullable: false,
            defaultValue: 0
        );

        migrationBuilder.AddColumn<Guid>(
            name: "generation_spec_id",
            table: "problem_setups",
            type: "uuid",
            nullable: true
        );

        migrationBuilder.AddColumn<string>(
            name: "reference_solution_code",
            table: "problem_setups",
            type: "text",
            nullable: true
        );

        migrationBuilder.CreateTable(
            name: "test_case_generation_specs",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                problem_setup_id = table.Column<Guid>(type: "uuid", nullable: false),
                parameters = table.Column<string>(type: "jsonb", nullable: false),
                output_value_type = table.Column<string>(
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: false
                ),
                target_case_count = table.Column<int>(type: "integer", nullable: false),
                seed = table.Column<int>(type: "integer", nullable: false),
                version = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_test_case_generation_specs", x => x.id);
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_test_cases_generation_spec_id",
            table: "test_cases",
            column: "generation_spec_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_problem_setups_generation_spec_id",
            table: "problem_setups",
            column: "generation_spec_id"
        );

        migrationBuilder.AddForeignKey(
            name: "FK_problem_setups_test_case_generation_specs_generation_spec_id",
            table: "problem_setups",
            column: "generation_spec_id",
            principalTable: "test_case_generation_specs",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict
        );

        migrationBuilder.AddForeignKey(
            name: "FK_test_cases_test_case_generation_specs_generation_spec_id",
            table: "test_cases",
            column: "generation_spec_id",
            principalTable: "test_case_generation_specs",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_problem_setups_test_case_generation_specs_generation_spec_id",
            table: "problem_setups"
        );

        migrationBuilder.DropForeignKey(
            name: "FK_test_cases_test_case_generation_specs_generation_spec_id",
            table: "test_cases"
        );

        migrationBuilder.DropTable(name: "test_case_generation_specs");

        migrationBuilder.DropIndex(name: "IX_test_cases_generation_spec_id", table: "test_cases");

        migrationBuilder.DropIndex(name: "IX_problem_setups_generation_spec_id", table: "problem_setups");

        migrationBuilder.DropColumn(name: "generation_case_index", table: "test_cases");

        migrationBuilder.DropColumn(name: "generation_spec_id", table: "test_cases");

        migrationBuilder.DropColumn(name: "source", table: "test_cases");

        migrationBuilder.DropColumn(name: "generation_spec_id", table: "problem_setups");

        migrationBuilder.DropColumn(name: "reference_solution_code", table: "problem_setups");
    }
}