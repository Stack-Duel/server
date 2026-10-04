using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddSubmissionPipeline : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "pipeline_id",
            table: "problem_setups",
            type: "uuid",
            nullable: false,
            defaultValue: new Guid("00000000-0000-0000-0000-000000000000")
        );

        migrationBuilder.CreateTable(
            name: "assert_step_configurations",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                pipeline_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                strategy = table.Column<int>(type: "integer", nullable: false),
                tolerance = table.Column<decimal>(type: "numeric", nullable: true),
                case_sensitive = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_assert_step_configurations", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "execution_pipelines",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_execution_pipelines", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "judge0_step_configurations",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                pipeline_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                is_encoded = table.Column<bool>(type: "boolean", nullable: false),
                should_wait = table.Column<bool>(type: "boolean", nullable: false),
                strip_whitespace = table.Column<bool>(type: "boolean", nullable: false),
                default_timeout_seconds = table.Column<int>(type: "integer", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_judge0_step_configurations", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "submission_jobs",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                pipeline_id = table.Column<Guid>(type: "uuid", nullable: false),
                current_step_id = table.Column<Guid>(type: "uuid", nullable: true),
                status = table.Column<int>(type: "integer", nullable: false),
                failure_reason = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_submission_jobs", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "execution_pipeline_steps",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                step_type = table.Column<int>(type: "integer", nullable: false),
                step_order = table.Column<int>(type: "integer", nullable: false),
                max_attempts = table.Column<int>(type: "integer", nullable: false),
                timeout_seconds = table.Column<int>(type: "integer", nullable: false),
                is_polling = table.Column<bool>(type: "boolean", nullable: false),
                pipeline_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_execution_pipeline_steps", x => x.id);
                table.ForeignKey(
                    name: "FK_execution_pipeline_steps_execution_pipelines_pipeline_id",
                    column: x => x.pipeline_id,
                    principalTable: "execution_pipelines",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "submission_job_attempts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                pipeline_step_id = table.Column<Guid>(type: "uuid", nullable: false),
                attempt_number = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                request_payload = table.Column<string>(type: "text", nullable: true),
                response_payload = table.Column<string>(type: "text", nullable: true),
                error = table.Column<string>(type: "text", nullable: true),
                started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                duration_ms = table.Column<int>(type: "integer", nullable: true),
                job_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_submission_job_attempts", x => x.id);
                table.ForeignKey(
                    name: "FK_submission_job_attempts_submission_jobs_job_id",
                    column: x => x.job_id,
                    principalTable: "submission_jobs",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_assert_step_configurations_pipeline_step_id",
            table: "assert_step_configurations",
            column: "pipeline_step_id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_execution_pipeline_steps_pipeline_id",
            table: "execution_pipeline_steps",
            column: "pipeline_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_judge0_step_configurations_pipeline_step_id",
            table: "judge0_step_configurations",
            column: "pipeline_step_id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_submission_job_attempts_job_id",
            table: "submission_job_attempts",
            column: "job_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_submission_jobs_submission_id",
            table: "submission_jobs",
            column: "submission_id",
            unique: true
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "assert_step_configurations");

        migrationBuilder.DropTable(name: "execution_pipeline_steps");

        migrationBuilder.DropTable(name: "judge0_step_configurations");

        migrationBuilder.DropTable(name: "submission_job_attempts");

        migrationBuilder.DropTable(name: "execution_pipelines");

        migrationBuilder.DropTable(name: "submission_jobs");

        migrationBuilder.DropColumn(name: "pipeline_id", table: "problem_setups");
    }
}