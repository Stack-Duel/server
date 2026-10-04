using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Games;

public interface IGameReadRepository
{
    Task<GameMode?> FindGameModeByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<GameMode?> FindGameModeByKeyAsync(string key, CancellationToken cancellationToken = default);

    Task<GameMode?> FindGameModeByIdIncludingInactiveAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Game?> FindGameByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Game?> FindGameByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<GameMode>> GetActiveGameModesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Game>> GetRunningGamesPastExpiryAsync(
        DateTime cutoffUtc,
        int maxBatchSize,
        CancellationToken cancellationToken = default
    );

    Task<PageResult<Game>> GetPendingGamesAsync(
        string? gameModeKey,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Every game regardless of participation, most recently created first — backs the admin
    /// games list. Unlike <see cref="GetPendingGamesAsync"/> this isn't scoped to Pending lobbies
    /// or any particular viewer.
    /// </summary>
    Task<PageResult<Game>> GetAdminGamesPagedAsync(
        GameStatus? status,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Games a user is currently part of that haven't finished yet — Pending (lobbies they're
    /// waiting in) or Running (games in progress). Backs the "my active games" list and the
    /// global active-game banner so a player can find their way back after navigating away.
    /// </summary>
    Task<IReadOnlyList<Game>> GetActiveGamesForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Completed games (Status == Completed) a user participated in, most recently ended first —
    /// backs profile game-mode stats and the recent-games list. Forfeited/expired games are still
    /// Completed (see Game.Complete/CompleteIfEveryoneStopped) so they're included; Cancelled
    /// lobbies (never started) are not.
    /// </summary>
    Task<IReadOnlyList<Game>> GetCompletedGamesForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecentSoloRushRunDto>> GetRecentCompletedSoloRushRunsAsync(
        Guid gameModeId,
        int timeLimitInSeconds,
        Guid excludeUserId,
        int take,
        CancellationToken cancellationToken = default
    );
}