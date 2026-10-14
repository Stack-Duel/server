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

    Task<Guid?> FindRoundIdByGameAndProblemAsync(
        Guid gameId,
        Guid problemId,
        CancellationToken cancellationToken = default
    );
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

    Task<PageResult<Game>> GetAdminGamesPagedAsync(
        GameStatus? status,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<Game>> GetActiveGamesForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Game>> GetCompletedGamesForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecentSoloRushRunDto>> GetRecentCompletedSoloRushRunsAsync(
        Guid gameModeId,
        int timeLimitInSeconds,
        Guid excludeUserId,
        int take,
        CancellationToken cancellationToken = default
    );
}