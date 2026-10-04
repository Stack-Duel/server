using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using StackDuel.Domain.Authorization.Rbac.Entities;
using StackDuel.Domain.Authorization.Rbac.ValueObjects;

namespace StackDuel.Infrastructure.Persistence.Configuration.Authorization;

internal class RolesConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(r => r.Value, v => new RoleId(v))
            .ValueGeneratedNever();

        builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasConversion(n => n.Value, v => new Name(v))
            .HasMaxLength(Name.MaxLength)
            .IsRequired();

        builder.Navigation(r => r.Permissions).HasField("_permissions").UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(r => r.Permissions).WithOne().HasForeignKey("role_id").OnDelete(DeleteBehavior.Cascade);
    }
}

internal class PermissionsConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("permissions");

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(p => p.Value, v => new PermissionId(v))
            .ValueGeneratedNever();

        builder
            .Property(x => x.Code)
            .HasColumnName("code")
            .HasConversion(c => c.Value, v => new PermissionCode(v))
            .IsRequired();

        builder.HasIndex(x => x.Code).IsUnique();

        builder.Property(x => x.Description).HasColumnName("description").HasMaxLength(500).IsRequired();
    }
}

internal class RolePermissionsConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("role_permissions");

        builder
            .Property(x => x.PermissionId)
            .HasColumnName("permission_id")
            .HasConversion(p => p.Value, v => new PermissionId(v))
            .IsRequired();

        builder
            .Property<RoleId>("role_id")
            .HasColumnName("role_id")
            .HasConversion(r => r.Value, v => new RoleId(v))
            .IsRequired();

        builder.HasKey(nameof(RolePermission.PermissionId), "role_id");

        builder.Property(x => x.Effect).HasColumnName("effect").HasConversion<string>().IsRequired();

        builder.HasOne<Permission>().WithMany().HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PermissionId).HasDatabaseName("IX_role_permissions_permission_id");

        builder.HasIndex("role_id").HasDatabaseName("IX_role_permissions_role_id");
    }
}

internal class GroupsConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups");

        builder.HasKey(x => x.Id);

        builder
            .Property(x => x.Id)
            .HasColumnName("id")
            .HasConversion(g => g.Value, v => new GroupId(v))
            .ValueGeneratedNever();

        builder
            .Property(x => x.Name)
            .HasColumnName("name")
            .HasConversion(n => n.Value, v => new Name(v))
            .HasMaxLength(Name.MaxLength)
            .IsRequired();

        builder
            .HasMany(g => g.RoleGrants)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "group_roles",
                right => right.HasOne<Role>().WithMany().HasForeignKey("role_id").OnDelete(DeleteBehavior.Cascade),
                left => left.HasOne<Group>().WithMany().HasForeignKey("group_id").OnDelete(DeleteBehavior.Cascade),
                join =>
                {
                    join.ToTable("group_roles");

                    join.Property<GroupId>("group_id")
                        .HasColumnName("group_id")
                        .HasConversion(g => g.Value, v => new GroupId(v));

                    join.Property<RoleId>("role_id")
                        .HasColumnName("role_id")
                        .HasConversion(r => r.Value, v => new RoleId(v));

                    join.HasKey("group_id", "role_id");
                    join.HasIndex("role_id").HasDatabaseName("IX_group_roles_role_id");
                }
            );

        builder.Navigation(g => g.RoleGrants).HasField("_roleGrants").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}