using StackDuel.Domain.TestSuites.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Configuration.TestSuites;

internal sealed class TestCaseExpectedOutputConfiguration : IEntityTypeConfiguration<TestCaseExpectedOutput>
{
    public void Configure(EntityTypeBuilder<TestCaseExpectedOutput> builder)
    {
        builder.ToTable("test_case_expected_outputs");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).HasColumnName("id");

        builder.Property<Guid>("test_case_id").HasColumnName("test_case_id").IsRequired();

        builder.Property(o => o.Value).HasColumnName("value").IsRequired();

        builder.Property(o => o.ValueType).HasColumnName("value_type").HasMaxLength(100).IsRequired();
    }
}