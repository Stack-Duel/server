using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Users.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Configuration.Campaigns;

internal sealed class CampaignEnrollmentConfiguration : IEntityTypeConfiguration<CampaignEnrollment>
{
    public void Configure(EntityTypeBuilder<CampaignEnrollment> builder)
    {
        builder.ToTable("campaign_enrollments");

        builder.HasKey(enrollment => enrollment.Id);

        builder.Property(enrollment => enrollment.Id).HasColumnName("id");

        builder.Property(enrollment => enrollment.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(enrollment => enrollment.CampaignId).HasColumnName("campaign_id").IsRequired();

        builder.Property(enrollment => enrollment.Status).HasColumnName("status").HasConversion<int>().IsRequired();

        builder.Property(enrollment => enrollment.EnrolledAt).HasColumnName("enrolled_at").IsRequired();

        builder.Property(enrollment => enrollment.CompletedAt).HasColumnName("completed_at").IsRequired(false);

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(enrollment => enrollment.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<Campaign>()
            .WithMany()
            .HasForeignKey(enrollment => enrollment.CampaignId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(enrollment => new { enrollment.UserId, enrollment.CampaignId }).IsUnique();
    }
}

internal sealed class UnitCompletionConfiguration : IEntityTypeConfiguration<UnitCompletion>
{
    public void Configure(EntityTypeBuilder<UnitCompletion> builder)
    {
        builder.ToTable("unit_completions");

        builder.HasKey(completion => completion.Id);

        builder.Property(completion => completion.Id).HasColumnName("id");

        builder.Property(completion => completion.UserId).HasColumnName("user_id").IsRequired();

        builder.Property(completion => completion.CampaignUnitId).HasColumnName("campaign_unit_id").IsRequired();

        builder.Property(completion => completion.CompletedAt).HasColumnName("completed_at").IsRequired();

        builder
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(completion => completion.UserId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne<CampaignUnit>()
            .WithMany()
            .HasForeignKey(completion => completion.CampaignUnitId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(completion => new { completion.UserId, completion.CampaignUnitId }).IsUnique();
    }
}