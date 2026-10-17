namespace StackDuel.Domain.Games;

public interface IGameRoundSubmissionReadRepository
{
    Task<bool> IsLatestAttemptAsync(
        Guid participantId,
        Guid roundId,
        Guid submissionId,
        CancellationToken cancellationToken = default
    );
}