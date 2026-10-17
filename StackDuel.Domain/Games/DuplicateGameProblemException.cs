namespace StackDuel.Domain.Games;

public sealed class DuplicateGameProblemException(Guid gameId, Guid problemId)
    : Exception($"Game '{gameId}' has already served problem '{problemId}'.")
{
    public Guid GameId { get; } = gameId;

    public Guid ProblemId { get; } = problemId;
}