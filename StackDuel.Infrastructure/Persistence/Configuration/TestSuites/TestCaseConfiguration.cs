using StackDuel.Domain.TestCaseGeneration.Entities;
using StackDuel.Domain.TestSuites.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Configuration.TestSuites;

internal sealed class TestCaseConfiguration : IEntityTypeConfiguration<TestCase>
{
    public void Configure(EntityTypeBuilder<TestCase> builder)
    {
        builder.ToTable("test_cases");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property<Guid>("test_suite_id").HasColumnName("test_suite_id").IsRequired();

        builder.Property(t => t.Name).HasColumnName("name").HasMaxLength(200).IsRequired();

        builder.Property(t => t.Description).HasColumnName("description").IsRequired(false);

        builder
            .Property(t => t.Source)
            .HasColumnName("source")
            .HasConversion<int>()
            .HasDefaultValue(StackDuel.Domain.TestSuites.Enums.TestCaseSource.Authored)
            .IsRequired();

        builder.Property(t => t.GenerationSpecId).HasColumnName("generation_spec_id").IsRequired(false);

        builder.Property(t => t.GenerationCaseIndex).HasColumnName("generation_case_index").IsRequired(false);

        builder.Property(t => t.RetiredAt).HasColumnName("retired_at").IsRequired(false);

        builder
            .HasOne<TestCaseGenerationSpec>()
            .WithMany()
            .HasForeignKey(t => t.GenerationSpecId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(t => t.Inputs)
            .WithOne()
            .HasForeignKey("test_case_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(t => t.Inputs).HasField("_inputs").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder
            .HasMany(t => t.ExpectedOutputs)
            .WithOne()
            .HasForeignKey("test_case_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Navigation(t => t.ExpectedOutputs)
            .HasField("_expectedOutputs")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}