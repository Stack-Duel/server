namespace StackDuel.Domain.Games;

public interface IGameRoundSubmissionReadRepository
{
    /// <summary>
    /// Whether <paramref name="submissionId"/> is the most recent attempt this participant made on
    /// this round. Strictly the most recent: a superseded attempt must not stay eligible to complete
    /// the round, and a submission created outside the game's submit endpoint has no row here at all.
    /// </summary>
    Task<bool> IsLatestAttemptAsync(
        Guid participantId,
        Guid roundId,
        Guid submissionId,
        CancellationToken cancellationToken = default
    );
}