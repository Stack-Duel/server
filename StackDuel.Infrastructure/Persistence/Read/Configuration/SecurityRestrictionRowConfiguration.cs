using StackDuel.Infrastructure.Persistence.Read.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Read.Configuration;

internal sealed class SecurityRestrictionRowConfiguration : IEntityTypeConfiguration<SecurityRestrictionRow>
{
    public void Configure(EntityTypeBuilder<SecurityRestrictionRow> builder)
    {
        builder.ToTable("security_restrictions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("Id");
        builder.Property(x => x.UserId).HasColumnName("UserId");
        builder.Property(x => x.PermissionCode).HasColumnName("PermissionCode");
        builder.Property(x => x.Effect).HasColumnName("Effect").HasConversion<string>();
        builder.Property(x => x.ExpiresAt).HasColumnName("ExpiresAt");
    }
}