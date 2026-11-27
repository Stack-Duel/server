using Ardalis.Result;
using StackDuel.Application.DailyChallenges;
using StackDuel.Application.Dashboard.Dtos;
using StackDuel.Application.Games;
using StackDuel.Application.Queries.DailyChallenges;
using StackDuel.Application.Queries.Users;
using StackDuel.Application.Submissions;

namespace StackDuel.Application.Queries.Dashboard.GetMyDashboardStats;

internal sealed class GetMyDashboardStatsHandler(
    IGameReadRepository gameReadRepository,
    IDailyChallengeRepository dailyChallengeRepository,
    ISubmissionReadRepository submissionReadRepository
) : IQueryHandler<GetMyDashboardStatsQuery, UserDashboardStatsDto>
{
    public async Task<Result<UserDashboardStatsDto>> Handle(
        GetMyDashboardStatsQuery request,
        CancellationToken cancellationToken
    )
    {
        GameModeStatsResult gameModeStats = await GameModeStatsBuilder.BuildAsync(
            gameReadRepository,
            request.UserId,
            cancellationToken
        );

        DailyChallengeStreakResult streaks = await DailyChallengeStreakBuilder.BuildAsync(
            dailyChallengeRepository,
            submissionReadRepository,
            request.UserId,
            DateOnly.FromDateTime(DateTime.UtcNow),
            cancellationToken
        );

        int wins = gameModeStats.Stats.Sum(stat => stat.Wins);

        return Result.Success(new UserDashboardStatsDto(wins, streaks.CurrentStreak, streaks.LongestStreak));
    }
}