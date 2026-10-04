using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddAuditLog : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "audit_log_entries",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                actor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                actor_username = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                target_type = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                target_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                details_json = table.Column<string>(type: "jsonb", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_audit_log_entries", x => x.id);
            }
        );

        migrationBuilder.CreateIndex(
            name: "ix_audit_log_entries_actor_user_id",
            table: "audit_log_entries",
            column: "actor_user_id"
        );

        migrationBuilder.CreateIndex(
            name: "ix_audit_log_entries_created_at",
            table: "audit_log_entries",
            column: "created_at"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "audit_log_entries");
    }
}