using Ardalis.Result;
using StackDuel.Application.DailyChallenges;
using StackDuel.Application.DailyChallenges.Dtos;
using StackDuel.Application.Problems;
using StackDuel.Application.Submissions;
using StackDuel.Domain.DailyChallenges.Entities;
using StackDuel.Domain.Problems.Entities;

namespace StackDuel.Application.Queries.DailyChallenges.GetTodaysDailyChallenge;

internal sealed class GetTodaysDailyChallengeHandler(
    IDailyChallengeRepository dailyChallengeRepository,
    IProblemReadRepository problemReadRepository,
    ISubmissionReadRepository submissionReadRepository
) : IQueryHandler<GetTodaysDailyChallengeQuery, TodaysDailyChallengeDto>
{
    public async Task<Result<TodaysDailyChallengeDto>> Handle(
        GetTodaysDailyChallengeQuery request,
        CancellationToken cancellationToken
    )
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        DailyChallenge? todaysChallenge = await dailyChallengeRepository.FindByDateAsync(today, cancellationToken);
        if (todaysChallenge is null)
            return Result<TodaysDailyChallengeDto>.NotFound();

        Problem? problem = await problemReadRepository.FindByIdAsync(todaysChallenge.ProblemId, cancellationToken);
        if (problem is null)
            return Result<TodaysDailyChallengeDto>.NotFound();

        DailyChallengeStreakResult streaks = await DailyChallengeStreakBuilder.BuildAsync(
            dailyChallengeRepository,
            submissionReadRepository,
            request.UserId,
            today,
            cancellationToken
        );

        return Result.Success(
            new TodaysDailyChallengeDto(
                problem.Id,
                problem.Slug.Value,
                problem.Title.Value,
                problem.Difficulty.Tier,
                streaks.SolvedProblemIds.Contains(todaysChallenge.ProblemId),
                streaks.CurrentStreak,
                streaks.LongestStreak
            )
        );
    }
}