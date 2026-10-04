using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.Enums;
using StackDuel.Domain.Languages.ValueObjects;

namespace StackDuel.Infrastructure.Persistence.Configuration.Languages;

internal sealed class LanguageVersionConfiguration : IEntityTypeConfiguration<LanguageVersionEntry>
{
    public void Configure(EntityTypeBuilder<LanguageVersionEntry> builder)
    {
        builder.ToTable("language_versions");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id).HasColumnName("id");

        builder.Property<Guid>("language_id").HasColumnName("language_id").IsRequired();

        builder
            .Property(v => v.Version)
            .HasColumnName("version")
            .HasMaxLength(LanguageVersion.MaxLength)
            .IsRequired()
            .HasConversion(v => v.Value, v => new LanguageVersion(v));

        builder
            .Property(v => v.Judge0Id)
            .HasColumnName("judge0_id")
            .IsRequired()
            .HasConversion(j => j.Value, v => new Judge0Id(v));

        builder.Property(v => v.Status).HasColumnName("status").HasConversion<int>().IsRequired();

        builder
            .Property(v => v.ExecutionMode)
            .HasColumnName("execution_mode")
            .HasConversion<int>()
            .HasDefaultValue(TestExecutionMode.PerCase)
            .IsRequired();
    }
}