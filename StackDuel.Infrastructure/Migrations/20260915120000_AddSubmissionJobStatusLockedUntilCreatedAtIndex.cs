using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddSubmissionJobStatusLockedUntilCreatedAtIndex : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex(
            name: "IX_submission_jobs_Status_LockedUntil_CreatedAt",
            table: "submission_jobs",
            columns: new[] { "status", "locked_until", "created_at" }
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(name: "IX_submission_jobs_Status_LockedUntil_CreatedAt", table: "submission_jobs");
    }
}