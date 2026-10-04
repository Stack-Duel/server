using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddDailyChallenges : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "daily_challenges",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                challenge_date = table.Column<DateOnly>(type: "date", nullable: false),
                problem_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_daily_challenges", x => x.id);
                table.ForeignKey(
                    name: "FK_daily_challenges_problems_problem_id",
                    column: x => x.problem_id,
                    principalTable: "problems",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_daily_challenges_challenge_date",
            table: "daily_challenges",
            column: "challenge_date",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_daily_challenges_problem_id",
            table: "daily_challenges",
            column: "problem_id"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "daily_challenges");
    }
}