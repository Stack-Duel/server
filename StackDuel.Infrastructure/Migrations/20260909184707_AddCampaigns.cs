using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddCampaigns : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "campaigns",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                slug = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                difficulty = table.Column<int>(type: "integer", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                icon_key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                sort_order = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_campaigns", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "campaign_enrollments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<int>(type: "integer", nullable: false),
                enrolled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_campaign_enrollments", x => x.id);
                table.ForeignKey(
                    name: "FK_campaign_enrollments_campaigns_campaign_id",
                    column: x => x.campaign_id,
                    principalTable: "campaigns",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
                table.ForeignKey(
                    name: "FK_campaign_enrollments_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "campaign_modules",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false),
                campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_campaign_modules", x => x.id);
                table.ForeignKey(
                    name: "FK_campaign_modules_campaigns_campaign_id",
                    column: x => x.campaign_id,
                    principalTable: "campaigns",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "campaign_prerequisites",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                CampaignId = table.Column<Guid>(type: "uuid", nullable: false),
                required_campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
                campaign_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_campaign_prerequisites", x => x.id);
                table.ForeignKey(
                    name: "FK_campaign_prerequisites_campaigns_campaign_id",
                    column: x => x.campaign_id,
                    principalTable: "campaigns",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_campaign_prerequisites_campaigns_required_campaign_id",
                    column: x => x.required_campaign_id,
                    principalTable: "campaigns",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "campaign_units",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                CampaignModuleId = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                content = table.Column<string>(type: "character varying(20000)", maxLength: 20000, nullable: false),
                unit_type = table.Column<int>(type: "integer", nullable: false),
                estimated_minutes = table.Column<int>(type: "integer", nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false),
                campaign_module_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_campaign_units", x => x.id);
                table.ForeignKey(
                    name: "FK_campaign_units_campaign_modules_campaign_module_id",
                    column: x => x.campaign_module_id,
                    principalTable: "campaign_modules",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "unit_completions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                campaign_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
                completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_unit_completions", x => x.id);
                table.ForeignKey(
                    name: "FK_unit_completions_campaign_units_campaign_unit_id",
                    column: x => x.campaign_unit_id,
                    principalTable: "campaign_units",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
                table.ForeignKey(
                    name: "FK_unit_completions_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "unit_problems",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                CampaignUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                problem_id = table.Column<Guid>(type: "uuid", nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false),
                campaign_unit_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_unit_problems", x => x.id);
                table.ForeignKey(
                    name: "FK_unit_problems_campaign_units_campaign_unit_id",
                    column: x => x.campaign_unit_id,
                    principalTable: "campaign_units",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_unit_problems_problems_problem_id",
                    column: x => x.problem_id,
                    principalTable: "problems",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_campaign_enrollments_campaign_id",
            table: "campaign_enrollments",
            column: "campaign_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_campaign_enrollments_user_id_campaign_id",
            table: "campaign_enrollments",
            columns: new[] { "user_id", "campaign_id" },
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_campaign_modules_campaign_id_sort_order",
            table: "campaign_modules",
            columns: new[] { "campaign_id", "sort_order" }
        );

        migrationBuilder.CreateIndex(
            name: "IX_campaign_prerequisites_campaign_id_required_campaign_id",
            table: "campaign_prerequisites",
            columns: new[] { "campaign_id", "required_campaign_id" },
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_campaign_prerequisites_required_campaign_id",
            table: "campaign_prerequisites",
            column: "required_campaign_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_campaign_units_campaign_module_id_sort_order",
            table: "campaign_units",
            columns: new[] { "campaign_module_id", "sort_order" }
        );

        migrationBuilder.CreateIndex(name: "IX_campaigns_slug", table: "campaigns", column: "slug", unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_unit_completions_campaign_unit_id",
            table: "unit_completions",
            column: "campaign_unit_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_unit_completions_user_id_campaign_unit_id",
            table: "unit_completions",
            columns: new[] { "user_id", "campaign_unit_id" },
            unique: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_unit_problems_campaign_unit_id_problem_id",
            table: "unit_problems",
            columns: new[] { "campaign_unit_id", "problem_id" },
            unique: true
        );

        migrationBuilder.CreateIndex(name: "IX_unit_problems_problem_id", table: "unit_problems", column: "problem_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "campaign_enrollments");

        migrationBuilder.DropTable(name: "campaign_prerequisites");

        migrationBuilder.DropTable(name: "unit_completions");

        migrationBuilder.DropTable(name: "unit_problems");

        migrationBuilder.DropTable(name: "campaign_units");

        migrationBuilder.DropTable(name: "campaign_modules");

        migrationBuilder.DropTable(name: "campaigns");
    }
}