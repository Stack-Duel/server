using StackDuel.Infrastructure.Persistence.Read.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Read.Configuration;

internal sealed class RoleRowConfiguration : IEntityTypeConfiguration<RoleRow>
{
    public void Configure(EntityTypeBuilder<RoleRow> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name");
    }
}