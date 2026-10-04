using StackDuel.Application.Audit;
using StackDuel.Domain.Feedback.Enums;

namespace StackDuel.Application.Commands.Feedback.UpdateFeedbackStatus;

internal sealed record UpdateFeedbackStatusCommand(Guid FeedbackId, FeedbackStatus Status, string? AdminNote)
    : ICommand,
        IAuditableCommand
{
    public string AuditAction => "feedback.status-updated";
    public string? AuditTargetType => "feedback";
    public string? AuditTargetId => FeedbackId.ToString();
    public object? AuditDetails => new { Status, AdminNote };
}