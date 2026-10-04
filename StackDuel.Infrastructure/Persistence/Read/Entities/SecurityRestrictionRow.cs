using StackDuel.Domain.Authorization.Rbac.Enums;

namespace StackDuel.Infrastructure.Persistence.Read.Entities;

internal sealed class SecurityRestrictionRow
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public required string PermissionCode { get; set; }

    public DecisionEffect Effect { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}