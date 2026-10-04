using StackDuel.Infrastructure.Persistence.Read.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Read.Configuration;

internal sealed class UserLanguagePreferenceRowConfiguration : IEntityTypeConfiguration<UserLanguagePreferenceRow>
{
    public void Configure(EntityTypeBuilder<UserLanguagePreferenceRow> builder)
    {
        builder.ToTable("user_language_preferences");

        builder.HasKey(x => new { x.UserId, x.LanguageId });

        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.LanguageId).HasColumnName("language_id");
        builder.Property(x => x.Position).HasColumnName("position");
    }
}