using Microsoft.EntityFrameworkCore;
using StackDuel.Application.Leaderboards;
using StackDuel.Application.Pagination;
using StackDuel.Domain.Games.Entities;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Leaderboards;

internal sealed class LeaderboardReadRepository(StackDuelDbContext context) : ILeaderboardReadRepository
{
    public async Task<PageResult<LeaderboardParticipant>> GetRankingsAsync(
        Guid gameModeId,
        int timeLimitInSeconds,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<LeaderboardParticipant> query =
            from participant in context.LeaderboardParticipants
            join leaderboard in context.Leaderboards
                on EF.Property<Guid>(participant, "leaderboard_id") equals leaderboard.Id
            where leaderboard.GameModeId == gameModeId && leaderboard.TimeLimitInSeconds == timeLimitInSeconds
            orderby participant.HighScore descending, participant.Id
            select participant;

        int total = await query.CountAsync(cancellationToken);

        List<LeaderboardParticipant> results = await query
            .Skip((paginationRequest.Page - 1) * paginationRequest.Size)
            .Take(paginationRequest.Size)
            .ToListAsync(cancellationToken);

        return new PageResult<LeaderboardParticipant>
        {
            Results = results,
            Total = total,
            Page = paginationRequest.Page,
            Size = paginationRequest.Size,
        };
    }

    public async Task<LeaderboardParticipant?> FindParticipantForUserAsync(
        Guid gameModeId,
        int timeLimitInSeconds,
        Guid userId,
        CancellationToken cancellationToken = default
    )
    {
        return await (
            from participant in context.LeaderboardParticipants
            join leaderboard in context.Leaderboards
                on EF.Property<Guid>(participant, "leaderboard_id") equals leaderboard.Id
            where
                leaderboard.GameModeId == gameModeId
                && leaderboard.TimeLimitInSeconds == timeLimitInSeconds
                && participant.UserId == userId
            select participant
        ).FirstOrDefaultAsync(cancellationToken);
    }
}