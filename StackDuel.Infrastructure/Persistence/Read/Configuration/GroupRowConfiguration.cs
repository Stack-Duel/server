using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Infrastructure.Persistence.Read.Entities;

namespace StackDuel.Infrastructure.Persistence.Read.Configuration;

internal sealed class GroupRowConfiguration : IEntityTypeConfiguration<GroupRow>
{
    public void Configure(EntityTypeBuilder<GroupRow> builder)
    {
        builder.ToTable("groups");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Name).HasColumnName("name");
    }
}