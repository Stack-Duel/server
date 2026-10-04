using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.Achievements.Entities;
using StackDuel.Domain.Users.Entities;

namespace StackDuel.Infrastructure.Persistence.Configuration.Achievements;

internal sealed class UserAchievementConfiguration : IEntityTypeConfiguration<UserAchievement>
{
    public void Configure(EntityTypeBuilder<UserAchievement> builder)
    {
        builder.ToTable("user_achievements");

        builder.HasKey(ua => ua.Id);

        builder.Property(ua => ua.Id).HasColumnName("id");

        builder.Property(ua => ua.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(ua => ua.AchievementDefinitionId).HasColumnName("achievement_definition_id").IsRequired();

        builder.Property(ua => ua.EarnedAt).HasColumnName("earned_at").IsRequired();

        builder.HasOne<User>().WithMany().HasForeignKey(ua => ua.UserId).IsRequired().OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne<AchievementDefinition>()
            .WithMany()
            .HasForeignKey(ua => ua.AchievementDefinitionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ua => new { ua.UserId, ua.AchievementDefinitionId }).IsUnique();
    }
}