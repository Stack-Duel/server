using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddFeatureFlags : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "feature_flags",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                default_enabled = table.Column<bool>(type: "boolean", nullable: false),
                rollout_percentage = table.Column<int>(type: "integer", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_feature_flags", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "feature_flag_group_overrides",
            columns: table => new
            {
                group_id = table.Column<Guid>(type: "uuid", nullable: false),
                feature_flag_id = table.Column<Guid>(type: "uuid", nullable: false),
                effect = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_feature_flag_group_overrides", x => new { x.group_id, x.feature_flag_id });
                table.ForeignKey(
                    name: "FK_feature_flag_group_overrides_feature_flags_feature_flag_id",
                    column: x => x.feature_flag_id,
                    principalTable: "feature_flags",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_feature_flag_group_overrides_groups_group_id",
                    column: x => x.group_id,
                    principalTable: "groups",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "feature_flag_user_overrides",
            columns: table => new
            {
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                feature_flag_id = table.Column<Guid>(type: "uuid", nullable: false),
                effect = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_feature_flag_user_overrides", x => new { x.user_id, x.feature_flag_id });
                table.ForeignKey(
                    name: "FK_feature_flag_user_overrides_feature_flags_feature_flag_id",
                    column: x => x.feature_flag_id,
                    principalTable: "feature_flags",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_feature_flag_group_overrides_feature_flag_id",
            table: "feature_flag_group_overrides",
            column: "feature_flag_id"
        );

        migrationBuilder.CreateIndex(
            name: "IX_feature_flag_user_overrides_feature_flag_id",
            table: "feature_flag_user_overrides",
            column: "feature_flag_id"
        );

        migrationBuilder.CreateIndex(name: "IX_feature_flags_key", table: "feature_flags", column: "key", unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "feature_flag_group_overrides");

        migrationBuilder.DropTable(name: "feature_flag_user_overrides");

        migrationBuilder.DropTable(name: "feature_flags");
    }
}