using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddProblemValidationWorkflow : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
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

        migrationBuilder.AddColumn<string>(
            name: "validation_failure_reason",
            table: "problems",
            type: "character varying(4000)",
            maxLength: 4000,
            nullable: true
        );

        migrationBuilder.CreateTable(
            name: "problem_validation_jobs",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                problem_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                test_case_generation_job_ids = table.Column<string>(type: "jsonb", nullable: false),
                result_summary = table.Column<string>(type: "text", nullable: true),
                failure_reason = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_problem_validation_jobs", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "required_problem_languages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                language_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_required_problem_languages", x => x.id);
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_problem_validation_jobs_status_locked_until",
            table: "problem_validation_jobs",
            columns: new[] { "status", "locked_until" }
        );

        migrationBuilder.CreateIndex(
            name: "IX_required_problem_languages_language_version_id",
            table: "required_problem_languages",
            column: "language_version_id",
            unique: true
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "problem_validation_jobs");

        migrationBuilder.DropTable(name: "required_problem_languages");

        migrationBuilder.DropColumn(name: "generation_output_value_type", table: "problems");

        migrationBuilder.DropColumn(name: "generation_parameters", table: "problems");

        migrationBuilder.DropColumn(name: "generation_seed", table: "problems");

        migrationBuilder.DropColumn(name: "generation_target_case_count", table: "problems");

        migrationBuilder.DropColumn(name: "validation_failure_reason", table: "problems");
    }
}