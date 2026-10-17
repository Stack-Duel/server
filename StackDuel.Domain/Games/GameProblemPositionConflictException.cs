namespace StackDuel.Domain.Games;

public sealed class GameProblemPositionConflictException(Guid gameId, Exception innerException)
    : Exception(
        $"Game '{gameId}' already has a problem persisted at the position this save was about to create.",
        innerException
    )
{
    public Guid GameId { get; } = gameId;
}