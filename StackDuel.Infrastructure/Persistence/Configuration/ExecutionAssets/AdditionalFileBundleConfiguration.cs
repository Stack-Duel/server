using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.ExecutionAssets.Entities;
using StackDuel.Domain.ExecutionAssets.ValueObjects;

namespace StackDuel.Infrastructure.Persistence.Configuration.ExecutionAssets;

internal sealed class AdditionalFileBundleConfiguration : IEntityTypeConfiguration<AdditionalFileBundle>
{
    public void Configure(EntityTypeBuilder<AdditionalFileBundle> builder)
    {
        builder.ToTable("additional_file_bundles");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id).HasColumnName("id");

        builder
            .Property(b => b.Name)
            .HasColumnName("name")
            .HasMaxLength(AdditionalFileBundleName.MaxLength)
            .IsRequired()
            .HasConversion(n => n.Value, v => new AdditionalFileBundleName(v));

        builder.HasIndex(b => b.Name).IsUnique();

        builder.Property(b => b.Content).HasColumnName("content").IsRequired();

        builder.Property(b => b.CreatedAt).HasColumnName("created_at").IsRequired();
    }
}