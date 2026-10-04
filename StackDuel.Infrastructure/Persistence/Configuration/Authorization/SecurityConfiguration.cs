using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.Authorization.Rbac.ValueObjects;
using StackDuel.Domain.Authorization.Security.Entities;
using StackDuel.Domain.Authorization.Security.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Configuration.Authorization;

internal class SecurityRestrictionsConfiguration : IEntityTypeConfiguration<SecurityRestriction>
{
    public void Configure(EntityTypeBuilder<SecurityRestriction> builder)
    {
        builder.ToTable("security_restrictions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();

        builder.Property(x => x.UserId).HasConversion(u => u.Value, v => new UserId(v)).IsRequired();

        builder.Property(x => x.PermissionCode).HasConversion(p => p.Value, v => new PermissionCode(v)).IsRequired();

        builder.Property(x => x.Effect).HasConversion<string>().IsRequired();

        builder.Property(x => x.ExpiresAt).IsRequired();

        builder.Property(x => x.Reason).HasConversion(r => r.Value, v => new Reason(v)).IsRequired();

        builder
            .Property(x => x.DetectionEventId)
            .HasConversion(d => d.Value, v => new DetectionEventId(v))
            .IsRequired();

        builder.HasIndex(x => new { x.UserId, x.PermissionCode });
    }
}