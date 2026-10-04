using StackDuel.Domain.Achievements.Entities;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Configuration.Achievements;

internal sealed class UserAchievementStatsConfiguration : IEntityTypeConfiguration<UserAchievementStats>
{
    public void Configure(EntityTypeBuilder<UserAchievementStats> builder)
    {
        builder.ToTable("user_achievement_stats");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.UserId).HasColumnName("user_id").IsRequired();

        builder.HasIndex(s => s.UserId).IsUnique();

        builder
            .Property(s => s.AcceptedSolveCount)
            .HasColumnName("accepted_solve_count")
            .IsRequired()
            .HasDefaultValue(0);

        builder
            .Property<int>("_solvedDifficultyMask")
            .HasColumnName("solved_difficulty_mask")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Ignore(s => s.DistinctDifficultiesSolved);
        builder.Ignore(s => s.DistinctLanguagesUsed);

        builder
            .Property(s => s.CurrentSolveStreak)
            .HasColumnName("current_solve_streak")
            .IsRequired()
            .HasDefaultValue(0);

        builder
            .Property(s => s.LongestSolveStreak)
            .HasColumnName("longest_solve_streak")
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(s => s.LastSolveDateUtc).HasColumnName("last_solve_date_utc").IsRequired(false);

        builder.Property(s => s.GamesPlayed).HasColumnName("games_played").IsRequired().HasDefaultValue(0);

        builder.Property(s => s.GamesWon).HasColumnName("games_won").IsRequired().HasDefaultValue(0);

        builder.Property(s => s.GamesLost).HasColumnName("games_lost").IsRequired().HasDefaultValue(0);

        builder.Property(s => s.GamesDrawn).HasColumnName("games_drawn").IsRequired().HasDefaultValue(0);

        builder.Property(s => s.CurrentWinStreak).HasColumnName("current_win_streak").IsRequired().HasDefaultValue(0);

        builder.Property(s => s.LongestWinStreak).HasColumnName("longest_win_streak").IsRequired().HasDefaultValue(0);

        builder
            .Property(s => s.BugReportsSubmitted)
            .HasColumnName("bug_reports_submitted")
            .IsRequired()
            .HasDefaultValue(0);

        builder.HasOne<User>().WithMany().HasForeignKey(s => s.UserId).IsRequired().OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(s => s.Languages)
            .WithOne()
            .HasForeignKey("user_achievement_stats_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.Languages).HasField("_languages").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class UserAchievementStatLanguageConfiguration : IEntityTypeConfiguration<UserAchievementStatLanguage>
{
    public void Configure(EntityTypeBuilder<UserAchievementStatLanguage> builder)
    {
        builder.ToTable("user_achievement_stat_languages");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).HasColumnName("id");

        builder.Property<Guid>("user_achievement_stats_id").HasColumnName("user_achievement_stats_id").IsRequired();

        builder.Property(l => l.LanguageId).HasColumnName("language_id").IsRequired();

        builder
            .HasOne<Language>()
            .WithMany()
            .HasForeignKey(l => l.LanguageId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("user_achievement_stats_id", nameof(UserAchievementStatLanguage.LanguageId)).IsUnique();
    }
}