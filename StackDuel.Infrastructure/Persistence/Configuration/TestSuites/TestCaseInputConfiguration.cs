using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.TestSuites.Entities;

namespace StackDuel.Infrastructure.Persistence.Configuration.TestSuites;

internal sealed class TestCaseInputConfiguration : IEntityTypeConfiguration<TestCaseInput>
{
    public void Configure(EntityTypeBuilder<TestCaseInput> builder)
    {
        builder.ToTable("test_case_inputs");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id).HasColumnName("id");

        builder.Property<Guid>("test_case_id").HasColumnName("test_case_id").IsRequired();

        builder.Property(i => i.Value).HasColumnName("value").IsRequired();

        builder.Property(i => i.ValueType).HasColumnName("value_type").HasMaxLength(100).IsRequired();

        builder.Property(i => i.Position).HasColumnName("position").IsRequired();
    }
}