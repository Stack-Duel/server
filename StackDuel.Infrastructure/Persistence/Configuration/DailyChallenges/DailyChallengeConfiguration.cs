using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.DailyChallenges.Entities;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Infrastructure.Persistence.Configuration.DailyChallenges;

internal sealed class DailyChallengeConfiguration : IEntityTypeConfiguration<DailyChallenge>
{
    public void Configure(EntityTypeBuilder<DailyChallenge> builder)
    {
        builder.ToTable("daily_challenges");

        builder.HasKey(challenge => challenge.Id);

        builder.Property(challenge => challenge.Id).HasColumnName("id");

        builder.Property(challenge => challenge.ChallengeDate).HasColumnName("challenge_date").IsRequired();

        builder.HasIndex(challenge => challenge.ChallengeDate).IsUnique();

        builder.Property(challenge => challenge.ProblemId).HasColumnName("problem_id").IsRequired();

        builder
            .HasOne<Problem>()
            .WithMany()
            .HasForeignKey(challenge => challenge.ProblemId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}