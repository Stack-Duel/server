using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class MigrateOldDatabase : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "problems",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                question = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                difficulty = table.Column<int>(type: "integer", nullable: false),
                time_limit = table.Column<int>(type: "integer", nullable: false),
                memory_limit = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_problems", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "test_suites",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                type = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_test_suites", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "problem_history",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                question = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                difficulty = table.Column<int>(type: "integer", nullable: false),
                time_limit = table.Column<int>(type: "integer", nullable: false),
                memory_limit = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                problem_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_problem_history", x => x.id);
                table.ForeignKey(
                    name: "FK_problem_history_problems_problem_id",
                    column: x => x.problem_id,
                    principalTable: "problems",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "problem_setups",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                language_version_id = table.Column<Guid>(type: "uuid", nullable: false),
                initial_code = table.Column<string>(type: "text", nullable: false),
                function_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                problem_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_problem_setups", x => x.id);
                table.ForeignKey(
                    name: "FK_problem_setups_problems_problem_id",
                    column: x => x.problem_id,
                    principalTable: "problems",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "test_cases",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                test_suite_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_test_cases", x => x.id);
                table.ForeignKey(
                    name: "FK_test_cases_test_suites_test_suite_id",
                    column: x => x.test_suite_id,
                    principalTable: "test_suites",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "problem_setup_test_suites",
            columns: table => new
            {
                problem_setup_id = table.Column<Guid>(type: "uuid", nullable: false),
                test_suite_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_problem_setup_test_suites", x => new { x.problem_setup_id, x.test_suite_id });
                table.ForeignKey(
                    name: "FK_problem_setup_test_suites_problem_setups_problem_setup_id",
                    column: x => x.problem_setup_id,
                    principalTable: "problem_setups",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_problem_setup_test_suites_test_suites_test_suite_id",
                    column: x => x.test_suite_id,
                    principalTable: "test_suites",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "test_case_expected_outputs",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                value = table.Column<string>(type: "text", nullable: false),
                value_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                test_case_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_test_case_expected_outputs", x => x.id);
                table.ForeignKey(
                    name: "FK_test_case_expected_outputs_test_cases_test_case_id",
                    column: x => x.test_case_id,
                    principalTable: "test_cases",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "test_case_inputs",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                value = table.Column<string>(type: "text", nullable: false),
                value_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                test_case_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_test_case_inputs", x => x.id);
                table.ForeignKey(
                    name: "FK_test_case_inputs_test_cases_test_case_id",
                    column: x => x.test_case_id,
                    principalTable: "test_cases",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_problem_history_problem_id",
            table: "problem_history",
            column: "problem_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_problem_setup_test_suites_test_suite_id",
            table: "problem_setup_test_suites",
            column: "test_suite_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_problem_setups_problem_id",
            table: "problem_setups",
            column: "problem_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_test_case_expected_outputs_test_case_id",
            table: "test_case_expected_outputs",
            column: "test_case_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_test_case_inputs_test_case_id",
            table: "test_case_inputs",
            column: "test_case_id"
        );

        migrationBuilder.CreateIndex(name: "IX_test_cases_test_suite_id", table: "test_cases", column: "test_suite_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "problem_history");

        migrationBuilder.DropTable(name: "problem_setup_test_suites");

        migrationBuilder.DropTable(name: "test_case_expected_outputs");

        migrationBuilder.DropTable(name: "test_case_inputs");

        migrationBuilder.DropTable(name: "problem_setups");

        migrationBuilder.DropTable(name: "test_cases");

        migrationBuilder.DropTable(name: "problems");

        migrationBuilder.DropTable(name: "test_suites");
    }
}