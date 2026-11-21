using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Infrastructure.Persistence.Configuration.Problems;

internal sealed class ProblemVersionConfiguration : IEntityTypeConfiguration<ProblemVersion>
{
    public void Configure(EntityTypeBuilder<ProblemVersion> builder)
    {
        builder.ToTable("problem_versions");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id).HasColumnName("id");

        builder.Property(v => v.ProblemId).HasColumnName("problem_id").IsRequired();

        builder.Property(v => v.VersionNumber).HasColumnName("version_number").IsRequired();

        builder.HasIndex(v => new { v.ProblemId, v.VersionNumber }).IsUnique();

        builder.Property(v => v.State).HasColumnName("state").HasConversion<int>().IsRequired();

        builder.OwnsOne(
            v => v.Title,
            title =>
            {
                title.Property(t => t.Value).HasColumnName("title").HasMaxLength(Title.MaxLength).IsRequired();
            }
        );

        builder
            .Property(v => v.Question)
            .HasColumnName("question")
            .HasMaxLength(Question.MaxLength)
            .IsRequired()
            .HasConversion(q => q.Value, value => new Question(value));

        builder.OwnsOne(
            v => v.Difficulty,
            difficulty =>
            {
                difficulty.Property(d => d.Value).HasColumnName("difficulty").IsRequired();
            }
        );

        builder
            .Property(v => v.TimeLimit)
            .HasColumnName("time_limit")
            .IsRequired()
            .HasConversion(t => t.Milliseconds, value => new TimeLimit(value));

        builder
            .Property(v => v.MemoryLimit)
            .HasColumnName("memory_limit")
            .IsRequired()
            .HasConversion(m => m.Megabytes, value => new MemoryLimit(value));

        builder.Property(v => v.PublishedAt).HasColumnName("published_at").IsRequired();

        builder.Property(v => v.SupersededAt).HasColumnName("superseded_at").IsRequired(false);

        builder
            .HasMany(v => v.Setups)
            .WithOne()
            .HasForeignKey("problem_version_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(v => v.Setups).HasField("_setups").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}