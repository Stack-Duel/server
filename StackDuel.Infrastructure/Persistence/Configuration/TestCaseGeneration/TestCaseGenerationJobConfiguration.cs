using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace StackDuel.Infrastructure.Persistence.Configuration.TestCaseGeneration;

internal sealed class TestCaseGenerationJobConfiguration : IEntityTypeConfiguration<TestCaseGenerationJob>
{
    public void Configure(EntityTypeBuilder<TestCaseGenerationJob> builder)
    {
        builder.ToTable("test_case_generation_jobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.Id).HasColumnName("id");

        builder.Property(j => j.ProblemSetupId).HasColumnName("problem_setup_id").IsRequired();

        builder.Property(j => j.ReferenceSolutionCode).HasColumnName("reference_solution_code").IsRequired();

        builder
            .Property(j => j.Parameters)
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

        builder.Property(j => j.OutputValueType).HasColumnName("output_value_type").HasMaxLength(100).IsRequired();

        builder.Property(j => j.TargetCaseCount).HasColumnName("target_case_count").IsRequired();

        builder.Property(j => j.Seed).HasColumnName("seed").IsRequired();

        builder.Property(j => j.Status).HasColumnName("status").HasConversion<int>().IsRequired();

        builder.Property(j => j.ResultSummary).HasColumnName("result_summary").IsRequired(false);

        builder.Property(j => j.FailureReason).HasColumnName("failure_reason").IsRequired(false);

        builder.Property(j => j.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.Property(j => j.CompletedAt).HasColumnName("completed_at").IsRequired(false);

        builder.Property(j => j.LockedUntil).HasColumnName("locked_until").IsRequired(false);

        builder.HasIndex(j => j.Status);
    }
}