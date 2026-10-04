using StackDuel.Domain.Authorization.Rbac.ValueObjects;

namespace StackDuel.Domain.Authorization.Rbac.Entities;

public sealed class Group
{
    internal Group(GroupId id, Name name)
    {
        if (name is null)
            throw new ArgumentException("Group name required", nameof(name));
        Id = id;
        Name = name;
    }

    public static Group Create(Name name) => new(new GroupId(Guid.NewGuid()), name);

    public void GrantRole(Role role)
    {
        ArgumentNullException.ThrowIfNull(role);
        _roleGrants.Add(role);
    }

    public void RevokeRole(Role role) => _roleGrants.Remove(role);

    public GroupId Id { get; }

    public Name Name { get; }

    public IReadOnlyCollection<Role> RoleGrants => _roleGrants.ToList().AsReadOnly();

    private readonly HashSet<Role> _roleGrants = [];
}