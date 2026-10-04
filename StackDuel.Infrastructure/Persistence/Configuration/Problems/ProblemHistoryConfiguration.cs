using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Configuration.Problems;

internal sealed class ProblemHistoryConfiguration : IEntityTypeConfiguration<ProblemHistory>
{
    public void Configure(EntityTypeBuilder<ProblemHistory> builder)
    {
        builder.ToTable("problem_history");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id).HasColumnName("id");

        builder.Property<Guid>("problem_id").HasColumnName("problem_id").IsRequired();

        builder
            .Property(h => h.Title)
            .HasColumnName("title")
            .HasMaxLength(Title.MaxLength)
            .IsRequired()
            .HasConversion(t => t.Value, v => new Title(v));

        builder
            .Property(h => h.Question)
            .HasColumnName("question")
            .HasMaxLength(Question.MaxLength)
            .IsRequired()
            .HasConversion(q => q.Value, v => new Question(v));

        builder
            .Property(h => h.Difficulty)
            .HasColumnName("difficulty")
            .IsRequired()
            .HasConversion(d => d.Value, v => new Difficulty(v));

        builder
            .Property(h => h.TimeLimit)
            .HasColumnName("time_limit")
            .IsRequired()
            .HasConversion(t => t.Milliseconds, v => new TimeLimit(v));

        builder
            .Property(h => h.MemoryLimit)
            .HasColumnName("memory_limit")
            .IsRequired()
            .HasConversion(m => m.Megabytes, v => new MemoryLimit(v));

        builder.Property(h => h.CreatedAt).HasColumnName("created_at").IsRequired();
    }
}