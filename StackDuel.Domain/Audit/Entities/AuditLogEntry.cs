namespace StackDuel.Domain.Audit.Entities;

public sealed class AuditLogEntry
{
    private AuditLogEntry() { }

    public static AuditLogEntry Create(
        Guid? actorUserId,
        string actorUsername,
        string action,
        string? targetType,
        string? targetId,
        string? detailsJson
    )
    {
        if (string.IsNullOrWhiteSpace(actorUsername))
            throw new ArgumentException("Actor username is required", nameof(actorUsername));
        if (string.IsNullOrWhiteSpace(action))
            throw new ArgumentException("Action is required", nameof(action));

        return new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            ActorUsername = actorUsername,
            Action = action,
            TargetType = targetType,
            TargetId = targetId,
            DetailsJson = detailsJson,
            CreatedAt = DateTime.UtcNow,
        };
    }

    public Guid Id { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public string ActorUsername { get; private set; } = string.Empty;

    public string Action { get; private set; } = string.Empty;

    public string? TargetType { get; private set; }

    public string? TargetId { get; private set; }

    public string? DetailsJson { get; private set; }

    public DateTime CreatedAt { get; private set; }
}