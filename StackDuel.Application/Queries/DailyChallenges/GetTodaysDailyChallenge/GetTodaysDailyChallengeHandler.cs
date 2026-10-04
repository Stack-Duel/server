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

        IReadOnlyList<DailyChallenge> challengesNewestFirst =
        [
            .. (await dailyChallengeRepository.GetAllOrderedByDateDescendingAsync(cancellationToken)).Where(c =>
                c.ChallengeDate <= today
            ),
        ];

        IReadOnlyList<Guid> problemIds = [.. challengesNewestFirst.Select(c => c.ProblemId).Distinct()];
        IReadOnlyList<Guid> acceptedProblemIds = await submissionReadRepository.GetAcceptedProblemIdsForUserAsync(
            request.UserId,
            problemIds,
            cancellationToken
        );
        HashSet<Guid> solved = [.. acceptedProblemIds];

        bool solvedToday = solved.Contains(todaysChallenge.ProblemId);
        int currentStreak = CalculateCurrentStreak(challengesNewestFirst, solved, today);
        int longestStreak = CalculateLongestStreak(challengesNewestFirst, solved);

        return Result.Success(
            new TodaysDailyChallengeDto(
                problem.Id,
                problem.Slug.Value,
                problem.Title.Value,
                problem.Difficulty.Tier,
                solvedToday,
                currentStreak,
                longestStreak
            )
        );
    }

    private static int CalculateCurrentStreak(
        IReadOnlyList<DailyChallenge> challengesNewestFirst,
        HashSet<Guid> solvedProblemIds,
        DateOnly today
    )
    {
        int startIndex = 0;
        if (
            challengesNewestFirst.Count > 0
            && challengesNewestFirst[0].ChallengeDate == today
            && !solvedProblemIds.Contains(challengesNewestFirst[0].ProblemId)
        )
        {
            startIndex = 1;
        }

        int streak = 0;
        for (int i = startIndex; i < challengesNewestFirst.Count; i++)
        {
            if (!solvedProblemIds.Contains(challengesNewestFirst[i].ProblemId))
                break;

            streak++;
        }

        return streak;
    }

    private static int CalculateLongestStreak(IReadOnlyList<DailyChallenge> challenges, HashSet<Guid> solvedProblemIds)
    {
        int longest = 0;
        int current = 0;

        foreach (DailyChallenge challenge in challenges)
        {
            if (solvedProblemIds.Contains(challenge.ProblemId))
            {
                current++;
                longest = Math.Max(longest, current);
            }
            else
            {
                current = 0;
            }
        }

        return longest;
    }
}