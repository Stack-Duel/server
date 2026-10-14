using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.Games.Entities;
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
            .HasMany(game => game.Rounds)
            .WithOne()
            .HasForeignKey("game_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(game => game.Rounds).HasField("_rounds").UsePropertyAccessMode(PropertyAccessMode.Field);
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
        builder.ToTable(
            "game_participants",
            table =>
                table.HasCheckConstraint(
                    "ck_game_participants_current_round_pair",
                    "(current_round_id IS NULL) = (current_round_started_at IS NULL)"
                )
        );

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
            .Property(participant => participant.CurrentRoundId)
            .HasColumnName("current_round_id")
            .IsRequired(false);

        builder
            .Property(participant => participant.CurrentRoundStartedAt)
            .HasColumnName("current_round_started_at")
            .IsRequired(false);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(participant => participant.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("game_id", nameof(GameParticipant.UserId)).IsUnique();

        builder.HasIndex("game_id", nameof(GameParticipant.SeatNo)).IsUnique();

        builder.HasAlternateKey("Id", "game_id");

        builder.HasAlternateKey("Id", "game_id", nameof(GameParticipant.UserId));

        builder
            .HasMany<GameRoundResult>()
            .WithOne()
            .HasForeignKey("participant_id", "game_id")
            .HasPrincipalKey("Id", "game_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany<GameRoundSubmission>()
            .WithOne()
            .HasForeignKey(
                nameof(GameRoundSubmission.ParticipantId),
                nameof(GameRoundSubmission.GameId),
                nameof(GameRoundSubmission.UserId)
            )
            .HasPrincipalKey("Id", "game_id", nameof(GameParticipant.UserId))
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(participant => participant.RoundResults)
            .WithOne()
            .HasForeignKey("participant_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Navigation(participant => participant.RoundResults)
            .HasField("_roundResults")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
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