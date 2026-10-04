using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class FixDbIssues : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            @"
                INSERT INTO execution_pipelines (id, name, description)
                SELECT '2d8d6f9f-0cb6-4c85-924e-3038f7f89f58'::uuid,
                       'Default Judge0 Pipeline',
                       'Backfilled by migration to satisfy existing problem setup pipeline references.'
                WHERE NOT EXISTS (SELECT 1 FROM execution_pipelines);
            "
        );

        migrationBuilder.Sql(
            @"
                UPDATE problem_setups AS ps
                SET pipeline_id = fallback.id
                FROM (
                    SELECT id
                    FROM execution_pipelines
                    ORDER BY name
                    LIMIT 1
                ) AS fallback
                WHERE ps.pipeline_id = '00000000-0000-0000-0000-000000000000'::uuid
                   OR NOT EXISTS (
                       SELECT 1
                       FROM execution_pipelines ep
                       WHERE ep.id = ps.pipeline_id
                   );
            "
        );

        migrationBuilder.CreateIndex(
            name: "IX_submissions_problem_setup_id",
            table: "submissions",
            column: "problem_setup_id"
        );

        migrationBuilder.CreateIndex(name: "IX_submissions_user_id", table: "submissions", column: "user_id");

        migrationBuilder.CreateIndex(
            name: "IX_submission_results_test_case_id",
            table: "submission_results",
            column: "test_case_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_submission_jobs_current_step_id",
            table: "submission_jobs",
            column: "current_step_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_submission_jobs_pipeline_id",
            table: "submission_jobs",
            column: "pipeline_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_submission_job_attempts_pipeline_step_id",
            table: "submission_job_attempts",
            column: "pipeline_step_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_problem_setups_language_version_id",
            table: "problem_setups",
            column: "language_version_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_problem_setups_pipeline_id",
            table: "problem_setups",
            column: "pipeline_id"
        );

        migrationBuilder.AddForeignKey(
            name: "FK_assert_step_configurations_execution_pipeline_steps_pipelin~",
            table: "assert_step_configurations",
            column: "pipeline_step_id",
            principalTable: "execution_pipeline_steps",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade
        );

        migrationBuilder.AddForeignKey(
            name: "FK_judge0_step_configurations_execution_pipeline_steps_pipelin~",
            table: "judge0_step_configurations",
            column: "pipeline_step_id",
            principalTable: "execution_pipeline_steps",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade
        );

        migrationBuilder.AddForeignKey(
            name: "FK_problem_setups_execution_pipelines_pipeline_id",
            table: "problem_setups",
            column: "pipeline_id",
            principalTable: "execution_pipelines",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict
        );

        migrationBuilder.AddForeignKey(
            name: "FK_problem_setups_language_versions_language_version_id",
            table: "problem_setups",
            column: "language_version_id",
            principalTable: "language_versions",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict
        );

        migrationBuilder.AddForeignKey(
            name: "FK_submission_job_attempts_execution_pipeline_steps_pipeline_s~",
            table: "submission_job_attempts",
            column: "pipeline_step_id",
            principalTable: "execution_pipeline_steps",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict
        );

        migrationBuilder.AddForeignKey(
            name: "FK_submission_jobs_execution_pipeline_steps_current_step_id",
            table: "submission_jobs",
            column: "current_step_id",
            principalTable: "execution_pipeline_steps",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull
        );

        migrationBuilder.AddForeignKey(
            name: "FK_submission_jobs_execution_pipelines_pipeline_id",
            table: "submission_jobs",
            column: "pipeline_id",
            principalTable: "execution_pipelines",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict
        );

        migrationBuilder.AddForeignKey(
            name: "FK_submission_jobs_submissions_submission_id",
            table: "submission_jobs",
            column: "submission_id",
            principalTable: "submissions",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade
        );

        migrationBuilder.AddForeignKey(
            name: "FK_submission_results_test_cases_test_case_id",
            table: "submission_results",
            column: "test_case_id",
            principalTable: "test_cases",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict
        );

        migrationBuilder.AddForeignKey(
            name: "FK_submissions_problem_setups_problem_setup_id",
            table: "submissions",
            column: "problem_setup_id",
            principalTable: "problem_setups",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict
        );

        migrationBuilder.AddForeignKey(
            name: "FK_submissions_users_user_id",
            table: "submissions",
            column: "user_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_assert_step_configurations_execution_pipeline_steps_pipelin~",
            table: "assert_step_configurations"
        );

        migrationBuilder.DropForeignKey(
            name: "FK_judge0_step_configurations_execution_pipeline_steps_pipelin~",
            table: "judge0_step_configurations"
        );

        migrationBuilder.DropForeignKey(
            name: "FK_problem_setups_execution_pipelines_pipeline_id",
            table: "problem_setups"
        );

        migrationBuilder.DropForeignKey(
            name: "FK_problem_setups_language_versions_language_version_id",
            table: "problem_setups"
        );

        migrationBuilder.DropForeignKey(
            name: "FK_submission_job_attempts_execution_pipeline_steps_pipeline_s~",
            table: "submission_job_attempts"
        );

        migrationBuilder.DropForeignKey(
            name: "FK_submission_jobs_execution_pipeline_steps_current_step_id",
            table: "submission_jobs"
        );

        migrationBuilder.DropForeignKey(
            name: "FK_submission_jobs_execution_pipelines_pipeline_id",
            table: "submission_jobs"
        );

        migrationBuilder.DropForeignKey(name: "FK_submission_jobs_submissions_submission_id", table: "submission_jobs");

        migrationBuilder.DropForeignKey(
            name: "FK_submission_results_test_cases_test_case_id",
            table: "submission_results"
        );

        migrationBuilder.DropForeignKey(name: "FK_submissions_problem_setups_problem_setup_id", table: "submissions");

        migrationBuilder.DropForeignKey(name: "FK_submissions_users_user_id", table: "submissions");

        migrationBuilder.DropIndex(name: "IX_submissions_problem_setup_id", table: "submissions");

        migrationBuilder.DropIndex(name: "IX_submissions_user_id", table: "submissions");

        migrationBuilder.DropIndex(name: "IX_submission_results_test_case_id", table: "submission_results");

        migrationBuilder.DropIndex(name: "IX_submission_jobs_current_step_id", table: "submission_jobs");

        migrationBuilder.DropIndex(name: "IX_submission_jobs_pipeline_id", table: "submission_jobs");

        migrationBuilder.DropIndex(
            name: "IX_submission_job_attempts_pipeline_step_id",
            table: "submission_job_attempts"
        );

        migrationBuilder.DropIndex(name: "IX_problem_setups_language_version_id", table: "problem_setups");

        migrationBuilder.DropIndex(name: "IX_problem_setups_pipeline_id", table: "problem_setups");
    }
}