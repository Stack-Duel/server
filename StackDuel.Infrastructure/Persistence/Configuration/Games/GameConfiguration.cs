using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.ValueObjects;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Tracks.Entities;
using StackDuel.Domain.Users.Entities;

namespace StackDuel.Infrastructure.Persistence.Configuration.Games;

internal sealed class GameConfiguration : IEntityTypeConfiguration<Game>
{
    public void Configure(EntityTypeBuilder<Game> builder)
    {
        builder.ToTable("games");

        builder.HasKey(game => game.Id);

        builder.Property(game => game.Id).HasColumnName("id");

        builder.Property(game => game.LobbyId).HasColumnName("lobby_id").IsRequired(false);

        builder.Property(game => game.GameModeId).HasColumnName("game_mode_id").IsRequired();

        builder.Property(game => game.PoolId).HasColumnName("pool_id").IsRequired();

        builder.Property(game => game.JoinCode).HasColumnName("join_code").HasMaxLength(7).IsRequired();

        builder.HasIndex(game => game.JoinCode).IsUnique();

        builder.Property(game => game.TimeLimitInSeconds).HasColumnName("time_limit_in_seconds").IsRequired();

        builder.Property(game => game.SkipsEnabled).HasColumnName("skips_enabled").IsRequired().HasDefaultValue(true);

        builder.Property(game => game.Status).HasColumnName("status").HasConversion<int>().IsRequired();

        builder.Property(game => game.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.Property(game => game.StartedAt).HasColumnName("started_at").IsRequired(false);

        builder.Property(game => game.EndedAt).HasColumnName("ended_at").IsRequired(false);

        builder
            .HasOne<GameMode>()
            .WithMany()
            .HasForeignKey(game => game.GameModeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<ProblemPool>()
            .WithMany()
            .HasForeignKey(game => game.PoolId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(game => game.Participants)
            .WithOne()
            .HasForeignKey("game_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Navigation(game => game.Participants)
            .HasField("_participants")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder
            .HasMany(game => game.Tracks)
            .WithOne()
            .HasForeignKey("game_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(game => game.Tracks).HasField("_tracks").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder
            .HasMany(game => game.ProblemSequence)
            .WithOne()
            .HasForeignKey("game_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Navigation(game => game.ProblemSequence)
            .HasField("_problemSequence")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class GameProblemConfiguration : IEntityTypeConfiguration<GameProblem>
{
    public void Configure(EntityTypeBuilder<GameProblem> builder)
    {
        builder.ToTable("game_problems");

        builder.HasKey(gp => gp.Id);

        builder.Property(gp => gp.Id).HasColumnName("id");

        builder.Property<Guid>("game_id").HasColumnName("game_id").IsRequired();

        builder.Property(gp => gp.Position).HasColumnName("position").IsRequired();

        builder.Property(gp => gp.ProblemId).HasColumnName("problem_id").IsRequired();

        builder
            .HasOne<Problem>()
            .WithMany()
            .HasForeignKey(gp => gp.ProblemId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // The concurrency safety net: when two participants race to be the first to reach a new
        // position, both build the same next-position GameProblem in memory, but only one
        // SaveChangesAsync can win this unique index. The loser's save fails cleanly instead of
        // silently persisting two different problems at the same position.
        builder.HasIndex("game_id", nameof(GameProblem.Position)).IsUnique();
    }
}

internal sealed class GameTrackConfiguration : IEntityTypeConfiguration<GameTrack>
{
    public void Configure(EntityTypeBuilder<GameTrack> builder)
    {
        builder.ToTable("game_tracks");

        builder.HasKey(gt => gt.Id);

        builder.Property(gt => gt.Id).HasColumnName("id");

        builder.Property<Guid>("game_id").HasColumnName("game_id").IsRequired();

        builder.Property(gt => gt.TrackId).HasColumnName("track_id").IsRequired();

        builder
            .HasOne<Track>()
            .WithMany()
            .HasForeignKey(gt => gt.TrackId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("game_id", nameof(GameTrack.TrackId)).IsUnique();

        builder
            .HasMany(gt => gt.Languages)
            .WithOne()
            .HasForeignKey("game_track_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(gt => gt.Languages).HasField("_languages").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class GameTrackLanguageConfiguration : IEntityTypeConfiguration<GameTrackLanguage>
{
    public void Configure(EntityTypeBuilder<GameTrackLanguage> builder)
    {
        builder.ToTable("game_track_languages");

        builder.HasKey(gtl => gtl.Id);

        builder.Property(gtl => gtl.Id).HasColumnName("id");

        builder.Property<Guid>("game_track_id").HasColumnName("game_track_id").IsRequired();

        builder.Property(gtl => gtl.LanguageId).HasColumnName("language_id").IsRequired();

        builder
            .HasOne<Language>()
            .WithMany()
            .HasForeignKey(gtl => gtl.LanguageId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("game_track_id", nameof(GameTrackLanguage.LanguageId)).IsUnique();
    }
}

internal sealed class GameParticipantConfiguration : IEntityTypeConfiguration<GameParticipant>
{
    public void Configure(EntityTypeBuilder<GameParticipant> builder)
    {
        builder.ToTable("game_participants");

        builder.HasKey(participant => participant.Id);

        builder.Property(participant => participant.Id).HasColumnName("id");

        builder.Property<Guid>("game_id").HasColumnName("game_id").IsRequired();

        builder.Property(participant => participant.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(participant => participant.SeatNo).HasColumnName("seat_no").IsRequired();

        builder.Property(participant => participant.JoinedAt).HasColumnName("joined_at").IsRequired();

        builder.Property(participant => participant.Score).HasColumnName("score").IsRequired().HasDefaultValue(0);

        builder
            .Property(participant => participant.SkipsRemaining)
            .HasColumnName("skips_remaining")
            .IsRequired()
            .HasDefaultValue(GameParticipant.TotalSkips);

        builder.Property(participant => participant.ForfeitedAt).HasColumnName("forfeited_at").IsRequired(false);

        builder.Property(participant => participant.FinishedAt).HasColumnName("finished_at").IsRequired(false);

        builder
            .Property(participant => participant.ProblemSession)
            .HasColumnName("problem_session")
            .HasColumnType("jsonb")
            .IsRequired(false)
            .HasConversion(session => SerializeProblemSession(session), json => DeserializeProblemSession(json));

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(participant => participant.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("game_id", nameof(GameParticipant.UserId)).IsUnique();

        builder.HasIndex("game_id", nameof(GameParticipant.SeatNo)).IsUnique();
    }

    private static string SerializeProblemSession(GameProblemSession session)
    {
        if (session == null)
            return null;

        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
        };

        return System.Text.Json.JsonSerializer.Serialize(
            new
            {
                session.CurrentProblemId,
                SolvedProblemIds = session.SolvedProblemIds.ToList(),
                SkippedProblemIds = session.SkippedProblemIds.ToList(),
                session.ActiveSubmissionId,
                SolvedProblemSubmissions = session
                    .SolvedProblemSubmissionIds.Select(kvp => new { ProblemId = kvp.Key, SubmissionId = kvp.Value })
                    .ToList(),
            },
            options
        );
    }

    private static GameProblemSession DeserializeProblemSession(string json)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            };

            var document = System.Text.Json.JsonDocument.Parse(
                json,
                new System.Text.Json.JsonDocumentOptions { AllowTrailingCommas = true }
            );
            var root = document.RootElement;

            if (
                root.TryGetProperty("currentProblemId", out var problemIdElement)
                && problemIdElement.TryGetGuid(out var problemId)
            )
            {
                var session = new GameProblemSession(problemId);

                foreach (var solvedId in ParseGuidArray(root, "solvedProblemIds"))
                    session.AddSolvedProblem(solvedId);

                foreach (var skippedId in ParseGuidArray(root, "skippedProblemIds"))
                    session.AddSkippedProblem(skippedId);

                if (
                    root.TryGetProperty("activeSubmissionId", out var activeSubEl)
                    && activeSubEl.ValueKind != System.Text.Json.JsonValueKind.Null
                    && activeSubEl.TryGetGuid(out var activeSubId)
                )
                {
                    session.SetActiveSubmission(activeSubId);
                }

                if (
                    root.TryGetProperty("solvedProblemSubmissions", out var submissionsElement)
                    && submissionsElement.ValueKind == System.Text.Json.JsonValueKind.Array
                )
                {
                    foreach (var item in submissionsElement.EnumerateArray())
                    {
                        if (
                            item.TryGetProperty("problemId", out var problemIdEl)
                            && problemIdEl.TryGetGuid(out var solvedProblemId)
                            && item.TryGetProperty("submissionId", out var submissionIdEl)
                            && submissionIdEl.TryGetGuid(out var solvedSubmissionId)
                        )
                        {
                            session.RecordSolvedSubmission(solvedProblemId, solvedSubmissionId);
                        }
                    }
                }

                return session;
            }
        }
        catch
        {
            return null;
        }

        return null;
    }

    private static IEnumerable<Guid> ParseGuidArray(System.Text.Json.JsonElement root, string propertyName)
    {
        if (
            !root.TryGetProperty(propertyName, out var arrayElement)
            || arrayElement.ValueKind != System.Text.Json.JsonValueKind.Array
        )
            return [];

        return arrayElement.EnumerateArray().Where(item => item.TryGetGuid(out _)).Select(item => item.GetGuid());
    }
}

internal sealed class GameModeConfiguration : IEntityTypeConfiguration<GameMode>
{
    public void Configure(EntityTypeBuilder<GameMode> builder)
    {
        builder.ToTable("game_modes");

        builder.HasKey(mode => mode.Id);

        builder.Property(mode => mode.Id).HasColumnName("id");

        builder.Property(mode => mode.Key).HasColumnName("key").HasMaxLength(100).IsRequired();

        builder.HasIndex(mode => mode.Key).IsUnique();

        builder.Property(mode => mode.Name).HasColumnName("name").HasMaxLength(200).IsRequired();

        builder.Property(mode => mode.Description).HasColumnName("description").HasMaxLength(1000).IsRequired();

        builder.Property(mode => mode.IsBuiltIn).HasColumnName("is_built_in").IsRequired();

        builder.Property(mode => mode.IsActive).HasColumnName("is_active").IsRequired();

        builder.Property(mode => mode.MinPlayers).HasColumnName("min_players").IsRequired();

        builder.Property(mode => mode.MaxPlayers).HasColumnName("max_players").IsRequired();

        builder.Property(mode => mode.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.Property(mode => mode.DefaultPoolId).HasColumnName("default_pool_id").IsRequired();

        builder
            .HasOne<ProblemPool>()
            .WithMany()
            .HasForeignKey(mode => mode.DefaultPoolId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(mode => mode.TimeOptions)
            .WithOne()
            .HasForeignKey("game_mode_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Navigation(mode => mode.TimeOptions)
            .HasField("_timeOptions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class GameModeTimeOptionConfiguration : IEntityTypeConfiguration<GameModeTimeOption>
{
    public void Configure(EntityTypeBuilder<GameModeTimeOption> builder)
    {
        builder.ToTable("game_mode_time_options");

        builder.HasKey(option => option.Id);

        builder.Property(option => option.Id).HasColumnName("id");

        builder.Property<Guid>("game_mode_id").HasColumnName("game_mode_id").IsRequired();

        builder.Property(option => option.DurationSeconds).HasColumnName("duration_seconds").IsRequired();

        builder.Property(option => option.IsDefault).HasColumnName("is_default").IsRequired();

        builder.HasIndex("game_mode_id", nameof(GameModeTimeOption.DurationSeconds)).IsUnique();
    }
}