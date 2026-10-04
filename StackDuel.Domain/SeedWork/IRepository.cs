namespace StackDuel.Domain.SeedWork;

public interface IRepository<T>
    where T : AggregateRoot
{
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
    Task<T?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
}