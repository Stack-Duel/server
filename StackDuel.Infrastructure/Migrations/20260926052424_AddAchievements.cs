using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddAchievements : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "achievement_definitions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                category = table.Column<int>(type: "integer", nullable: false),
                tier = table.Column<int>(type: "integer", nullable: false),
                icon_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                is_secret = table.Column<bool>(type: "boolean", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
                criteria_type = table.Column<int>(type: "integer", nullable: false),
                criteria_stat = table.Column<int>(type: "integer", nullable: true),
                criteria_threshold = table.Column<int>(type: "integer", nullable: true),
                custom_rule_key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_achievement_definitions", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "user_achievement_stats",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                accepted_solve_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                current_solve_streak = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                longest_solve_streak = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                last_solve_date_utc = table.Column<DateOnly>(type: "date", nullable: true),
                games_played = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                games_won = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                games_lost = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                games_drawn = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                current_win_streak = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                longest_win_streak = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                bug_reports_submitted = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                solved_difficulty_mask = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_achievement_stats", x => x.id);
                table.ForeignKey(
                    name: "FK_user_achievement_stats_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "user_achievements",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                achievement_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                earned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_achievements", x => x.id);
                table.ForeignKey(
                    name: "FK_user_achievements_achievement_definitions_achievement_defin~",
                    column: x => x.achievement_definition_id,
                    principalTable: "achievement_definitions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
                table.ForeignKey(
                    name: "FK_user_achievements_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "user_achievement_stat_languages",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                language_id = table.Column<Guid>(type: "uuid", nullable: false),
                user_achievement_stats_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_achievement_stat_languages", x => x.id);
                table.ForeignKey(
                    name: "FK_user_achievement_stat_languages_languages_language_id",
                    column: x => x.language_id,
                    principalTable: "languages",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
                table.ForeignKey(
                    name: "FK_user_achievement_stat_languages_user_achievement_stats_user~",
                    column: x => x.user_achievement_stats_id,
                    principalTable: "user_achievement_stats",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_achievement_definitions_code",
            table: "achievement_definitions",
            column: "code",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_user_achievement_stat_languages_language_id",
            table: "user_achievement_stat_languages",
            column: "language_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_user_achievement_stat_languages_user_achievement_stats_id_l~",
            table: "user_achievement_stat_languages",
            columns: new[] { "user_achievement_stats_id", "language_id" },
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_user_achievement_stats_user_id",
            table: "user_achievement_stats",
            column: "user_id",
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_user_achievements_achievement_definition_id",
            table: "user_achievements",
            column: "achievement_definition_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_user_achievements_user_id_achievement_definition_id",
            table: "user_achievements",
            columns: new[] { "user_id", "achievement_definition_id" },
            unique: true
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "user_achievement_stat_languages");

        migrationBuilder.DropTable(name: "user_achievements");

        migrationBuilder.DropTable(name: "user_achievement_stats");

        migrationBuilder.DropTable(name: "achievement_definitions");
    }
}