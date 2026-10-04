using StackDuel.Application.DailyChallenges;
using StackDuel.Application.Jobs.DailyChallenges;
using StackDuel.Application.Problems;
using StackDuel.Domain.DailyChallenges.Entities;
using Microsoft.Extensions.Logging;

namespace StackDuel.Infrastructure.Jobs.DailyChallenges;

internal sealed partial class DailyChallengeAssignmentService(
    IDailyChallengeRepository dailyChallengeRepository,
    IProblemPoolRepository problemPoolRepository,
    IProblemReadRepository problemReadRepository,
    ILogger<DailyChallengeAssignmentService> logger
) : IDailyChallengeAssignmentService
{
    public const string PoolKey = "daily-challenge";
    public const int BufferDays = 30;

    /// <summary>
    /// Keeps the next <see cref="BufferDays"/> days' challenges assigned by cycling through the
    /// pool's members in their admin-defined order. The cursor resumes from one past whatever
    /// problem was most recently assigned, so it continues the sequence across runs rather than
    /// restarting it; a pool smaller than <see cref="BufferDays"/> simply wraps around and repeats
    /// rather than leaving days unassigned.
    /// </summary>
    public async Task AssignUpcomingChallengesAsync(CancellationToken cancellationToken = default)
    {
        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);

        var pool = await problemPoolRepository.FindByKeyAsync(PoolKey, cancellationToken);
        if (pool is null)
        {
            LogPoolMissing();
            return;
        }

        List<Guid> orderedProblemIds = [.. await problemReadRepository.GetOrderedEligibleProblemIdsAsync(
            pool.Id,
            cancellationToken
        )];
        if (orderedProblemIds.Count == 0)
        {
            LogPoolEmpty();
            return;
        }

        int cursor = await ResolveStartingCursorAsync(orderedProblemIds, cancellationToken);

        for (int offset = 0; offset < BufferDays; offset++)
        {
            DateOnly date = today.AddDays(offset);
            cancellationToken.ThrowIfCancellationRequested();

            if (await dailyChallengeRepository.FindByDateAsync(date, cancellationToken) is not null)
            {
                LogAlreadyAssigned(date);
                continue;
            }

            Guid problemId = orderedProblemIds[cursor % orderedProblemIds.Count];
            cursor++;

            await dailyChallengeRepository.AddAsync(new DailyChallenge(date, problemId), cancellationToken);
            LogAssigned(date, problemId);
        }
    }

    private async Task<int> ResolveStartingCursorAsync(
        List<Guid> orderedProblemIds,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<DailyChallenge> history = await dailyChallengeRepository.GetAllOrderedByDateDescendingAsync(
            cancellationToken
        );
        DailyChallenge? mostRecent = history.FirstOrDefault();
        if (mostRecent is null)
            return 0;

        int lastIndex = orderedProblemIds.IndexOf(mostRecent.ProblemId);
        return lastIndex >= 0 ? lastIndex + 1 : 0;
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Daily challenge for {Date} already assigned; skipping.")]
    private partial void LogAlreadyAssigned(DateOnly date);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No '" + PoolKey + "' problem pool exists; skipping daily challenge assignment."
    )]
    private partial void LogPoolMissing();

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "The '" + PoolKey + "' problem pool has no eligible problems; skipping daily challenge assignment."
    )]
    private partial void LogPoolEmpty();

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Assigned problem {ProblemId} as the daily challenge for {Date}."
    )]
    private partial void LogAssigned(DateOnly date, Guid problemId);
}