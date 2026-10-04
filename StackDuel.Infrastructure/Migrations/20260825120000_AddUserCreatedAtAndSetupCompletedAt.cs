using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddUserCreatedAtAndSetupCompletedAt : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<DateTime>(
            name: "created_at",
            table: "users",
            type: "timestamp with time zone",
            nullable: false,
            defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)
        );

        migrationBuilder.AddColumn<DateTime>(
            name: "setup_completed_at",
            table: "users",
            type: "timestamp with time zone",
            nullable: true
        );

        migrationBuilder.Sql(
            "UPDATE users SET setup_completed_at = username_last_changed_at WHERE username_last_changed_at IS NOT NULL;"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "created_at", table: "users");

        migrationBuilder.DropColumn(name: "setup_completed_at", table: "users");
    }
}