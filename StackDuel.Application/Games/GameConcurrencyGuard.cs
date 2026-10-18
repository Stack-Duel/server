using StackDuel.Domain.SeedWork;

namespace StackDuel.Application.Games;

internal sealed class GameConcurrencyGuard(IAdvisoryLock advisoryLock) : IGameConcurrencyGuard
{
    private const string LockNamespace = "game";

    public Task<T> RunExclusiveAsync<T>(
        Guid gameId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default
    ) => advisoryLock.RunExclusiveAsync(LockNamespace, gameId, action, cancellationToken);
}