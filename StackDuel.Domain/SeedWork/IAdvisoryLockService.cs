namespace StackDuel.Domain.SeedWork;

public interface IAdvisoryLockService
{
    Task AcquireUserScopedLockAsync(Guid key, CancellationToken cancellationToken = default);
}