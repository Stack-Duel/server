using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Infrastructure.Persistence.Read.Entities;

namespace StackDuel.Infrastructure.Persistence.Read.Configuration;

internal sealed class PermissionRowConfiguration : IEntityTypeConfiguration<PermissionRow>
{
    public void Configure(EntityTypeBuilder<PermissionRow> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Code).HasColumnName("code");
    }
}