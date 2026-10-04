using StackDuel.Application.Notifications;
using Microsoft.AspNetCore.SignalR;

namespace StackDuel.Api.Hubs;

internal sealed class SignalRSubmissionNotificationService(IHubContext<SubmissionHub> hubContext)
    : ISubmissionNotificationService
{
    public const string SubmissionCompletedMethod = "SubmissionCompleted";

    public Task NotifySubmissionCompletedAsync(
        Guid userId,
        Guid submissionId,
        CancellationToken cancellationToken = default
    )
    {
        var payload = new { submissionId };
        return hubContext
            .Clients.Group(SubmissionHub.UserGroupName(userId))
            .SendAsync(SubmissionCompletedMethod, payload, cancellationToken);
    }
}