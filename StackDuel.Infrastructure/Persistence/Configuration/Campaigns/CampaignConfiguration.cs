using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Problems.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Configuration.Campaigns;

internal sealed class CampaignConfiguration : IEntityTypeConfiguration<Campaign>
{
    public void Configure(EntityTypeBuilder<Campaign> builder)
    {
        builder.ToTable("campaigns");

        builder.HasKey(campaign => campaign.Id);

        builder.Property(campaign => campaign.Id).HasColumnName("id");

        builder.Property(campaign => campaign.Slug).HasColumnName("slug").HasMaxLength(200).IsRequired();

        builder.HasIndex(campaign => campaign.Slug).IsUnique();

        builder.Property(campaign => campaign.Title).HasColumnName("title").HasMaxLength(200).IsRequired();

        builder.Property(campaign => campaign.Description).HasColumnName("description").HasMaxLength(4000).IsRequired();

        builder.Property(campaign => campaign.Difficulty).HasColumnName("difficulty").HasConversion<int>().IsRequired();

        builder.Property(campaign => campaign.Status).HasColumnName("status").HasConversion<int>().IsRequired();

        builder.Property(campaign => campaign.IconKey).HasColumnName("icon_key").HasMaxLength(200).IsRequired(false);

        builder.Property(campaign => campaign.SortOrder).HasColumnName("sort_order").IsRequired();

        builder.Property(campaign => campaign.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.Property(campaign => campaign.PublishedAt).HasColumnName("published_at").IsRequired(false);

        builder
            .HasMany(campaign => campaign.Modules)
            .WithOne()
            .HasForeignKey("campaign_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Navigation(campaign => campaign.Modules)
            .HasField("_modules")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder
            .HasMany(campaign => campaign.Prerequisites)
            .WithOne()
            .HasForeignKey("campaign_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Navigation(campaign => campaign.Prerequisites)
            .HasField("_prerequisites")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class CampaignModuleConfiguration : IEntityTypeConfiguration<CampaignModule>
{
    public void Configure(EntityTypeBuilder<CampaignModule> builder)
    {
        builder.ToTable("campaign_modules");

        builder.HasKey(module => module.Id);

        builder.Property(module => module.Id).HasColumnName("id");

        builder.Property<Guid>("campaign_id").HasColumnName("campaign_id").IsRequired();

        builder.Property(module => module.Title).HasColumnName("title").HasMaxLength(200).IsRequired();

        builder.Property(module => module.Description).HasColumnName("description").HasMaxLength(4000).IsRequired();

        builder.Property(module => module.SortOrder).HasColumnName("sort_order").IsRequired();

        builder
            .HasMany(module => module.Units)
            .WithOne()
            .HasForeignKey("campaign_module_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(module => module.Units).HasField("_units").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex("campaign_id", nameof(CampaignModule.SortOrder));
    }
}

internal sealed class CampaignUnitConfiguration : IEntityTypeConfiguration<CampaignUnit>
{
    public void Configure(EntityTypeBuilder<CampaignUnit> builder)
    {
        builder.ToTable("campaign_units");

        builder.HasKey(unit => unit.Id);

        builder.Property(unit => unit.Id).HasColumnName("id");

        builder.Property<Guid>("campaign_module_id").HasColumnName("campaign_module_id").IsRequired();

        builder.Property(unit => unit.Title).HasColumnName("title").HasMaxLength(200).IsRequired();

        builder.Property(unit => unit.Content).HasColumnName("content").HasMaxLength(20000).IsRequired();

        builder.Property(unit => unit.UnitType).HasColumnName("unit_type").HasConversion<int>().IsRequired();

        builder.Property(unit => unit.EstimatedMinutes).HasColumnName("estimated_minutes").IsRequired();

        builder.Property(unit => unit.SortOrder).HasColumnName("sort_order").IsRequired();

        builder
            .HasMany(unit => unit.Problems)
            .WithOne()
            .HasForeignKey("campaign_unit_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(unit => unit.Problems).HasField("_problems").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex("campaign_module_id", nameof(CampaignUnit.SortOrder));
    }
}

internal sealed class UnitProblemConfiguration : IEntityTypeConfiguration<UnitProblem>
{
    public void Configure(EntityTypeBuilder<UnitProblem> builder)
    {
        builder.ToTable("unit_problems");

        builder.HasKey(unitProblem => unitProblem.Id);

        builder.Property(unitProblem => unitProblem.Id).HasColumnName("id");

        builder.Property<Guid>("campaign_unit_id").HasColumnName("campaign_unit_id").IsRequired();

        builder.Property(unitProblem => unitProblem.ProblemId).HasColumnName("problem_id").IsRequired();

        builder.Property(unitProblem => unitProblem.SortOrder).HasColumnName("sort_order").IsRequired();

        builder
            .HasOne<Problem>()
            .WithMany()
            .HasForeignKey(unitProblem => unitProblem.ProblemId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("campaign_unit_id", nameof(UnitProblem.ProblemId)).IsUnique();
    }
}

internal sealed class CampaignPrerequisiteConfiguration : IEntityTypeConfiguration<CampaignPrerequisite>
{
    public void Configure(EntityTypeBuilder<CampaignPrerequisite> builder)
    {
        builder.ToTable("campaign_prerequisites");

        builder.HasKey(prerequisite => prerequisite.Id);

        builder.Property(prerequisite => prerequisite.Id).HasColumnName("id");

        builder.Property<Guid>("campaign_id").HasColumnName("campaign_id").IsRequired();

        builder
            .Property(prerequisite => prerequisite.RequiredCampaignId)
            .HasColumnName("required_campaign_id")
            .IsRequired();

        builder
            .HasOne<Campaign>()
            .WithMany()
            .HasForeignKey(prerequisite => prerequisite.RequiredCampaignId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex("campaign_id", nameof(CampaignPrerequisite.RequiredCampaignId)).IsUnique();
    }
}