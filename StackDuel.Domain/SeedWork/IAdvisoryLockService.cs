namespace StackDuel.Domain.SeedWork;

public interface IAdvisoryLockService
{
    Task AcquireUserScopedLockAsync(Guid key, CancellationToken cancellationToken = default);

    Task<T> RunExclusiveAsync<T>(
        string lockNamespace,
        Guid key,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default
    );
}