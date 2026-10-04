using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.Authorization.Rbac.ValueObjects;
using StackDuel.Domain.Authorization.Security.ValueObjects;

namespace StackDuel.Domain.Authorization.Security.Entities;

public sealed class SecurityRestriction
{
    internal SecurityRestriction(
        Guid id,
        UserId userId,
        PermissionCode permissionCode,
        DecisionEffect effect,
        DateTimeOffset expiresAt,
        Reason reason,
        DetectionEventId detectionEventId
    )
    {
        if (userId.Value == Guid.Empty)
            throw new ArgumentException("UserId must not be empty");
        if (string.IsNullOrWhiteSpace(permissionCode.Value))
            throw new ArgumentException("PermissionCode required.", nameof(permissionCode));
        if (expiresAt <= DateTimeOffset.UtcNow)
            throw new ArgumentException("ExpiresAt must be in the future.", nameof(expiresAt));
        if (reason.Value is null)
            throw new ArgumentException("Reason required.", nameof(reason));

        Id = id;
        UserId = userId;
        PermissionCode = permissionCode;
        Effect = effect;
        ExpiresAt = expiresAt;
        Reason = reason;
        DetectionEventId = detectionEventId;
    }

    public static SecurityRestriction CreateDenyTemporary(
        Guid id,
        UserId userId,
        PermissionCode permissionCode,
        DateTimeOffset expiresAt,
        Reason reason,
        DetectionEventId detectionEventId
    ) => new(id, userId, permissionCode, DecisionEffect.Deny, expiresAt, reason, detectionEventId);

    public bool IsActiveAt(DateTimeOffset now) => now <= ExpiresAt;

    public Guid Id { get; }

    public UserId UserId { get; }

    public PermissionCode PermissionCode { get; }

    public DecisionEffect Effect { get; }

    public DateTimeOffset ExpiresAt { get; }

    public Reason Reason { get; }

    public DetectionEventId DetectionEventId { get; }
}