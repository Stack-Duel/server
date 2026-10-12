using Microsoft.Extensions.Logging;
using StackDuel.Application.Events;
using StackDuel.Domain.Submissions;
using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Submissions;

internal sealed partial class SubmissionFailureService(
    ISubmissionWriteRepository submissionWriteRepository,
    IDomainEventDispatcher domainEventDispatcher,
    ILogger<SubmissionFailureService> logger
) : ISubmissionFailureService
{
    public async Task MarkFailedAsync(Guid submissionId, CancellationToken cancellationToken = default)
    {
        try
        {
            Submission? submission = await submissionWriteRepository.FindByIdAsync(submissionId, cancellationToken);
            if (submission is null || IsTerminal(submission.Status))
                return;

            submission.Fail();
            await submissionWriteRepository.UpdateAsync(submission, cancellationToken);
            await AnnounceTerminalStateRegardlessOfCancellationAsync(submission);
        }
        catch (Exception ex)
        {
            LogMarkFailedFailed(ex, submissionId);
        }
    }

    private static bool IsTerminal(SubmissionStatus status) =>
        status is SubmissionStatus.Accepted or SubmissionStatus.WrongAnswer;

    /// <summary>
    /// Dispatched outside the caller's token: the terminal state is already persisted, so dropping
    /// these events on cancellation would leave clients waiting on a submission nothing announces.
    /// </summary>
    private Task AnnounceTerminalStateRegardlessOfCancellationAsync(Submission submission) =>
        domainEventDispatcher.DispatchAsync(submission.PopDomainEvents(), CancellationToken.None);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to mark submission {SubmissionId} as failed; clients may keep waiting on it until the next sweep."
    )]
    private partial void LogMarkFailedFailed(Exception ex, Guid submissionId);
}
