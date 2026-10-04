using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddProblemReactions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "problem_reaction_types",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                key = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                emoji = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                sort_order = table.Column<int>(type: "integer", nullable: false),
                is_enabled = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_problem_reaction_types", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "problem_reactions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                problem_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                reaction_type_id = table.Column<Guid>(type: "uuid", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_problem_reactions", x => x.id);
                table.ForeignKey(
                    name: "FK_problem_reactions_problem_reaction_types_reaction_type_id",
                    column: x => x.reaction_type_id,
                    principalTable: "problem_reaction_types",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
                table.ForeignKey(
                    name: "FK_problem_reactions_problems_problem_id",
                    column: x => x.problem_id,
                    principalTable: "problems",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_problem_reactions_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_problem_reaction_types_key",
            table: "problem_reaction_types",
            column: "key",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "ix_problem_reactions_active_per_user",
            table: "problem_reactions",
            columns: new[] { "problem_id", "user_id" },
            unique: true,
            filter: "is_active = true"
        );

        migrationBuilder.CreateIndex(
            name: "IX_problem_reactions_problem_id_user_id_reaction_type_id",
            table: "problem_reactions",
            columns: new[] { "problem_id", "user_id", "reaction_type_id" },
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_problem_reactions_reaction_type_id",
            table: "problem_reactions",
            column: "reaction_type_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_problem_reactions_user_id",
            table: "problem_reactions",
            column: "user_id"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "problem_reactions");

        migrationBuilder.DropTable(name: "problem_reaction_types");
    }
}