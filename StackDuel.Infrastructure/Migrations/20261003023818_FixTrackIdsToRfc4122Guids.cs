using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StackDuel.Infrastructure.Migrations;

/// <inheritdoc />
public partial class FixTrackIdsToRfc4122Guids : Migration
{
    private const string OldGeneralPurposeId = "11111111-1111-1111-1111-111111111111";
    private const string OldSqlId = "22222222-2222-2222-2222-222222222222";
    private const string OldFrontendId = "33333333-3333-3333-3333-333333333333";

    private const string NewGeneralPurposeId = "c4ca3593-673d-4440-94ed-a59410479c4c";
    private const string NewSqlId = "b33287e4-153d-4f8a-a3e3-8cbfbcea1454";
    private const string NewFrontendId = "06a25f31-197f-4931-87f3-686e3b4bc153";

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        RenumberTracks(migrationBuilder, fromGeneralPurpose: OldGeneralPurposeId, fromSql: OldSqlId, fromFrontend: OldFrontendId, toGeneralPurpose: NewGeneralPurposeId, toSql: NewSqlId, toFrontend: NewFrontendId);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        RenumberTracks(migrationBuilder, fromGeneralPurpose: NewGeneralPurposeId, fromSql: NewSqlId, fromFrontend: NewFrontendId, toGeneralPurpose: OldGeneralPurposeId, toSql: OldSqlId, toFrontend: OldFrontendId);
    }

    // The three FKs on tracks.id are RESTRICT and non-deferrable, so Postgres checks
    // each one immediately after the statement that touches it — updating tracks.id
    // directly would fail while children still point at the old value. Dropping the
    // FKs for the duration of the data fix avoids that.
    private static void RenumberTracks(
        MigrationBuilder migrationBuilder,
        string fromGeneralPurpose,
        string fromSql,
        string fromFrontend,
        string toGeneralPurpose,
        string toSql,
        string toFrontend
    )
    {
        migrationBuilder.DropForeignKey(name: "FK_languages_tracks_track_id", table: "languages");
        migrationBuilder.DropForeignKey(name: "FK_game_tracks_tracks_track_id", table: "game_tracks");
        migrationBuilder.DropForeignKey(name: "FK_problems_tracks_track_id", table: "problems");

        foreach ((string from, string to) in new[]
        {
            (fromGeneralPurpose, toGeneralPurpose),
            (fromSql, toSql),
            (fromFrontend, toFrontend),
        })
        {
            migrationBuilder.Sql($"UPDATE tracks SET id = '{to}' WHERE id = '{from}';");
            migrationBuilder.Sql($"UPDATE languages SET track_id = '{to}' WHERE track_id = '{from}';");
            migrationBuilder.Sql($"UPDATE game_tracks SET track_id = '{to}' WHERE track_id = '{from}';");
            migrationBuilder.Sql($"UPDATE problems SET track_id = '{to}' WHERE track_id = '{from}';");
        }

        migrationBuilder.AddForeignKey(
            name: "FK_languages_tracks_track_id",
            table: "languages",
            column: "track_id",
            principalTable: "tracks",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict
        );

        migrationBuilder.AddForeignKey(
            name: "FK_game_tracks_tracks_track_id",
            table: "game_tracks",
            column: "track_id",
            principalTable: "tracks",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict
        );

        migrationBuilder.AddForeignKey(
            name: "FK_problems_tracks_track_id",
            table: "problems",
            column: "track_id",
            principalTable: "tracks",
            principalColumn: "id",
            onDelete: ReferentialAction.Restrict
        );
    }
}