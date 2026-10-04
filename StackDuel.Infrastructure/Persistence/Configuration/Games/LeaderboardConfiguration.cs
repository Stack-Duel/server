using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Configuration.Games;

internal sealed class LeaderboardConfiguration : IEntityTypeConfiguration<Leaderboard>
{
    public void Configure(EntityTypeBuilder<Leaderboard> builder)
    {
        builder.ToTable("leaderboards");

        builder.HasKey(leaderboard => leaderboard.Id);

        builder.Property(leaderboard => leaderboard.Id).HasColumnName("id");

        builder.Property(leaderboard => leaderboard.GameModeId).HasColumnName("game_mode_id").IsRequired();

        builder
            .Property(leaderboard => leaderboard.TimeLimitInSeconds)
            .HasColumnName("time_limit_in_seconds")
            .IsRequired();

        builder
            .HasOne<GameMode>()
            .WithMany()
            .HasForeignKey(leaderboard => leaderboard.GameModeId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(leaderboard => new { leaderboard.GameModeId, leaderboard.TimeLimitInSeconds }).IsUnique();

        builder
            .HasMany(leaderboard => leaderboard.Participants)
            .WithOne()
            .HasForeignKey("leaderboard_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Navigation(leaderboard => leaderboard.Participants)
            .HasField("_participants")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Ignore(leaderboard => leaderboard.Rankings);
    }
}

internal sealed class LeaderboardParticipantConfiguration : IEntityTypeConfiguration<LeaderboardParticipant>
{
    public void Configure(EntityTypeBuilder<LeaderboardParticipant> builder)
    {
        builder.ToTable("leaderboard_participants");

        builder.HasKey(participant => participant.Id);

        builder.Property(participant => participant.Id).HasColumnName("id");

        builder.Property<Guid>("leaderboard_id").HasColumnName("leaderboard_id").IsRequired();

        builder.Property(participant => participant.UserId).HasColumnName("user_id").IsRequired();

        builder
            .Property(participant => participant.HighScore)
            .HasColumnName("high_score")
            .IsRequired()
            .HasDefaultValue(0);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(participant => participant.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("leaderboard_id", nameof(LeaderboardParticipant.UserId)).IsUnique();
    }
}