namespace StackDuel.Domain.SeedWork;

public interface IUnitOfWork
{
    Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}