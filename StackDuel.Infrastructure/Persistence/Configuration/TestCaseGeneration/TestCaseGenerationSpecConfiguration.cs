using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Text.Json;

namespace StackDuel.Infrastructure.Persistence.Configuration.TestCaseGeneration;

internal sealed class TestCaseGenerationSpecConfiguration : IEntityTypeConfiguration<TestCaseGenerationSpec>
{
    public void Configure(EntityTypeBuilder<TestCaseGenerationSpec> builder)
    {
        builder.ToTable("test_case_generation_specs");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.ProblemSetupId).HasColumnName("problem_setup_id").IsRequired();

        builder
            .Property(s => s.Parameters)
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

        builder.Property(s => s.OutputValueType).HasColumnName("output_value_type").HasMaxLength(100).IsRequired();

        builder.Property(s => s.TargetCaseCount).HasColumnName("target_case_count").IsRequired();

        builder.Property(s => s.Seed).HasColumnName("seed").IsRequired();

        builder.Property(s => s.Version).HasColumnName("version").IsRequired();

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();
    }
}