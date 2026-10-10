using StackDuel.Domain.Authorization.Rbac.Entities;
using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.Authorization.Rbac.ValueObjects;

namespace StackDuel.Domain.Tests.Authorization.Rbac.Entities;

public class RoleTests
{
    private static Role CreateRole(string name = "admin") => Role.Create(new Name(name));

    [Test]
    public void Create_SetsNameAndAssignsAnId()
    {
        Role role = CreateRole("moderator");

        Assert.Multiple(() =>
        {
            Assert.That(role.Name.Value, Is.EqualTo("moderator"));
            Assert.That(role.Id.Value, Is.Not.EqualTo(Guid.Empty));
        });
    }

    [Test]
    public void Create_StartsWithNoPermissions()
    {
        Assert.That(CreateRole().Permissions, Is.Empty);
    }

    [Test]
    public void Create_GivesEachRoleADistinctId()
    {
        Assert.That(CreateRole().Id, Is.Not.EqualTo(CreateRole().Id));
    }

    [Test]
    public void GrantPermission_AddsAnAllowEntry()
    {
        Role role = CreateRole();
        PermissionId permissionId = new(Guid.NewGuid());

        role.GrantPermission(permissionId);

        RolePermission granted = role.Permissions.Single();
        Assert.Multiple(() =>
        {
            Assert.That(granted.PermissionId, Is.EqualTo(permissionId));
            Assert.That(granted.Effect, Is.EqualTo(DecisionEffect.Allow));
        });
    }

    [Test]
    public void DenyPermission_AddsADenyEntry()
    {
        Role role = CreateRole();
        PermissionId permissionId = new(Guid.NewGuid());

        role.DenyPermission(permissionId);

        Assert.That(role.Permissions.Single().Effect, Is.EqualTo(DecisionEffect.Deny));
    }

    [Test]
    public void GrantPermission_DifferentPermissions_AreKeptSeparately()
    {
        Role role = CreateRole();

        role.GrantPermission(new PermissionId(Guid.NewGuid()));
        role.GrantPermission(new PermissionId(Guid.NewGuid()));

        Assert.That(role.Permissions, Has.Count.EqualTo(2));
    }

    [Test]
    public void DenyPermission_AfterGrant_FlipsTheEffectInPlaceRatherThanDuplicating()
    {
        Role role = CreateRole();
        PermissionId permissionId = new(Guid.NewGuid());

        role.GrantPermission(permissionId);
        role.DenyPermission(permissionId);

        Assert.Multiple(() =>
        {
            Assert.That(role.Permissions, Has.Count.EqualTo(1));
            Assert.That(role.Permissions.Single().Effect, Is.EqualTo(DecisionEffect.Deny));
        });
    }

    [Test]
    public void GrantPermission_AfterDeny_FlipsTheEffectBack()
    {
        Role role = CreateRole();
        PermissionId permissionId = new(Guid.NewGuid());

        role.DenyPermission(permissionId);
        role.GrantPermission(permissionId);

        Assert.Multiple(() =>
        {
            Assert.That(role.Permissions, Has.Count.EqualTo(1));
            Assert.That(role.Permissions.Single().Effect, Is.EqualTo(DecisionEffect.Allow));
        });
    }

    [Test]
    public void GrantPermission_SamePermissionTwice_DoesNotDuplicate()
    {
        Role role = CreateRole();
        PermissionId permissionId = new(Guid.NewGuid());

        role.GrantPermission(permissionId);
        role.GrantPermission(permissionId);

        Assert.That(role.Permissions, Has.Count.EqualTo(1));
    }
}

public class PermissionTests
{
    [Test]
    public void Create_SetsCodeDescriptionAndAssignsAnId()
    {
        Permission permission = Permission.Create(new PermissionCode("submission:create"), "Create a submission.");

        Assert.Multiple(() =>
        {
            Assert.That(permission.Code.Value, Is.EqualTo("submission:create"));
            Assert.That(permission.Description, Is.EqualTo("Create a submission."));
            Assert.That(permission.Id.Value, Is.Not.EqualTo(Guid.Empty));
        });
    }

    [Test]
    public void Create_GivesEachPermissionADistinctId()
    {
        PermissionCode code = new("submission:create");

        Assert.That(Permission.Create(code, "desc").Id, Is.Not.EqualTo(Permission.Create(code, "desc").Id));
    }
}

public class GroupTests
{
    private static Group CreateGroup(string name = "admin") => Group.Create(new Name(name));

    [Test]
    public void Create_SetsNameAndAssignsAnId()
    {
        Group group = CreateGroup("default-user");

        Assert.Multiple(() =>
        {
            Assert.That(group.Name.Value, Is.EqualTo("default-user"));
            Assert.That(group.Id.Value, Is.Not.EqualTo(Guid.Empty));
        });
    }

    [Test]
    public void Create_StartsWithNoRoleGrants()
    {
        Assert.That(CreateGroup().RoleGrants, Is.Empty);
    }

    [Test]
    public void GrantRole_AddsTheRole()
    {
        Group group = CreateGroup();
        Role role = Role.Create(new Name("admin"));

        group.GrantRole(role);

        Assert.That(group.RoleGrants.Single(), Is.SameAs(role));
    }

    [Test]
    public void GrantRole_SameRoleTwice_IsStoredOnce()
    {
        Group group = CreateGroup();
        Role role = Role.Create(new Name("admin"));

        group.GrantRole(role);
        group.GrantRole(role);

        Assert.That(group.RoleGrants, Has.Count.EqualTo(1));
    }

    [Test]
    public void GrantRole_DistinctRoles_AreBothKept()
    {
        Group group = CreateGroup();

        group.GrantRole(Role.Create(new Name("admin")));
        group.GrantRole(Role.Create(new Name("moderator")));

        Assert.That(group.RoleGrants, Has.Count.EqualTo(2));
    }

    [Test]
    public void GrantRole_Null_Throws()
    {
        Group group = CreateGroup();

        Assert.Throws<ArgumentNullException>(() => group.GrantRole(null!));
    }

    [Test]
    public void RevokeRole_RemovesAGrantedRole()
    {
        Group group = CreateGroup();
        Role role = Role.Create(new Name("admin"));
        group.GrantRole(role);

        group.RevokeRole(role);

        Assert.That(group.RoleGrants, Is.Empty);
    }

    [Test]
    public void RevokeRole_NotGranted_IsANoOp()
    {
        Group group = CreateGroup();
        group.GrantRole(Role.Create(new Name("admin")));

        group.RevokeRole(Role.Create(new Name("moderator")));

        Assert.That(group.RoleGrants, Has.Count.EqualTo(1));
    }

    [Test]
    public void RoleGrants_IsASnapshotThatDoesNotTrackLaterGrants()
    {
        Group group = CreateGroup();
        IReadOnlyCollection<Role> snapshot = group.RoleGrants;

        group.GrantRole(Role.Create(new Name("admin")));

        Assert.That(snapshot, Is.Empty);
    }
}