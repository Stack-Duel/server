using StackDuel.Domain.SeedWork;

namespace StackDuel.Application.Authorization;

public interface IOwnershipPolicy<TEntity>
    where TEntity : AggregateRoot
{
    Task<bool> IsOwnedByAsync(Guid resourceId, Guid userId, CancellationToken cancellationToken);
}
