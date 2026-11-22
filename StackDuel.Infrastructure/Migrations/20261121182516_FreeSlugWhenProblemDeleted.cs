using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class FreeSlugWhenProblemDeleted : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_problems_slug",
            table: "problems");

        migrationBuilder.CreateIndex(
            name: "IX_problems_slug",
            table: "problems",
            column: "slug",
            unique: true,
            filter: "deleted_at IS NULL");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_problems_slug",
            table: "problems");

        migrationBuilder.CreateIndex(
            name: "IX_problems_slug",
            table: "problems",
            column: "slug",
            unique: true);
    }
}