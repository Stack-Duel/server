using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.ProblemValidation.Entities;

namespace StackDuel.Infrastructure.Persistence.Configuration.ProblemValidation;

internal sealed class ProblemValidationStepConfiguration : IEntityTypeConfiguration<ProblemValidationStep>
{
    public void Configure(EntityTypeBuilder<ProblemValidationStep> builder)
    {
        builder.ToTable("problem_validation_steps");

        builder.HasKey(step => step.Id);

        builder.Property(step => step.Id).HasColumnName("id");

        builder.Property(step => step.Kind).HasColumnName("kind").HasConversion<int>().IsRequired();

        builder.Property(step => step.Status).HasColumnName("status").HasConversion<int>().IsRequired();

        builder.Property(step => step.StartedAt).HasColumnName("started_at").IsRequired(false);

        builder.Property(step => step.CompletedAt).HasColumnName("completed_at").IsRequired(false);

        builder.Property(step => step.Detail).HasColumnName("detail").HasMaxLength(2000).IsRequired(false);

        builder
            .HasMany(step => step.Items)
            .WithOne()
            .HasForeignKey("problem_validation_step_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(step => step.Items).HasField("_items").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
