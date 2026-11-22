using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddProblemValidationSteps : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "problem_validation_steps",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                kind = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                detail = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                problem_validation_job_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_problem_validation_steps", x => x.id);
                table.ForeignKey(
                    name: "FK_problem_validation_steps_problem_validation_jobs_problem_va~",
                    column: x => x.problem_validation_job_id,
                    principalTable: "problem_validation_jobs",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "problem_validation_step_items",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                detail = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                problem_validation_step_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_problem_validation_step_items", x => x.id);
                table.ForeignKey(
                    name: "FK_problem_validation_step_items_problem_validation_steps_prob~",
                    column: x => x.problem_validation_step_id,
                    principalTable: "problem_validation_steps",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_problem_validation_jobs_problem_id_created_at",
            table: "problem_validation_jobs",
            columns: new[] { "problem_id", "created_at" });

        migrationBuilder.CreateIndex(
            name: "IX_problem_validation_step_items_problem_validation_step_id",
            table: "problem_validation_step_items",
            column: "problem_validation_step_id");

        migrationBuilder.CreateIndex(
            name: "IX_problem_validation_steps_problem_validation_job_id",
            table: "problem_validation_steps",
            column: "problem_validation_job_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "problem_validation_step_items");

        migrationBuilder.DropTable(
            name: "problem_validation_steps");

        migrationBuilder.DropIndex(
            name: "IX_problem_validation_jobs_problem_id_created_at",
            table: "problem_validation_jobs");
    }
}