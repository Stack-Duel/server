using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackDuel.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGameRoundTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "game_problems");

            migrationBuilder.DropColumn(
                name: "problem_session",
                table: "game_participants");

            migrationBuilder.AddColumn<Guid>(
                name: "current_round_id",
                table: "game_participants",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "current_round_started_at",
                table: "game_participants",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddUniqueConstraint(
                name: "AK_submissions_id_problem_setup_id_user_id",
                table: "submissions",
                columns: new[] { "id", "problem_setup_id", "user_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_problem_setups_id_problem_id",
                table: "problem_setups",
                columns: new[] { "id", "problem_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_game_participants_id_game_id",
                table: "game_participants",
                columns: new[] { "id", "game_id" });

            migrationBuilder.AddUniqueConstraint(
                name: "AK_game_participants_id_game_id_user_id",
                table: "game_participants",
                columns: new[] { "id", "game_id", "user_id" });

            migrationBuilder.CreateTable(
                name: "game_rounds",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    problem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_rounds", x => x.id);
                    table.UniqueConstraint("AK_game_rounds_id_game_id", x => new { x.id, x.game_id });
                    table.UniqueConstraint("AK_game_rounds_id_problem_id_game_id", x => new { x.id, x.problem_id, x.game_id });
                    table.ForeignKey(
                        name: "FK_game_rounds_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_game_rounds_problems_problem_id",
                        column: x => x.problem_id,
                        principalTable: "problems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "game_round_results",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    round_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_round_results", x => x.id);
                    table.UniqueConstraint("AK_game_round_results_id_outcome", x => new { x.id, x.outcome });
                    table.ForeignKey(
                        name: "FK_game_round_results_game_participants_participant_id",
                        column: x => x.participant_id,
                        principalTable: "game_participants",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_game_round_results_game_participants_participant_id_game_id",
                        columns: x => new { x.participant_id, x.game_id },
                        principalTable: "game_participants",
                        principalColumns: new[] { "id", "game_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_game_round_results_game_rounds_round_id_game_id",
                        columns: x => new { x.round_id, x.game_id },
                        principalTable: "game_rounds",
                        principalColumns: new[] { "id", "game_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "game_round_submissions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    round_id = table.Column<Guid>(type: "uuid", nullable: false),
                    participant_id = table.Column<Guid>(type: "uuid", nullable: false),
                    setup_id = table.Column<Guid>(type: "uuid", nullable: false),
                    submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    problem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_round_submissions", x => x.id);
                    table.ForeignKey(
                        name: "FK_game_round_submissions_game_participants_participant_id_gam~",
                        columns: x => new { x.participant_id, x.game_id, x.user_id },
                        principalTable: "game_participants",
                        principalColumns: new[] { "id", "game_id", "user_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_game_round_submissions_game_rounds_round_id_problem_id_game~",
                        columns: x => new { x.round_id, x.problem_id, x.game_id },
                        principalTable: "game_rounds",
                        principalColumns: new[] { "id", "problem_id", "game_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_game_round_submissions_problem_setups_setup_id_problem_id",
                        columns: x => new { x.setup_id, x.problem_id },
                        principalTable: "problem_setups",
                        principalColumns: new[] { "id", "problem_id" },
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_game_round_submissions_submissions_submission_id_setup_id_u~",
                        columns: x => new { x.submission_id, x.setup_id, x.user_id },
                        principalTable: "submissions",
                        principalColumns: new[] { "id", "problem_setup_id", "user_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "game_round_solutions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    submission_id = table.Column<Guid>(type: "uuid", nullable: false),
                    outcome = table.Column<int>(type: "integer", nullable: false),
                    round_result_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_round_solutions", x => x.id);
                    table.CheckConstraint("ck_game_round_solutions_outcome", "outcome = 0");
                    table.ForeignKey(
                        name: "FK_game_round_solutions_game_round_results_round_result_id_out~",
                        columns: x => new { x.round_result_id, x.outcome },
                        principalTable: "game_round_results",
                        principalColumns: new[] { "id", "outcome" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_game_participants_current_round_id_game_id",
                table: "game_participants",
                columns: new[] { "current_round_id", "game_id" });

            migrationBuilder.AddCheckConstraint(
                name: "ck_game_participants_current_round_pair",
                table: "game_participants",
                sql: "(current_round_id IS NULL) = (current_round_started_at IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_game_round_results_participant_id_game_id",
                table: "game_round_results",
                columns: new[] { "participant_id", "game_id" });

            migrationBuilder.CreateIndex(
                name: "IX_game_round_results_participant_id_round_id",
                table: "game_round_results",
                columns: new[] { "participant_id", "round_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_round_results_round_id_game_id",
                table: "game_round_results",
                columns: new[] { "round_id", "game_id" });

            migrationBuilder.CreateIndex(
                name: "IX_game_round_solutions_round_result_id",
                table: "game_round_solutions",
                column: "round_result_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_round_solutions_round_result_id_outcome",
                table: "game_round_solutions",
                columns: new[] { "round_result_id", "outcome" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_round_submissions_participant_id_game_id_user_id",
                table: "game_round_submissions",
                columns: new[] { "participant_id", "game_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_game_round_submissions_participant_id_round_id_created_at",
                table: "game_round_submissions",
                columns: new[] { "participant_id", "round_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_game_round_submissions_round_id_problem_id_game_id",
                table: "game_round_submissions",
                columns: new[] { "round_id", "problem_id", "game_id" });

            migrationBuilder.CreateIndex(
                name: "IX_game_round_submissions_setup_id_problem_id",
                table: "game_round_submissions",
                columns: new[] { "setup_id", "problem_id" });

            migrationBuilder.CreateIndex(
                name: "IX_game_round_submissions_submission_id",
                table: "game_round_submissions",
                column: "submission_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_round_submissions_submission_id_setup_id_user_id",
                table: "game_round_submissions",
                columns: new[] { "submission_id", "setup_id", "user_id" });

            migrationBuilder.CreateIndex(
                name: "IX_game_rounds_game_id_position",
                table: "game_rounds",
                columns: new[] { "game_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_rounds_game_id_problem_id",
                table: "game_rounds",
                columns: new[] { "game_id", "problem_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_rounds_problem_id",
                table: "game_rounds",
                column: "problem_id");

            migrationBuilder.AddForeignKey(
                name: "FK_game_participants_game_rounds_current_round_id_game_id",
                table: "game_participants",
                columns: new[] { "current_round_id", "game_id" },
                principalTable: "game_rounds",
                principalColumns: new[] { "id", "game_id" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_game_participants_game_rounds_current_round_id_game_id",
                table: "game_participants");

            migrationBuilder.DropTable(
                name: "game_round_solutions");

            migrationBuilder.DropTable(
                name: "game_round_submissions");

            migrationBuilder.DropTable(
                name: "game_round_results");

            migrationBuilder.DropTable(
                name: "game_rounds");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_submissions_id_problem_setup_id_user_id",
                table: "submissions");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_problem_setups_id_problem_id",
                table: "problem_setups");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_game_participants_id_game_id",
                table: "game_participants");

            migrationBuilder.DropUniqueConstraint(
                name: "AK_game_participants_id_game_id_user_id",
                table: "game_participants");

            migrationBuilder.DropIndex(
                name: "IX_game_participants_current_round_id_game_id",
                table: "game_participants");

            migrationBuilder.DropCheckConstraint(
                name: "ck_game_participants_current_round_pair",
                table: "game_participants");

            migrationBuilder.DropColumn(
                name: "current_round_id",
                table: "game_participants");

            migrationBuilder.DropColumn(
                name: "current_round_started_at",
                table: "game_participants");

            migrationBuilder.AddColumn<string>(
                name: "problem_session",
                table: "game_participants",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "game_problems",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    position = table.Column<int>(type: "integer", nullable: false),
                    problem_id = table.Column<Guid>(type: "uuid", nullable: false),
                    game_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_game_problems", x => x.id);
                    table.ForeignKey(
                        name: "FK_game_problems_games_game_id",
                        column: x => x.game_id,
                        principalTable: "games",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_game_problems_problems_problem_id",
                        column: x => x.problem_id,
                        principalTable: "problems",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_game_problems_game_id_position",
                table: "game_problems",
                columns: new[] { "game_id", "position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_game_problems_problem_id",
                table: "game_problems",
                column: "problem_id");
        }
    }
}
