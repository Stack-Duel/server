namespace StackDuel.Application.Audit.Dtos;

public sealed record AuditLogEntryDto(
    Guid Id,
    Guid? ActorUserId,
    string ActorUsername,
    string Action,
    string? TargetType,
    string? TargetId,
    string? DetailsJson,
    DateTime CreatedAt
);