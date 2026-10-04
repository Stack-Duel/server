namespace StackDuel.Application.Notifications;

public interface ISubmissionNotificationService
{
    Task NotifySubmissionCompletedAsync(Guid userId, Guid submissionId, CancellationToken cancellationToken = default);
}