namespace StackDuel.Domain.Games;

/// <summary>
/// Thrown by <see cref="IGameWriteRepository.SaveChangesAsync"/> when two participants raced to be
/// the first to reach the same new position in a game's shared problem sequence: both built a
/// GameProblem for that position in memory, but only one save can win the
/// (game_id, position) unique index. The loser should re-fetch the game (now picking up the
/// winner's already-persisted problem for that position via Game.ProblemIdAtPosition) and retry
/// once rather than treating this as a hard failure.
/// </summary>
public sealed class GameProblemPositionConflictException(Guid gameId, Exception innerException)
    : Exception(
        $"Game '{gameId}' already has a problem persisted at the position this save was about to create.",
        innerException
    )
{
    public Guid GameId { get; } = gameId;
}