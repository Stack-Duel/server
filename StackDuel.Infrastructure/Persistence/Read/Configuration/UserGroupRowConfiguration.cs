using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Infrastructure.Persistence.Read.Entities;

namespace StackDuel.Infrastructure.Persistence.Read.Configuration;

internal sealed class UserGroupRowConfiguration : IEntityTypeConfiguration<UserGroupRow>
{
    public void Configure(EntityTypeBuilder<UserGroupRow> builder)
    {
        builder.ToTable("user_groups");

        builder.HasKey(x => new { x.UserId, x.GroupId });

        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.GroupId).HasColumnName("group_id");
    }
}