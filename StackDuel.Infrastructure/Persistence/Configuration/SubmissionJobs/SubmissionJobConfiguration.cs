using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.ExecutionPipelines.Entities;
using StackDuel.Domain.SubmissionJobs;
using StackDuel.Domain.SubmissionJobs.Entities;
using StackDuel.Domain.Submissions.Entities;

namespace StackDuel.Infrastructure.Persistence.Configuration.SubmissionJobs;

internal sealed class SubmissionJobConfiguration : IEntityTypeConfiguration<SubmissionJob>
{
    public void Configure(EntityTypeBuilder<SubmissionJob> builder)
    {
        builder.ToTable("submission_jobs");

        builder.HasKey(j => j.Id);
        builder.Property(j => j.Id).HasColumnName("id");

        builder.Property(j => j.SubmissionId).HasColumnName("submission_id").IsRequired();
        builder.HasIndex(j => j.SubmissionId).IsUnique();

        builder
            .HasOne<Submission>()
            .WithOne()
            .HasForeignKey<SubmissionJob>(j => j.SubmissionId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(j => j.PipelineId).HasColumnName("pipeline_id").IsRequired();

        builder
            .HasOne<ExecutionPipeline>()
            .WithMany()
            .HasForeignKey(j => j.PipelineId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(j => j.CurrentStepId).HasColumnName("current_step_id").IsRequired(false);

        builder
            .HasOne<ExecutionPipelineStep>()
            .WithMany()
            .HasForeignKey(j => j.CurrentStepId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(j => j.Status).HasColumnName("status").HasConversion<int>().IsRequired();

        builder.Property(j => j.FailureReason).HasColumnName("failure_reason").IsRequired(false);

        builder.Property(j => j.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(j => j.CompletedAt).HasColumnName("completed_at").IsRequired(false);

        builder.Property(j => j.LockedUntil).HasColumnName("locked_until").IsRequired(false);

        builder.HasIndex(j => new
        {
            j.Status,
            j.LockedUntil,
            j.CreatedAt,
        });

        builder
            .HasMany(j => j.Attempts)
            .WithOne()
            .HasForeignKey("job_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(j => j.Attempts).HasField("_attempts").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class SubmissionJobAttemptConfiguration : IEntityTypeConfiguration<SubmissionJobAttempt>
{
    public void Configure(EntityTypeBuilder<SubmissionJobAttempt> builder)
    {
        builder.ToTable("submission_job_attempts");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id");

        builder.Property<Guid>("job_id").HasColumnName("job_id").IsRequired();

        builder.Property(a => a.PipelineStepId).HasColumnName("pipeline_step_id").IsRequired();

        builder
            .HasOne<ExecutionPipelineStep>()
            .WithMany()
            .HasForeignKey(a => a.PipelineStepId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.AttemptNumber).HasColumnName("attempt_number").IsRequired();

        builder.Property(a => a.Status).HasColumnName("status").HasConversion<int>().IsRequired();

        builder.Property(a => a.RequestPayload).HasColumnName("request_payload").IsRequired(false);
        builder.Property(a => a.ResponsePayload).HasColumnName("response_payload").IsRequired(false);
        builder.Property(a => a.Error).HasColumnName("error").IsRequired(false);
        builder.Property(a => a.StartedAt).HasColumnName("started_at").IsRequired();
        builder.Property(a => a.CompletedAt).HasColumnName("completed_at").IsRequired(false);
        builder.Property(a => a.DurationMs).HasColumnName("duration_ms").IsRequired(false);
    }
}