using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.ProblemValidation.Entities;
using System.Text.Json;

namespace StackDuel.Infrastructure.Persistence.Configuration.ProblemValidation;

internal sealed class ProblemValidationJobConfiguration : IEntityTypeConfiguration<ProblemValidationJob>
{
    public void Configure(EntityTypeBuilder<ProblemValidationJob> builder)
    {
        builder.ToTable("problem_validation_jobs");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.Id).HasColumnName("id");

        builder.Property(j => j.ProblemId).HasColumnName("problem_id").IsRequired();

        builder.Property(j => j.Status).HasColumnName("status").HasConversion<int>().IsRequired();

        builder
            .Property(j => j.TestCaseGenerationJobIds)
            .HasColumnName("test_case_generation_job_ids")
            .HasColumnType("jsonb")
            .HasConversion(
                ids => JsonSerializer.Serialize(ids, (JsonSerializerOptions?)null),
                json =>
                    JsonSerializer.Deserialize<IReadOnlyList<Guid>>(json, (JsonSerializerOptions?)null)
                    ?? new List<Guid>()
            )
            .IsRequired();

        builder.Property(j => j.ResultSummary).HasColumnName("result_summary").IsRequired(false);

        builder.Property(j => j.FailureReason).HasColumnName("failure_reason").IsRequired(false);

        builder.Property(j => j.CreatedAt).HasColumnName("created_at").IsRequired();

        builder.Property(j => j.CompletedAt).HasColumnName("completed_at").IsRequired(false);

        builder.Property(j => j.LockedUntil).HasColumnName("locked_until").IsRequired(false);

        builder.HasIndex(j => new { j.Status, j.LockedUntil });
    }
}