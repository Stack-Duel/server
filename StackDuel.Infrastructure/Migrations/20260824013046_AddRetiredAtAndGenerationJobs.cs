using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddRetiredAtAndGenerationJobs : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "retired_at",
            table: "test_cases",
            type: "timestamp with time zone",
            nullable: true
        );

        migrationBuilder.CreateTable(
            name: "test_case_generation_jobs",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                problem_setup_id = table.Column<Guid>(type: "uuid", nullable: false),
                reference_solution_code = table.Column<string>(type: "text", nullable: false),
                parameters = table.Column<string>(type: "jsonb", nullable: false),
                output_value_type = table.Column<string>(
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: false
                ),
                target_case_count = table.Column<int>(type: "integer", nullable: false),
                seed = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                result_summary = table.Column<string>(type: "text", nullable: true),
                failure_reason = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                locked_until = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_test_case_generation_jobs", x => x.id);
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_test_case_generation_jobs_status",
            table: "test_case_generation_jobs",
            column: "status"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "test_case_generation_jobs");

        migrationBuilder.DropColumn(name: "retired_at", table: "test_cases");
    }
}