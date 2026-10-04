using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class BringOldDatabaseTablesOver : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "submissions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                problem_setup_id = table.Column<Guid>(type: "uuid", nullable: false),
                source_code = table.Column<string>(type: "character varying(65536)", maxLength: 65536, nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                type = table.Column<int>(type: "integer", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_submissions", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "submission_results",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                actual_output = table.Column<string>(type: "text", nullable: true),
                compile_output = table.Column<string>(type: "text", nullable: true),
                memory_used = table.Column<int>(type: "integer", nullable: true),
                runtime = table.Column<int>(type: "integer", nullable: true),
                standard_error = table.Column<string>(type: "text", nullable: true),
                standard_output = table.Column<string>(type: "text", nullable: true),
                status = table.Column<int>(type: "integer", nullable: false),
                test_case_id = table.Column<Guid>(type: "uuid", nullable: false),
                evaluation_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                execution_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                submission_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_submission_results", x => x.id);
                table.ForeignKey(
                    name: "FK_submission_results_submissions_submission_id",
                    column: x => x.submission_id,
                    principalTable: "submissions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_submission_results_submission_id",
            table: "submission_results",
            column: "submission_id"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "submission_results");

        migrationBuilder.DropTable(name: "submissions");
    }
}