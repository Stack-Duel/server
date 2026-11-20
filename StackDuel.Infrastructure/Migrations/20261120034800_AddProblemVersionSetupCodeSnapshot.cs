using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackDuel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProblemVersionSetupCodeSnapshot : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "function_name",
                table: "problem_version_setups",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "initial_code",
                table: "problem_version_setups",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "language_version_id",
                table: "problem_version_setups",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql(
                """
                update problem_version_setups vs
                set language_version_id = s.language_version_id,
                    initial_code = s.initial_code,
                    function_name = s.function_name
                from problem_setups s
                where s.id = vs.problem_setup_id;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "function_name",
                table: "problem_version_setups");

            migrationBuilder.DropColumn(
                name: "initial_code",
                table: "problem_version_setups");

            migrationBuilder.DropColumn(
                name: "language_version_id",
                table: "problem_version_setups");
        }
    }
}
