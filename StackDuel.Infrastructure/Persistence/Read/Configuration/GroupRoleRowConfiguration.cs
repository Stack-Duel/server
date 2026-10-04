using StackDuel.Infrastructure.Persistence.Read.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace StackDuel.Infrastructure.Persistence.Read.Configuration;

internal sealed class GroupRoleRowConfiguration : IEntityTypeConfiguration<GroupRoleRow>
{
    public void Configure(EntityTypeBuilder<GroupRoleRow> builder)
    {
        builder.ToTable("group_roles");

        builder.HasKey(x => new { x.GroupId, x.RoleId });

        builder.Property(x => x.GroupId).HasColumnName("group_id");
        builder.Property(x => x.RoleId).HasColumnName("role_id");
    }
}