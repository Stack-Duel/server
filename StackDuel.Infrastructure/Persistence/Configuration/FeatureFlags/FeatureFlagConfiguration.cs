using StackDuel.Domain.Authorization.Rbac.Entities;
using StackDuel.Domain.Authorization.Rbac.ValueObjects;
using StackDuel.Domain.FeatureFlags.Entities;
using StackDuel.Domain.FeatureFlags.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Configuration.FeatureFlags;

internal class FeatureFlagsConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable("feature_flags");

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Value, v => new FeatureFlagId(v))
            .ValueGeneratedNever();

        builder
            .Property(x => x.Key)
            .HasColumnName("key")
            .HasConversion(k => k.Value, v => new FeatureFlagKey(v))
            .HasMaxLength(FeatureFlagKey.MaxLength)
            .IsRequired();

        builder.HasIndex(x => x.Key).IsUnique();

        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(200).IsRequired();

        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(1000).IsRequired();

        builder.Property(x => x.DefaultEnabled).HasColumnName("default_enabled").IsRequired();

        builder
            .Property(x => x.RolloutPercentage)
            .HasColumnName("rollout_percentage")
            .HasConversion(r => r.Value, v => new RolloutPercentage(v))
            .IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder
            .Navigation(x => x.UserOverrides)
            .HasField("_userOverrides")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder
            .Navigation(x => x.GroupOverrides)
            .HasField("_groupOverrides")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder
            .HasMany(x => x.UserOverrides)
            .WithOne()
            .HasForeignKey("feature_flag_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(x => x.GroupOverrides)
            .WithOne()
            .HasForeignKey("feature_flag_id")
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal class FeatureFlagUserOverridesConfiguration : IEntityTypeConfiguration<FeatureFlagUserOverride>
{
    public void Configure(EntityTypeBuilder<FeatureFlagUserOverride> builder)
    {
        builder.ToTable("feature_flag_user_overrides");

        builder
            .Property(x => x.UserId)
            .HasColumnName("user_id")
            .HasConversion(u => u.Value, v => new UserId(v))
            .IsRequired();

        builder
            .Property<FeatureFlagId>("feature_flag_id")
            .HasColumnName("feature_flag_id")
            .HasConversion(f => f.Value, v => new FeatureFlagId(v))
            .IsRequired();

        builder.HasKey(nameof(FeatureFlagUserOverride.UserId), "feature_flag_id");

        builder.Property(x => x.Effect).HasColumnName("effect").HasConversion<string>().IsRequired();

        builder.HasIndex("feature_flag_id").HasDatabaseName("IX_feature_flag_user_overrides_feature_flag_id");
    }
}

internal class FeatureFlagGroupOverridesConfiguration : IEntityTypeConfiguration<FeatureFlagGroupOverride>
{
    public void Configure(EntityTypeBuilder<FeatureFlagGroupOverride> builder)
    {
        builder.ToTable("feature_flag_group_overrides");

        builder
            .Property(x => x.GroupId)
            .HasColumnName("group_id")
            .HasConversion(g => g.Value, v => new GroupId(v))
            .IsRequired();

        builder
            .Property<FeatureFlagId>("feature_flag_id")
            .HasColumnName("feature_flag_id")
            .HasConversion(f => f.Value, v => new FeatureFlagId(v))
            .IsRequired();

        builder.HasKey(nameof(FeatureFlagGroupOverride.GroupId), "feature_flag_id");

        builder.Property(x => x.Effect).HasColumnName("effect").HasConversion<string>().IsRequired();

        builder.HasOne<Group>().WithMany().HasForeignKey(x => x.GroupId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex("feature_flag_id").HasDatabaseName("IX_feature_flag_group_overrides_feature_flag_id");
    }
}