using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.Languages.Entities;
using StackDuel.Domain.Languages.ValueObjects;
using StackDuel.Domain.Tracks.Entities;

namespace StackDuel.Infrastructure.Persistence.Configuration.Languages;

internal sealed class LanguageConfiguration : IEntityTypeConfiguration<Language>
{
    public void Configure(EntityTypeBuilder<Language> builder)
    {
        builder.ToTable("languages");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Id).HasColumnName("id");

        builder
            .Property(l => l.Name)
            .HasColumnName("name")
            .HasMaxLength(LanguageName.MaxLength)
            .IsRequired()
            .HasConversion(n => n.Value, v => new LanguageName(v));

        builder
            .Property(l => l.Slug)
            .HasColumnName("slug")
            .HasMaxLength(LanguageSlug.MaxLength)
            .IsRequired()
            .HasConversion(s => s.Value, v => new LanguageSlug(v));

        builder.Property(l => l.Status).HasColumnName("status").HasConversion<int>().IsRequired();

        builder.Property(l => l.TrackId).HasColumnName("track_id").IsRequired();

        builder.HasOne<Track>().WithMany().HasForeignKey(l => l.TrackId).IsRequired().OnDelete(DeleteBehavior.Restrict);

        builder
            .HasMany(l => l.Versions)
            .WithOne()
            .HasForeignKey("language_id")
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(l => l.Versions).HasField("_versions").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}