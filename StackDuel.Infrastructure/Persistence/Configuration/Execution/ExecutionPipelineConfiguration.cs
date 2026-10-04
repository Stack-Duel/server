using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.ExecutionPipelines.Entities;

namespace StackDuel.Infrastructure.Persistence.Configuration.Execution;

internal sealed class ExecutionPipelineConfiguration : IEntityTypeConfiguration<ExecutionPipeline>
{
    public void Configure(EntityTypeBuilder<ExecutionPipeline> builder)
    {
        builder.ToTable("execution_pipelines");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id");

        builder.Property(p => p.Name).HasColumnName("name").HasMaxLength(200).IsRequired();

        builder.Property(p => p.Description).HasColumnName("description").IsRequired(false);

        builder
            .HasMany(p => p.Steps)
            .WithOne()
            .HasForeignKey("pipeline_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Steps).HasField("_steps").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

internal sealed class ExecutionPipelineStepConfiguration : IEntityTypeConfiguration<ExecutionPipelineStep>
{
    public void Configure(EntityTypeBuilder<ExecutionPipelineStep> builder)
    {
        builder.ToTable("execution_pipeline_steps");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property<Guid>("pipeline_id").HasColumnName("pipeline_id").IsRequired();

        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(200).IsRequired();

        builder.Property(s => s.StepType).HasColumnName("step_type").HasConversion<int>().IsRequired();

        builder.Property(s => s.StepOrder).HasColumnName("step_order").IsRequired();
        builder.Property(s => s.MaxAttempts).HasColumnName("max_attempts").IsRequired();
        builder.Property(s => s.TimeoutSeconds).HasColumnName("timeout_seconds").IsRequired();
        builder.Property(s => s.IsPolling).HasColumnName("is_polling").IsRequired();
    }
}