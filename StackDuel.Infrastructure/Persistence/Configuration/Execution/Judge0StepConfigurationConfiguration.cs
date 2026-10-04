using StackDuel.Domain.ExecutionPipelines.Entities;
using StackDuel.Infrastructure.ExecutionEngine.Judge0;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Configuration.Execution;

internal sealed class Judge0StepConfigurationConfiguration : IEntityTypeConfiguration<Judge0StepConfiguration>
{
    public void Configure(EntityTypeBuilder<Judge0StepConfiguration> builder)
    {
        builder.ToTable("judge0_step_configurations");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.PipelineStepId).HasColumnName("pipeline_step_id").IsRequired();

        builder.HasIndex(c => c.PipelineStepId).IsUnique();

        builder
            .HasOne<ExecutionPipelineStep>()
            .WithOne()
            .HasForeignKey<Judge0StepConfiguration>(c => c.PipelineStepId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(c => c.IsEncoded).HasColumnName("is_encoded").IsRequired();
        builder.Property(c => c.ShouldWait).HasColumnName("should_wait").IsRequired();
        builder.Property(c => c.StripWhitespace).HasColumnName("strip_whitespace").IsRequired();
        builder.Property(c => c.DefaultTimeoutSeconds).HasColumnName("default_timeout_seconds").IsRequired();
    }
}