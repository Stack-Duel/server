using StackDuel.Domain.Authorization.Rbac.Enums;

namespace StackDuel.Infrastructure.Persistence.Read.Entities;

internal sealed class RolePermissionRow
{
    public Guid RoleId { get; set; }

    public Guid PermissionId { get; set; }

    public DecisionEffect Effect { get; set; }
}