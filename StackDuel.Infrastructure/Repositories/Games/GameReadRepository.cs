using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.Games;

internal sealed class GameReadRepository(StackDuelDbContext context) : IGameReadRepository
{
    public async Task<GameMode?> FindGameModeByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context
            .GameModes.Include(m => m.TimeOptions)
            .FirstOrDefaultAsync(m => m.Id == id && m.IsActive, cancellationToken);
    }

    public async Task<GameMode?> FindGameModeByIdIncludingInactiveAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        return await context.GameModes.FirstOrDefaultAsync(m => m.Id == id, cancellationToken);
    }

    public async Task<GameMode?> FindGameModeByKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        return await context
            .GameModes.Include(m => m.TimeOptions)
            .FirstOrDefaultAsync(m => m.Key == key && m.IsActive, cancellationToken);
    }

    public async Task<Game?> FindGameByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await context
            .Games.AsSplitQuery()
            .Include(g => g.Participants)
            .Include(g => g.Tracks)
                .ThenInclude(t => t.Languages)
            .Include(g => g.ProblemSequence)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);
    }

    public async Task<Game?> FindGameByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default)
    {
        string normalizedJoinCode = joinCode.Trim().ToUpperInvariant();

        return await context
            .Games.AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.Participants)
            .Include(g => g.Tracks)
            .FirstOrDefaultAsync(g => g.JoinCode == normalizedJoinCode, cancellationToken);
    }

    public async Task<IReadOnlyList<GameMode>> GetActiveGameModesAsync(CancellationToken cancellationToken = default)
    {
        return await context
            .GameModes.Include(m => m.TimeOptions)
            .Where(m => m.IsActive)
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Game>> GetRunningGamesPastExpiryAsync(
        DateTime cutoffUtc,
        int maxBatchSize,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .Games.AsNoTracking()
            .Include(g => g.Participants)
            .Where(g =>
                g.Status == GameStatus.Running
                && g.StartedAt != null
                && g.StartedAt.Value.AddSeconds(g.TimeLimitInSeconds) <= cutoffUtc
            )
            .OrderBy(g => g.StartedAt)
            .Take(maxBatchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<PageResult<Game>> GetPendingGamesAsync(
        string? gameModeKey,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken = default
    )
    {
        var query = context
            .Games.AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.Participants)
            .Include(g => g.Tracks)
            .Where(g => g.Status == GameStatus.Pending);

        if (!string.IsNullOrWhiteSpace(gameModeKey))
        {
            Guid? gameModeId = await context
                .GameModes.Where(m => m.Key == gameModeKey)
                .Select(m => (Guid?)m.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (gameModeId is null)
            {
                return new PageResult<Game>
                {
                    Results = [],
                    Total = 0,
                    Page = paginationRequest.Page,
                    Size = paginationRequest.Size,
                };
            }

            query = query.Where(g => g.GameModeId == gameModeId.Value);
        }

        int total = await query.CountAsync(cancellationToken);

        List<Game> results = await query
            .OrderByDescending(g => g.CreatedAt)
            .Skip((paginationRequest.Page - 1) * paginationRequest.Size)
            .Take(paginationRequest.Size)
            .ToListAsync(cancellationToken);

        return new PageResult<Game>
        {
            Results = results,
            Total = total,
            Page = paginationRequest.Page,
            Size = paginationRequest.Size,
        };
    }

    public async Task<PageResult<Game>> GetAdminGamesPagedAsync(
        GameStatus? status,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken = default
    )
    {
        var query = context.Games.AsNoTracking().Include(g => g.Participants).AsQueryable();

        if (status is GameStatus gameStatus)
            query = query.Where(g => g.Status == gameStatus);

        int total = await query.CountAsync(cancellationToken);

        List<Game> results = await query
            .OrderByDescending(g => g.CreatedAt)
            .Skip((paginationRequest.Page - 1) * paginationRequest.Size)
            .Take(paginationRequest.Size)
            .ToListAsync(cancellationToken);

        return new PageResult<Game>
        {
            Results = results,
            Total = total,
            Page = paginationRequest.Page,
            Size = paginationRequest.Size,
        };
    }

    public async Task<IReadOnlyList<Game>> GetActiveGamesForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .Games.AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.Participants)
            .Include(g => g.Tracks)
            .Where(g =>
                g.Participants.Any(p => p.UserId == userId)
                && (
                    g.Status == GameStatus.Pending
                    || (
                        g.Status == GameStatus.Running
                        && g.Participants.Any(self =>
                            self.UserId == userId && self.ForfeitedAt == null && self.FinishedAt == null
                        )
                    )
                )
            )
            .OrderByDescending(g => g.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Game>> GetCompletedGamesForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .Games.AsNoTracking()
            .Include(g => g.Participants)
            .Where(g => g.Status == GameStatus.Completed && g.Participants.Any(p => p.UserId == userId))
            .OrderByDescending(g => g.EndedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RecentSoloRushRunDto>> GetRecentCompletedSoloRushRunsAsync(
        Guid gameModeId,
        int timeLimitInSeconds,
        Guid excludeUserId,
        int take,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .Games.AsNoTracking()
            .Where(g =>
                g.GameModeId == gameModeId
                && g.TimeLimitInSeconds == timeLimitInSeconds
                && g.Status == GameStatus.Completed
                && g.Participants.Any(p => p.UserId != excludeUserId)
            )
            .OrderByDescending(g => g.EndedAt)
            .Take(take)
            .Select(g => new RecentSoloRushRunDto(
                g.Participants.Select(p => p.UserId).First(),
                g.Participants.Select(p => p.Score).First()
            ))
            .ToListAsync(cancellationToken);
    }
}