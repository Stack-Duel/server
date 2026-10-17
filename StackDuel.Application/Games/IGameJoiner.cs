using Ardalis.Result;

namespace StackDuel.Application.Games;

/// <summary>
/// Seats a user in a pending game: loads the game for writing, enforces the mode's access
/// permission and player limit, adds the participant and persists the game. Shared by joining a
/// game directly and by joining through an invite code, which resolves the game from the code
/// and then needs the same work done rather than dispatching a second command.
/// </summary>
public interface IGameJoiner
{
    Task<Result> JoinAsync(Guid gameId, Guid requestedByUserId, CancellationToken cancellationToken = default);
}