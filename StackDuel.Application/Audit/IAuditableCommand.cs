namespace StackDuel.Application.Audit;

public interface IAuditableCommand
{
    string AuditAction { get; }

    string? AuditTargetType { get; }

    string? AuditTargetId { get; }

    object? AuditDetails { get; }
}