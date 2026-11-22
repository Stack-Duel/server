using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.ProblemValidation.Entities;

namespace StackDuel.Infrastructure.Persistence.Configuration.ProblemValidation;

internal sealed class ProblemValidationStepItemConfiguration
    : IEntityTypeConfiguration<ProblemValidationStepItem>
{
    public void Configure(EntityTypeBuilder<ProblemValidationStepItem> builder)
    {
        builder.ToTable("problem_validation_step_items");

        builder.HasKey(item => item.Id);

        builder.Property(item => item.Id).HasColumnName("id");

        builder.Property(item => item.Label).HasColumnName("label").HasMaxLength(200).IsRequired();

        builder.Property(item => item.Status).HasColumnName("status").HasConversion<int>().IsRequired();

        builder.Property(item => item.Detail).HasColumnName("detail").HasMaxLength(2000).IsRequired(false);
    }
}