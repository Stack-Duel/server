using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.TestSuites.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Configuration.Submissions;

internal sealed class SubmissionResultConfiguration : IEntityTypeConfiguration<SubmissionResult>
{
    public void Configure(EntityTypeBuilder<SubmissionResult> builder)
    {
        builder.ToTable("submission_results");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property<Guid>("submission_id").HasColumnName("submission_id").IsRequired();

        builder.Property(r => r.TestCaseId).HasColumnName("test_case_id").IsRequired();

        builder
            .HasOne<TestCase>()
            .WithMany()
            .HasForeignKey(r => r.TestCaseId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Status).HasColumnName("status").HasConversion<int>().IsRequired();

        builder.Property(r => r.Runtime).HasColumnName("runtime").IsRequired(false);

        builder.Property(r => r.MemoryUsed).HasColumnName("memory_used").IsRequired(false);

        builder.Property(r => r.ActualOutput).HasColumnName("actual_output").IsRequired(false);

        builder.Property(r => r.StandardOutput).HasColumnName("standard_output").IsRequired(false);

        builder.Property(r => r.StandardError).HasColumnName("standard_error").IsRequired(false);

        builder.Property(r => r.CompileOutput).HasColumnName("compile_output").IsRequired(false);

        builder.Property<string?>("execution_id").HasColumnName("execution_id").HasMaxLength(100);

        builder.Property<string?>("evaluation_id").HasColumnName("evaluation_id").HasMaxLength(100);
    }
}