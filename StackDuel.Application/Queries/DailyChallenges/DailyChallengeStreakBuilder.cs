using StackDuel.Application.DailyChallenges;
using StackDuel.Application.Submissions;
using StackDuel.Domain.DailyChallenges.Entities;

namespace StackDuel.Application.Queries.DailyChallenges;

internal sealed record DailyChallengeStreakResult(
    int CurrentStreak,
    int LongestStreak,
    IReadOnlySet<Guid> SolvedProblemIds
);

internal static class DailyChallengeStreakBuilder
{
    public static async Task<DailyChallengeStreakResult> BuildAsync(
        IDailyChallengeRepository dailyChallengeRepository,
        ISubmissionReadRepository submissionReadRepository,
        Guid userId,
        DateOnly today,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<DailyChallenge> challengesNewestFirst =
        [
            .. (await dailyChallengeRepository.GetAllOrderedByDateDescendingAsync(cancellationToken)).Where(c =>
                c.ChallengeDate <= today
            ),
        ];

        IReadOnlyList<Guid> problemIds = [.. challengesNewestFirst.Select(c => c.ProblemId).Distinct()];
        IReadOnlyList<Guid> acceptedProblemIds = await submissionReadRepository.GetAcceptedProblemIdsForUserAsync(
            userId,
            problemIds,
            cancellationToken
        );

        HashSet<Guid> solved = [.. acceptedProblemIds];

        return new DailyChallengeStreakResult(
            CalculateCurrentStreak(challengesNewestFirst, solved, today),
            CalculateLongestStreak(challengesNewestFirst, solved),
            solved
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