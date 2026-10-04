using Microsoft.EntityFrameworkCore;
using StackDuel.Infrastructure.Persistence.Read.Configuration;
using StackDuel.Infrastructure.Persistence.Read.Entities;

namespace StackDuel.Infrastructure.Persistence.Read;

internal sealed class StackDuelReadDbContext(DbContextOptions<StackDuelReadDbContext> options) : DbContext(options)
{
    public DbSet<UserRow> Users { get; init; }

    public DbSet<GroupRow> Groups { get; init; }

    public DbSet<RoleRow> Roles { get; init; }

    public DbSet<PermissionRow> Permissions { get; init; }

    public DbSet<RolePermissionRow> RolePermissions { get; init; }

    public DbSet<SecurityRestrictionRow> SecurityRestrictions { get; init; }

    public DbSet<UserGroupRow> UserGroups { get; init; }

    public DbSet<GroupRoleRow> GroupRoles { get; init; }

    public DbSet<UserLanguagePreferenceRow> UserLanguagePreferences { get; init; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new UserRowConfiguration());
        modelBuilder.ApplyConfiguration(new GroupRowConfiguration());
        modelBuilder.ApplyConfiguration(new RoleRowConfiguration());
        modelBuilder.ApplyConfiguration(new PermissionRowConfiguration());
        modelBuilder.ApplyConfiguration(new RolePermissionRowConfiguration());
        modelBuilder.ApplyConfiguration(new SecurityRestrictionRowConfiguration());
        modelBuilder.ApplyConfiguration(new UserGroupRowConfiguration());
        modelBuilder.ApplyConfiguration(new GroupRoleRowConfiguration());
        modelBuilder.ApplyConfiguration(new UserLanguagePreferenceRowConfiguration());
    }
}