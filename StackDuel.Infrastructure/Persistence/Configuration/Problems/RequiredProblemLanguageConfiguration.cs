using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.Problems.RequiredLanguages.Entities;

namespace StackDuel.Infrastructure.Persistence.Configuration.Problems;

internal sealed class RequiredProblemLanguageConfiguration : IEntityTypeConfiguration<RequiredProblemLanguage>
{
    public void Configure(EntityTypeBuilder<RequiredProblemLanguage> builder)
    {
        builder.ToTable("required_problem_languages");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");

        builder.Property(x => x.LanguageVersionId).HasColumnName("language_version_id").IsRequired();

        builder.HasIndex(x => x.LanguageVersionId).IsUnique();

        builder.Property(x => x.SortOrder).HasColumnName("sort_order").IsRequired();

        builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
    }
}