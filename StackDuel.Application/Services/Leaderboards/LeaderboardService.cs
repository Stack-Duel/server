using StackDuel.Application.Leaderboards.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Queries.Leaderboards.GetLeaderboard;
using StackDuel.Application.Queries.Leaderboards.GetMyLeaderboardEntry;
using Ardalis.Result;
using MediatR;

namespace StackDuel.Application.Services.Leaderboards;

public interface ILeaderboardService
{
    Task<Result<PageResult<LeaderboardEntryDto>>> GetLeaderboardAsync(
        string gameModeKey,
        int timeLimitInSeconds,
        PaginationRequest paginationRequest,
        Guid? requestedByUserId,
        CancellationToken cancellationToken
    );

    Task<Result<MyLeaderboardEntryDto>> GetMyEntryAsync(
        string gameModeKey,
        int timeLimitInSeconds,
        Guid userId,
        CancellationToken cancellationToken
    );
}

internal sealed class LeaderboardService(IMediator mediator) : ILeaderboardService
{
    public async Task<Result<PageResult<LeaderboardEntryDto>>> GetLeaderboardAsync(
        string gameModeKey,
        int timeLimitInSeconds,
        PaginationRequest paginationRequest,
        Guid? requestedByUserId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(
            new GetLeaderboardQuery(gameModeKey, timeLimitInSeconds, paginationRequest, requestedByUserId),
            cancellationToken
        );
    }

    public async Task<Result<MyLeaderboardEntryDto>> GetMyEntryAsync(
        string gameModeKey,
        int timeLimitInSeconds,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        return await mediator.Send(
            new GetMyLeaderboardEntryQuery(gameModeKey, timeLimitInSeconds, userId),
            cancellationToken
        );
    }
}