using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddProblemVersions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "problem_version_id",
            table: "submissions",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<Guid>(
            name: "published_version_id",
            table: "problems",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateTable(
            name: "problem_versions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                problem_id = table.Column<Guid>(type: "uuid", nullable: false),
                version_number = table.Column<int>(type: "integer", nullable: false),
                state = table.Column<int>(type: "integer", nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                question = table.Column<string>(type: "character varying(10000)", maxLength: 10000, nullable: false),
                difficulty = table.Column<int>(type: "integer", nullable: false),
                time_limit = table.Column<int>(type: "integer", nullable: false),
                memory_limit = table.Column<int>(type: "integer", nullable: false),
                published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                superseded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_problem_versions", x => x.id);
                table.ForeignKey(
                    name: "FK_problem_versions_problems_problem_id",
                    column: x => x.problem_id,
                    principalTable: "problems",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "problem_version_setups",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                problem_setup_id = table.Column<Guid>(type: "uuid", nullable: false),
                problem_version_id = table.Column<Guid>(type: "uuid", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_problem_version_setups", x => x.id);
                table.ForeignKey(
                    name: "FK_problem_version_setups_problem_setups_problem_setup_id",
                    column: x => x.problem_setup_id,
                    principalTable: "problem_setups",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_problem_version_setups_problem_versions_problem_version_id",
                    column: x => x.problem_version_id,
                    principalTable: "problem_versions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_submissions_problem_version_id",
            table: "submissions",
            column: "problem_version_id");

        migrationBuilder.CreateIndex(
            name: "IX_problem_version_setups_problem_setup_id",
            table: "problem_version_setups",
            column: "problem_setup_id");

        migrationBuilder.CreateIndex(
            name: "IX_problem_version_setups_problem_version_id",
            table: "problem_version_setups",
            column: "problem_version_id");

        migrationBuilder.CreateIndex(
            name: "IX_problem_versions_problem_id_version_number",
            table: "problem_versions",
            columns: new[] { "problem_id", "version_number" },
            unique: true);

        migrationBuilder.AddForeignKey(
            name: "FK_submissions_problem_versions_problem_version_id",
            table: "submissions",
            column: "problem_version_id",
            principalTable: "problem_versions",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict);

        migrationBuilder.Sql(
            """
            insert into problem_versions (
                id, problem_id, version_number, state, title, question,
                difficulty, time_limit, memory_limit, published_at, superseded_at
            )
            select gen_random_uuid(), p.id, 1, 1, p.title, p.question,
                   p.difficulty, p.time_limit, p.memory_limit, p.created_at, null
            from problems p
            where p.status in (2, 3);
            """
        );

        migrationBuilder.Sql(
            """
            update problems p
            set published_version_id = v.id
            from problem_versions v
            where v.problem_id = p.id and v.version_number = 1;
            """
        );

        migrationBuilder.Sql(
            """
            insert into problem_version_setups (id, problem_setup_id, problem_version_id)
            select gen_random_uuid(), s.id, v.id
            from problem_setups s
            join problem_versions v on v.problem_id = s.problem_id;
            """
        );

        migrationBuilder.Sql(
            """
            update submissions sub
            set problem_version_id = p.published_version_id
            from problem_setups s
            join problems p on p.id = s.problem_id
            where sub.problem_setup_id = s.id and p.published_version_id is not null;
            """
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_submissions_problem_versions_problem_version_id",
            table: "submissions");

        migrationBuilder.DropTable(
            name: "problem_version_setups");

        migrationBuilder.DropTable(
            name: "problem_versions");

        migrationBuilder.DropIndex(
            name: "IX_submissions_problem_version_id",
            table: "submissions");

        migrationBuilder.DropColumn(
            name: "problem_version_id",
            table: "submissions");

        migrationBuilder.DropColumn(
            name: "published_version_id",
            table: "problems");
    }
}