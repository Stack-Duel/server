namespace StackDuel.Application.Jobs.Submissions;

public interface ISubmissionCleanupService
{
    Task RunAsync(CancellationToken cancellationToken = default);
}