using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddGroupsAndPermissions : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "groups",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_groups", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "permissions",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                code = table.Column<string>(type: "text", nullable: false),
                description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_permissions", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "roles",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_roles", x => x.id);
            }
        );

        migrationBuilder.CreateTable(
            name: "security_restrictions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                UserId = table.Column<Guid>(type: "uuid", nullable: false),
                PermissionCode = table.Column<string>(type: "text", nullable: false),
                Effect = table.Column<string>(type: "text", nullable: false),
                ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                Reason = table.Column<string>(type: "text", nullable: false),
                DetectionEventId = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_security_restrictions", x => x.Id);
            }
        );

        migrationBuilder.CreateTable(
            name: "user_groups",
            columns: table => new
            {
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                group_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_groups", x => new { x.user_id, x.group_id });
                table.ForeignKey(
                    name: "FK_user_groups_groups_group_id",
                    column: x => x.group_id,
                    principalTable: "groups",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_user_groups_users_user_id",
                    column: x => x.user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "group_roles",
            columns: table => new
            {
                group_id = table.Column<Guid>(type: "uuid", nullable: false),
                role_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_group_roles", x => new { x.group_id, x.role_id });
                table.ForeignKey(
                    name: "FK_group_roles_groups_group_id",
                    column: x => x.group_id,
                    principalTable: "groups",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_group_roles_roles_role_id",
                    column: x => x.role_id,
                    principalTable: "roles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "role_permissions",
            columns: table => new
            {
                permission_id = table.Column<Guid>(type: "uuid", nullable: false),
                role_id = table.Column<Guid>(type: "uuid", nullable: false),
                effect = table.Column<string>(type: "text", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_role_permissions", x => new { x.permission_id, x.role_id });
                table.ForeignKey(
                    name: "FK_role_permissions_permissions_permission_id",
                    column: x => x.permission_id,
                    principalTable: "permissions",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
                table.ForeignKey(
                    name: "FK_role_permissions_roles_role_id",
                    column: x => x.role_id,
                    principalTable: "roles",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
            }
        );

        migrationBuilder.CreateIndex(name: "IX_group_roles_role_id", table: "group_roles", column: "role_id");

        migrationBuilder.CreateIndex(name: "IX_permissions_code", table: "permissions", column: "code", unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_role_permissions_permission_id",
            table: "role_permissions",
            column: "permission_id"
        );

        migrationBuilder.CreateIndex(name: "IX_role_permissions_role_id", table: "role_permissions", column: "role_id");

        migrationBuilder.CreateIndex(
            name: "IX_security_restrictions_UserId_PermissionCode",
            table: "security_restrictions",
            columns: new[] { "UserId", "PermissionCode" }
        );

        migrationBuilder.CreateIndex(name: "IX_user_groups_group_id", table: "user_groups", column: "group_id");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "group_roles");

        migrationBuilder.DropTable(name: "role_permissions");

        migrationBuilder.DropTable(name: "security_restrictions");

        migrationBuilder.DropTable(name: "user_groups");

        migrationBuilder.DropTable(name: "permissions");

        migrationBuilder.DropTable(name: "roles");

        migrationBuilder.DropTable(name: "groups");
    }
}