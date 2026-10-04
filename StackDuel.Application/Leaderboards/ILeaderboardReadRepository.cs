using StackDuel.Application.Pagination;
using StackDuel.Domain.Games.Entities;

namespace StackDuel.Application.Leaderboards;

public interface ILeaderboardReadRepository
{
    /// <summary>
    /// Participants for the leaderboard identified by (gameModeId, timeLimitInSeconds), ordered
    /// by high score descending. Returns an empty page (not a 404) when no leaderboard exists yet
    /// for that combination — nobody has finished a game there, which isn't an error.
    /// </summary>
    Task<PageResult<LeaderboardParticipant>> GetRankingsAsync(
        Guid gameModeId,
        int timeLimitInSeconds,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken = default
    );

    Task<LeaderboardParticipant?> FindParticipantForUserAsync(
        Guid gameModeId,
        int timeLimitInSeconds,
        Guid userId,
        CancellationToken cancellationToken = default
    );
}