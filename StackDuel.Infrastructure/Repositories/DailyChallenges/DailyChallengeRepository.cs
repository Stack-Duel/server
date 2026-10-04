using StackDuel.Application.DailyChallenges;
using StackDuel.Domain.DailyChallenges.Entities;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace StackDuel.Infrastructure.Repositories.DailyChallenges;

internal sealed class DailyChallengeRepository(StackDuelDbContext context, IMemoryCache cache)
    : IDailyChallengeRepository
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromDays(1);
    private const string AllOrderedCacheKey = "daily-challenges:all-ordered";

    private static string ByDateCacheKey(DateOnly date) => $"daily-challenges:by-date:{date:O}";

    public async Task<DailyChallenge?> FindByDateAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        string cacheKey = ByDateCacheKey(date);

        if (cache.TryGetValue(cacheKey, out DailyChallenge? cached))
            return cached;

        DailyChallenge? challenge = await context
            .DailyChallenges.AsNoTracking()
            .FirstOrDefaultAsync(c => c.ChallengeDate == date, cancellationToken);

        cache.Set(cacheKey, challenge, CacheDuration);

        return challenge;
    }

    public async Task<IReadOnlyList<DailyChallenge>> GetAllOrderedByDateDescendingAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (cache.TryGetValue(AllOrderedCacheKey, out IReadOnlyList<DailyChallenge>? cached) && cached is not null)
            return cached;

        IReadOnlyList<DailyChallenge> challenges = await context
            .DailyChallenges.AsNoTracking()
            .OrderByDescending(c => c.ChallengeDate)
            .ToListAsync(cancellationToken);

        cache.Set(AllOrderedCacheKey, challenges, CacheDuration);

        return challenges;
    }

    public async Task<IReadOnlyList<DailyChallenge>> GetUpcomingAsync(
        DateOnly afterDate,
        CancellationToken cancellationToken = default
    ) =>
        await context
            .DailyChallenges.AsNoTracking()
            .Where(c => c.ChallengeDate > afterDate)
            .OrderBy(c => c.ChallengeDate)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(DailyChallenge challenge, CancellationToken cancellationToken = default)
    {
        await context.DailyChallenges.AddAsync(challenge, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        cache.Remove(AllOrderedCacheKey);
    }

    public async Task UpdateAsync(DailyChallenge challenge, CancellationToken cancellationToken = default)
    {
        if (context.Entry(challenge).State == EntityState.Detached)
            context.DailyChallenges.Update(challenge);

        await context.SaveChangesAsync(cancellationToken);

        cache.Remove(ByDateCacheKey(challenge.ChallengeDate));
        cache.Remove(AllOrderedCacheKey);
    }
}