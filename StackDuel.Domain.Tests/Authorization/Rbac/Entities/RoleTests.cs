using StackDuel.Domain.Authorization.Rbac.Entities;
using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.Authorization.Rbac.ValueObjects;

namespace StackDuel.Domain.Tests.Authorization.Rbac.Entities;

public class RoleTests
{
    private static Role CreateRole(string name = "admin") => Role.Create(new Name(name));

    [Fact]
    public void Create_SetsNameAndAssignsAnId()
    {
        Role role = CreateRole("moderator");

        Assert.Equal("moderator", role.Name.Value);
        Assert.NotEqual(Guid.Empty, role.Id.Value);
    }

    [Fact]
    public void Create_StartsWithNoPermissions()
    {
        Assert.Empty(CreateRole().Permissions);
    }

    [Fact]
    public void Create_GivesEachRoleADistinctId()
    {
        Role first = CreateRole();
        Role second = CreateRole();

        Assert.NotEqual(second.Id, first.Id);
    }

    [Fact]
    public void GrantPermission_AddsAnAllowEntry()
    {
        Role role = CreateRole();
        PermissionId permissionId = new(Guid.NewGuid());

        role.GrantPermission(permissionId);

        RolePermission granted = role.Permissions.Single();
        Assert.Equal(permissionId, granted.PermissionId);
        Assert.Equal(DecisionEffect.Allow, granted.Effect);
    }

    [Fact]
    public void DenyPermission_AddsADenyEntry()
    {
        Role role = CreateRole();
        PermissionId permissionId = new(Guid.NewGuid());

        role.DenyPermission(permissionId);

        Assert.Equal(DecisionEffect.Deny, role.Permissions.Single().Effect);
    }

    [Fact]
    public void GrantPermission_DifferentPermissions_AreKeptSeparately()
    {
        Role role = CreateRole();

        role.GrantPermission(new PermissionId(Guid.NewGuid()));
        role.GrantPermission(new PermissionId(Guid.NewGuid()));

        Assert.Equal(2, role.Permissions.Count);
    }

    [Fact]
    public void DenyPermission_AfterGrant_FlipsTheEffectInPlaceRatherThanDuplicating()
    {
        Role role = CreateRole();
        PermissionId permissionId = new(Guid.NewGuid());

        role.GrantPermission(permissionId);
        role.DenyPermission(permissionId);

        Assert.Single(role.Permissions);
        Assert.Equal(DecisionEffect.Deny, role.Permissions.Single().Effect);
    }

    [Fact]
    public void GrantPermission_AfterDeny_FlipsTheEffectBack()
    {
        Role role = CreateRole();
        PermissionId permissionId = new(Guid.NewGuid());

        role.DenyPermission(permissionId);
        role.GrantPermission(permissionId);

        Assert.Single(role.Permissions);
        Assert.Equal(DecisionEffect.Allow, role.Permissions.Single().Effect);
    }

    [Fact]
    public void GrantPermission_SamePermissionTwice_DoesNotDuplicate()
    {
        Role role = CreateRole();
        PermissionId permissionId = new(Guid.NewGuid());

        role.GrantPermission(permissionId);
        role.GrantPermission(permissionId);

        Assert.Single(role.Permissions);
    }
}

public class PermissionTests
{
    [Fact]
    public void Create_SetsCodeDescriptionAndAssignsAnId()
    {
        Permission permission = Permission.Create(new PermissionCode("submission:create"), "Create a submission.");

        Assert.Equal("submission:create", permission.Code.Value);
        Assert.Equal("Create a submission.", permission.Description);
        Assert.NotEqual(Guid.Empty, permission.Id.Value);
    }

    [Fact]
    public void Create_GivesEachPermissionADistinctId()
    {
        PermissionCode code = new("submission:create");

        Permission first = Permission.Create(code, "desc");
        Permission second = Permission.Create(code, "desc");

        Assert.NotEqual(second.Id, first.Id);
    }
}

public class GroupTests
{
    private static Group CreateGroup(string name = "admin") => Group.Create(new Name(name));

    [Fact]
    public void Create_SetsNameAndAssignsAnId()
    {
        Group group = CreateGroup("default-user");

        Assert.Equal("default-user", group.Name.Value);
        Assert.NotEqual(Guid.Empty, group.Id.Value);
    }

    [Fact]
    public void Create_StartsWithNoRoleGrants()
    {
        Assert.Empty(CreateGroup().RoleGrants);
    }

    [Fact]
    public void GrantRole_AddsTheRole()
    {
        Group group = CreateGroup();
        Role role = Role.Create(new Name("admin"));

        group.GrantRole(role);

        Assert.Same(role, group.RoleGrants.Single());
    }

    [Fact]
    public void GrantRole_SameRoleTwice_IsStoredOnce()
    {
        Group group = CreateGroup();
        Role role = Role.Create(new Name("admin"));

        group.GrantRole(role);
        group.GrantRole(role);

        Assert.Single(group.RoleGrants);
    }

    [Fact]
    public void GrantRole_DistinctRoles_AreBothKept()
    {
        Group group = CreateGroup();

        group.GrantRole(Role.Create(new Name("admin")));
        group.GrantRole(Role.Create(new Name("moderator")));

        Assert.Equal(2, group.RoleGrants.Count);
    }

    [Fact]
    public void GrantRole_Null_Throws()
    {
        Group group = CreateGroup();

        Assert.Throws<ArgumentNullException>(() => group.GrantRole(null!));
    }

    [Fact]
    public void RevokeRole_RemovesAGrantedRole()
    {
        Group group = CreateGroup();
        Role role = Role.Create(new Name("admin"));
        group.GrantRole(role);

        group.RevokeRole(role);

        Assert.Empty(group.RoleGrants);
    }

    [Fact]
    public void RevokeRole_NotGranted_IsANoOp()
    {
        Group group = CreateGroup();
        group.GrantRole(Role.Create(new Name("admin")));

        group.RevokeRole(Role.Create(new Name("moderator")));

        Assert.Single(group.RoleGrants);
    }

    [Fact]
    public void RoleGrants_IsASnapshotThatDoesNotTrackLaterGrants()
    {
        Group group = CreateGroup();
        IReadOnlyCollection<Role> snapshot = group.RoleGrants;

        group.GrantRole(Role.Create(new Name("admin")));

        Assert.Empty(snapshot);
    }
}