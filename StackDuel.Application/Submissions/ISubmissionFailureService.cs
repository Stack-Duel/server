namespace StackDuel.Application.Submissions;

public interface ISubmissionFailureService
{
    /// <summary>
    /// Transitions a non-terminal submission to a terminal failed state and dispatches its domain
    /// events, so clients stop waiting on it. No-op when the submission is missing or already
    /// terminal.
    /// </summary>
    Task MarkFailedAsync(Guid submissionId, CancellationToken cancellationToken = default);
}