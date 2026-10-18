namespace StackDuel.Application.Games;

public interface IGameConcurrencyGuard
{
    Task<T> RunExclusiveAsync<T>(
        Guid gameId,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default
    );
}