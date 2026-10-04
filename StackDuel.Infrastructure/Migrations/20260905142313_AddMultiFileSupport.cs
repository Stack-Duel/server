using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddMultiFileSupport : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "additional_files",
            table: "submissions",
            type: "text",
            nullable: false,
            defaultValue: "[]"
        );

        migrationBuilder.AddColumn<string>(
            name: "additional_files",
            table: "problem_setups",
            type: "text",
            nullable: false,
            defaultValue: "[]"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "additional_files", table: "submissions");

        migrationBuilder.DropColumn(name: "additional_files", table: "problem_setups");
    }
}