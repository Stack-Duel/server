using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddProblemTags : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "tags",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tags", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "problem_tags",
            columns: table => new
            {
                ProblemsId = table.Column<Guid>(type: "uuid", nullable: false),
                TagsId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_problem_tags", x => new { x.ProblemsId, x.TagsId });
                table.ForeignKey(
                    name: "FK_problem_tags_problems_ProblemsId",
                    column: x => x.ProblemsId,
                    principalTable: "problems",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_problem_tags_tags_TagsId",
                    column: x => x.TagsId,
                    principalTable: "tags",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(name: "IX_problem_tags_TagsId", table: "problem_tags", column: "TagsId");

        migrationBuilder.CreateIndex(name: "IX_tags_name", table: "tags", column: "name", unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "problem_tags");

        migrationBuilder.DropTable(name: "tags");
    }
}