namespace StackDuel.Domain.SeedWork;

public interface IAdvisoryLock
{
    Task<T> RunExclusiveAsync<T>(
        string lockNamespace,
        Guid key,
        Func<CancellationToken, Task<T>> action,
        CancellationToken cancellationToken = default
    );
}