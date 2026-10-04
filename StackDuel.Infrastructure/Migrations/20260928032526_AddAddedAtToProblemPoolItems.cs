using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddAddedAtToProblemPoolItems : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "added_at",
            table: "problem_pool_problems",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "CURRENT_TIMESTAMP"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "added_at", table: "problem_pool_problems");
    }
}