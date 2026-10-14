namespace StackDuel.Domain.Games;

/// <summary>
/// Thrown by <see cref="Entities.Game.AppendRound"/> when the problem selection strategy returned a
/// problem the game has already served. <c>game_rounds</c> is unique on (game_id, problem_id) — a
/// composite foreign key target that <c>game_round_submissions</c> depends on — so the duplicate is
/// rejected here, in memory, rather than surfacing as a unique-violation on save.
/// </summary>
/// <remarks>
/// Distinct from <see cref="GameProblemPositionConflictException"/>, which is a benign race between
/// two participants reaching the same new position and is retried. This one means selection itself
/// misbehaved — the excluded-problem set passed to the strategy failed to exclude something — so it
/// is a genuine fault rather than something to retry.
/// </remarks>
public sealed class DuplicateGameProblemException(Guid gameId, Guid problemId)
    : Exception($"Game '{gameId}' has already served problem '{problemId}'.")
{
    public Guid GameId { get; } = gameId;

    public Guid ProblemId { get; } = problemId;
}