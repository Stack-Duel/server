using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using StackDuel.Domain.Tracks.Entities;
using System.Text.Json;

namespace StackDuel.Infrastructure.Persistence.Configuration.Problems;

internal sealed class ProblemConfiguration : IEntityTypeConfiguration<Problem>
{
    public void Configure(EntityTypeBuilder<Problem> builder)
    {
        builder.ToTable("problems");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).HasColumnName("id");

        builder.OwnsOne(
            p => p.Slug,
            slug =>
            {
                slug.Property(s => s.Value).HasColumnName("slug").HasMaxLength(Slug.MaxLength).IsRequired();

                slug.HasIndex(s => s.Value).IsUnique();
            }
        );

        builder.OwnsOne(
            p => p.Title,
            title =>
            {
                title.Property(t => t.Value).HasColumnName("title").HasMaxLength(Title.MaxLength).IsRequired();
            }
        );

        builder
            .Property(p => p.Question)
            .HasColumnName("question")
            .HasMaxLength(Question.MaxLength)
            .IsRequired()
            .HasConversion(q => q.Value, v => new Question(v));

        builder.OwnsOne(
            p => p.Difficulty,
            difficulty =>
            {
                difficulty.Property(d => d.Value).HasColumnName("difficulty").IsRequired();
            }
        );

        builder
            .Property(p => p.TimeLimit)
            .HasColumnName("time_limit")
            .IsRequired()
            .HasConversion(t => t.Milliseconds, v => new TimeLimit(v));

        builder
            .Property(p => p.MemoryLimit)
            .HasColumnName("memory_limit")
            .IsRequired()
            .HasConversion(m => m.Megabytes, v => new MemoryLimit(v));

        builder.Property(p => p.Status).HasColumnName("status").HasConversion<int>().IsRequired();

        builder
            .Property(p => p.ValidationFailureReason)
            .HasColumnName("validation_failure_reason")
            .HasMaxLength(4000)
            .IsRequired(false);

        builder.Property(p => p.TrackId).HasColumnName("track_id").IsRequired(false);

        builder
            .HasOne<Track>()
            .WithMany()
            .HasForeignKey(p => p.TrackId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.OwnsOne(
            p => p.GenerationSpec,
            spec =>
            {
                spec.ToTable("problem_generation_parameters");

                spec.Property(s => s.Parameters)
                    .HasColumnName("parameters")
                    .HasColumnType("jsonb")
                    .HasConversion(
                        parameters => JsonSerializer.Serialize(parameters, (JsonSerializerOptions?)null),
                        json =>
                            JsonSerializer.Deserialize<IReadOnlyList<GenerationParameterSpec>>(
                                json,
                                (JsonSerializerOptions?)null
                            ) ?? new List<GenerationParameterSpec>()
                    )
                    .IsRequired();

                spec.Property(s => s.OutputValueType).HasColumnName("output_value_type").HasMaxLength(100).IsRequired();

                spec.Property(s => s.TargetCaseCount).HasColumnName("target_case_count").IsRequired();

                spec.Property(s => s.Seed).HasColumnName("seed").IsRequired();
            }
        );

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").IsRequired();

        builder
            .HasMany(p => p.History)
            .WithOne()
            .HasForeignKey("problem_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.History).HasField("_history").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder
            .HasMany(p => p.Setups)
            .WithOne()
            .HasForeignKey("problem_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Setups).HasField("_setups").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.Tags).WithMany(t => t.Problems).UsingEntity(j => j.ToTable("problem_tags"));

        builder.Navigation(p => p.Tags).HasField("_tags").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Property(p => p.CreatedById).HasColumnName("created_by_id").IsRequired(false);

        builder
            .HasOne(p => p.CreatedBy)
            .WithMany()
            .HasForeignKey(p => p.CreatedById)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);
    }
}