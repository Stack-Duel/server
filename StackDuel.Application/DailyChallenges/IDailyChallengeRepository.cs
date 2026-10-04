using StackDuel.Domain.DailyChallenges.Entities;

namespace StackDuel.Application.DailyChallenges;

public interface IDailyChallengeRepository
{
    Task<DailyChallenge?> FindByDateAsync(DateOnly date, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<DailyChallenge>> GetAllOrderedByDateDescendingAsync(
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<DailyChallenge>> GetUpcomingAsync(
        DateOnly afterDate,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(DailyChallenge challenge, CancellationToken cancellationToken = default);

    Task UpdateAsync(DailyChallenge challenge, CancellationToken cancellationToken = default);
}