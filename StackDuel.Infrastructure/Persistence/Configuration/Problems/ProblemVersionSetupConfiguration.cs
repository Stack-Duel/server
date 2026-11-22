using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Infrastructure.Persistence.Configuration.Problems;

internal sealed class ProblemVersionSetupConfiguration : IEntityTypeConfiguration<ProblemVersionSetup>
{
    public void Configure(EntityTypeBuilder<ProblemVersionSetup> builder)
    {
        builder.ToTable("problem_version_setups");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.ProblemSetupId).HasColumnName("problem_setup_id").IsRequired();

        builder.Property(s => s.LanguageVersionId).HasColumnName("language_version_id").IsRequired();

        builder.Property(s => s.InitialCode).HasColumnName("initial_code").IsRequired();

        builder.Property(s => s.FunctionName).HasColumnName("function_name").HasMaxLength(200).IsRequired(false);

        builder
            .HasOne<ProblemSetup>()
            .WithMany()
            .HasForeignKey(s => s.ProblemSetupId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
    }
}