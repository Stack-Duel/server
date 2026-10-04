using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Configuration.Problems;

internal sealed class ProblemTagConfiguration : IEntityTypeConfiguration<ProblemTag>
{
    public void Configure(EntityTypeBuilder<ProblemTag> builder)
    {
        builder.ToTable("tags");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("id");

        builder
            .Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(Tag.MaxLength)
            .IsRequired()
            .HasConversion(t => t.Value, v => new Tag(v));

        builder.HasIndex(t => t.Name).IsUnique();

        builder.Navigation(t => t.Problems).HasField("_problems").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}