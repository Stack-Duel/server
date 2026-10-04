using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.Authorization.Rbac.ValueObjects;

namespace StackDuel.Domain.Authorization.Rbac.Entities;

public sealed class Role
{
    internal Role(RoleId id, Name name)
    {
        Id = id;
        Name = name;
    }

    public static Role Create(Name name) => new(new RoleId(Guid.NewGuid()), name);

    public void GrantPermission(PermissionId permissionId) => Upsert(permissionId, DecisionEffect.Allow);

    public void DenyPermission(PermissionId permissionId) => Upsert(permissionId, DecisionEffect.Deny);

    private void Upsert(PermissionId permissionId, DecisionEffect effect)
    {
        var existingPermission = _permissions.FirstOrDefault(p => p.PermissionId == permissionId);
        if (existingPermission != null)
        {
            existingPermission.UpdateEffect(effect);
        }
        else
        {
            var newPermission = new RolePermission(permissionId, effect);
            _permissions.Add(newPermission);
        }
    }

    public RoleId Id { get; }

    public Name Name { get; }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    private readonly List<RolePermission> _permissions = [];
}

public sealed class Permission
{
    internal Permission(PermissionId id, PermissionCode code, string description)
    {
        Id = id;
        Code = code;
        Description = description;
    }

    public static Permission Create(PermissionCode code, string description) =>
        new(new PermissionId(Guid.NewGuid()), code, description);

    public PermissionId Id { get; }

    public PermissionCode Code { get; }

    public string Description { get; }
}

public sealed class RolePermission
{
    public PermissionId PermissionId { get; }

    public DecisionEffect Effect { get; private set; }

    internal RolePermission(PermissionId permissionId, DecisionEffect effect)
    {
        PermissionId = permissionId;
        Effect = effect;
    }

    internal void UpdateEffect(DecisionEffect effect) => Effect = effect;
}