using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class _20261125113451_RestrictVersionSetupSnapshotDelete : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_problem_version_setups_problem_setups_problem_setup_id",
            table: "problem_version_setups");

        migrationBuilder.AddForeignKey(
            name: "FK_problem_version_setups_problem_setups_problem_setup_id",
            table: "problem_version_setups",
            column: "problem_setup_id",
            principalTable: "problem_setups",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_problem_version_setups_problem_setups_problem_setup_id",
            table: "problem_version_setups");

        migrationBuilder.AddForeignKey(
            name: "FK_problem_version_setups_problem_setups_problem_setup_id",
            table: "problem_version_setups",
            column: "problem_setup_id",
            principalTable: "problem_setups",
            principalColumn: "id",
            onDelete: ReferentialAction.Cascade);
    }
}