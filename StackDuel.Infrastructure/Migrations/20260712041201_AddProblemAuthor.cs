using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddProblemAuthor : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "created_by_id", table: "problems", type: "uuid", nullable: true);

        migrationBuilder.CreateIndex(name: "IX_problems_created_by_id", table: "problems", column: "created_by_id");

        migrationBuilder.AddForeignKey(
            name: "FK_problems_users_created_by_id",
            table: "problems",
            column: "created_by_id",
            principalTable: "users",
            principalColumn: "id",
            onDelete: ReferentialAction.SetNull
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_problems_users_created_by_id", table: "problems");

        migrationBuilder.DropIndex(name: "IX_problems_created_by_id", table: "problems");

        migrationBuilder.DropColumn(name: "created_by_id", table: "problems");
    }
}