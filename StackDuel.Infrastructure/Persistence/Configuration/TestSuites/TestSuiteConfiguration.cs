using StackDuel.Domain.TestSuites.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Configuration.TestSuites;

internal sealed class TestSuiteConfiguration : IEntityTypeConfiguration<TestSuite>
{
    public void Configure(EntityTypeBuilder<TestSuite> builder)
    {
        builder.ToTable("test_suites");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.Name).HasColumnName("name").HasMaxLength(200).IsRequired();

        builder.Property(s => s.Type).HasColumnName("type").HasConversion<int>().IsRequired();

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").IsRequired();

        builder
            .HasMany(s => s.TestCases)
            .WithOne()
            .HasForeignKey("test_suite_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(s => s.TestCases).HasField("_testCases").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}