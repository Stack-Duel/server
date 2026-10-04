using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class AddTracks : Migration
{
    private const string GeneralPurposeTrackId = "11111111-1111-1111-1111-111111111111";
    private const string SqlTrackId = "22222222-2222-2222-2222-222222222222";
    private const string FrontendTrackId = "33333333-3333-3333-3333-333333333333";

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterColumn<string>(
            name: "function_name",
            table: "problem_setups",
            type: "character varying(200)",
            maxLength: 200,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(200)",
            oldMaxLength: 200
        );

        migrationBuilder.CreateTable(
            name: "tracks",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tracks", x => x.id);
            }
        );

        migrationBuilder.CreateIndex(name: "IX_tracks_key", table: "tracks", column: "key", unique: true);

        // Fixed ids so pre-existing rows (languages, games) can be backfilled to a real track
        // within this same migration — TrackSeeder recognizes these by key at startup and will
        // not duplicate them.
        migrationBuilder.InsertData(
            table: "tracks",
            columns: new[] { "id", "key", "name", "is_active" },
            values: new object[,]
            {
                { GeneralPurposeTrackId, "general-purpose", "General Purpose", true },
                { SqlTrackId, "sql", "SQL", true },
                { FrontendTrackId, "frontend", "Frontend", false },
            }
        );

        migrationBuilder.AddColumn<Guid>(name: "track_id", table: "languages", type: "uuid", nullable: true);

        migrationBuilder.Sql($"UPDATE languages SET track_id = '{SqlTrackId}' WHERE slug = 'sqlite'");
        migrationBuilder.Sql($"UPDATE languages SET track_id = '{GeneralPurposeTrackId}' WHERE track_id IS NULL");

        migrationBuilder.AlterColumn<Guid>(
            name: "track_id",
            table: "languages",
            type: "uuid",
            nullable: false,
            oldClrType: typeof(Guid),
            oldType: "uuid",
            oldNullable: true
        );

        migrationBuilder.CreateIndex(name: "IX_languages_track_id", table: "languages", column: "track_id");

        migrationBuilder.AddForeignKey(
            name: "FK_languages_tracks_track_id",
            table: "languages",
            column: "track_id",
            principalTable: "tracks",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict
        );

        migrationBuilder.CreateTable(
            name: "game_tracks",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                track_id = table.Column<Guid>(type: "uuid", nullable: false),
                game_id = table.Column<Guid>(type: "uuid", nullable: false),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_game_tracks", x => x.id);
                table.ForeignKey(
                    name: "FK_game_tracks_games_game_id",
                    column: x => x.game_id,
                    principalTable: "games",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade
                );
                table.ForeignKey(
                    name: "FK_game_tracks_tracks_track_id",
                    column: x => x.track_id,
                    principalTable: "tracks",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_game_tracks_game_id_track_id",
            table: "game_tracks",
            columns: new[] { "game_id", "track_id" },
            unique: true
        );

        migrationBuilder.CreateIndex(name: "IX_game_tracks_track_id", table: "game_tracks", column: "track_id");

        // Pre-existing games predate the track concept — put them in general-purpose so they
        // stay queryable rather than left with no track membership at all.
        migrationBuilder.Sql(
            $"INSERT INTO game_tracks (id, game_id, track_id) SELECT gen_random_uuid(), id, '{GeneralPurposeTrackId}' FROM games"
        );
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "game_tracks");

        migrationBuilder.DropForeignKey(name: "FK_languages_tracks_track_id", table: "languages");

        migrationBuilder.DropIndex(name: "IX_languages_track_id", table: "languages");

        migrationBuilder.DropColumn(name: "track_id", table: "languages");

        migrationBuilder.DropTable(name: "tracks");

        migrationBuilder.AlterColumn<string>(
            name: "function_name",
            table: "problem_setups",
            type: "character varying(200)",
            maxLength: 200,
            nullable: false,
            defaultValue: "",
            oldClrType: typeof(string),
            oldType: "character varying(200)",
            oldMaxLength: 200,
            oldNullable: true
        );
    }
}