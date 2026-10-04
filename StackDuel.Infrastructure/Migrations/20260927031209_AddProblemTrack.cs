using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddProblemTrack : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(name: "track_id", table: "problems", type: "uuid", nullable: true);

        migrationBuilder.CreateIndex(name: "IX_problems_track_id", table: "problems", column: "track_id");

        migrationBuilder.AddForeignKey(
            name: "FK_problems_tracks_track_id",
            table: "problems",
            column: "track_id",
            principalTable: "tracks",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_problems_tracks_track_id", table: "problems");

        migrationBuilder.DropIndex(name: "IX_problems_track_id", table: "problems");

        migrationBuilder.DropColumn(name: "track_id", table: "problems");
    }
}