using StackDuel.Domain.Achievements.Entities;
using StackDuel.Domain.Achievements.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Configuration.Achievements;

internal sealed class AchievementDefinitionConfiguration : IEntityTypeConfiguration<AchievementDefinition>
{
    public void Configure(EntityTypeBuilder<AchievementDefinition> builder)
    {
        builder.ToTable("achievement_definitions");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id).HasColumnName("id");

        builder
            .Property(a => a.Code)
            .HasColumnName("code")
            .HasConversion(code => code.Value, value => new AchievementCode(value))
            .HasMaxLength(AchievementCode.MaxLength)
            .IsRequired();

        builder.HasIndex(a => a.Code).IsUnique();

        builder.Property(a => a.Name).HasColumnName("name").HasMaxLength(200).IsRequired();

        builder.Property(a => a.Description).HasColumnName("description").HasMaxLength(1000).IsRequired();

        builder.Property(a => a.Category).HasColumnName("category").HasConversion<int>().IsRequired();

        builder.Property(a => a.Tier).HasColumnName("tier").HasConversion<int>().IsRequired();

        builder.Property(a => a.IconKey).HasColumnName("icon_key").HasMaxLength(100).IsRequired();

        builder.Property(a => a.IsSecret).HasColumnName("is_secret").IsRequired();

        builder.Property(a => a.IsActive).HasColumnName("is_active").IsRequired();

        builder.Property(a => a.CriteriaType).HasColumnName("criteria_type").HasConversion<int>().IsRequired();

        builder.Property(a => a.CriteriaStat).HasColumnName("criteria_stat").HasConversion<int?>().IsRequired(false);

        builder.Property(a => a.CriteriaThreshold).HasColumnName("criteria_threshold").IsRequired(false);

        builder.Property(a => a.CustomRuleKey).HasColumnName("custom_rule_key").HasMaxLength(100).IsRequired(false);

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").IsRequired();
    }
}