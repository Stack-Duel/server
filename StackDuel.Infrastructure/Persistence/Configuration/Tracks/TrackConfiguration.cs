using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.Tracks.Entities;

namespace StackDuel.Infrastructure.Persistence.Configuration.Tracks;

internal sealed class TrackConfiguration : IEntityTypeConfiguration<Track>
{
    public void Configure(EntityTypeBuilder<Track> builder)
    {
        builder.ToTable("tracks");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("id");

        builder.Property(t => t.Key).HasColumnName("key").HasMaxLength(100).IsRequired();

        builder.HasIndex(t => t.Key).IsUnique();

        builder.Property(t => t.Name).HasColumnName("name").HasMaxLength(200).IsRequired();

        builder.Property(t => t.IsActive).HasColumnName("is_active").IsRequired();

        builder.Property(t => t.AllowsLanguageSelection).HasColumnName("allows_language_selection").IsRequired();
    }
}