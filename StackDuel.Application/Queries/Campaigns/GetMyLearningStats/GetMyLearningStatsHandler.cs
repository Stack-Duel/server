using StackDuel.Application.Campaigns;
using StackDuel.Application.Campaigns.Dtos;
using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Campaigns.Enums;
using Ardalis.Result;

namespace StackDuel.Application.Queries.Campaigns.GetMyLearningStats;

internal sealed class GetMyLearningStatsHandler(ICampaignProgressReadRepository progressReadRepository)
    : IQueryHandler<GetMyLearningStatsQuery, UserLearningStatsDto>
{
    private const int XpPerLevel = 500;

    public async Task<Result<UserLearningStatsDto>> Handle(
        GetMyLearningStatsQuery request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<UnitType> completedUnitTypes = await progressReadRepository.GetCompletedUnitTypesAsync(
            request.UserId,
            cancellationToken
        );
        IReadOnlyList<DateTime> completionTimestamps = await progressReadRepository.GetCompletionTimestampsAsync(
            request.UserId,
            cancellationToken
        );
        IReadOnlyList<CampaignEnrollment> enrollments = await progressReadRepository.GetEnrollmentsForUserAsync(
            request.UserId,
            cancellationToken
        );

        int totalXp = completedUnitTypes.Sum(UnitTypeXp.For);
        int level = (totalXp / XpPerLevel) + 1;
        int xpIntoLevel = totalXp % XpPerLevel;

        int currentStreakDays = CalculateCurrentStreakDays(completionTimestamps);

        int completedCampaignsCount = enrollments.Count(e => e.Status == EnrollmentStatus.Completed);

        return Result.Success(
            new UserLearningStatsDto(
                totalXp,
                level,
                xpIntoLevel,
                XpPerLevel,
                currentStreakDays,
                completedCampaignsCount
            )
        );
    }

    private static int CalculateCurrentStreakDays(IReadOnlyList<DateTime> completionTimestamps)
    {
        HashSet<DateOnly> completedDates = [.. completionTimestamps.Select(t => DateOnly.FromDateTime(t.Date))];

        DateOnly cursor = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        if (!completedDates.Contains(cursor))
            cursor = cursor.AddDays(-1);

        int streakDays = 0;
        while (completedDates.Contains(cursor))
        {
            streakDays++;
            cursor = cursor.AddDays(-1);
        }

        return streakDays;
    }
}