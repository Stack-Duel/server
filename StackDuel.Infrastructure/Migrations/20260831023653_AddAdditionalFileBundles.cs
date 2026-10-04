using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddAdditionalFileBundles : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "additional_file_bundle_id",
            table: "problem_setups",
            type: "uuid",
            nullable: true
        );

        migrationBuilder.CreateTable(
            name: "additional_file_bundles",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                content = table.Column<byte[]>(type: "bytea", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_additional_file_bundles", x => x.id);
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_problem_setups_additional_file_bundle_id",
            table: "problem_setups",
            column: "additional_file_bundle_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_additional_file_bundles_name",
            table: "additional_file_bundles",
            column: "name",
            unique: true
        );

        migrationBuilder.AddForeignKey(
            name: "FK_problem_setups_additional_file_bundles_additional_file_bund~",
            table: "problem_setups",
            column: "additional_file_bundle_id",
            principalTable: "additional_file_bundles",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(
            name: "FK_problem_setups_additional_file_bundles_additional_file_bund~",
            table: "problem_setups"
        );

        migrationBuilder.DropTable(name: "additional_file_bundles");

        migrationBuilder.DropIndex(name: "IX_problem_setups_additional_file_bundle_id", table: "problem_setups");

        migrationBuilder.DropColumn(name: "additional_file_bundle_id", table: "problem_setups");
    }
}