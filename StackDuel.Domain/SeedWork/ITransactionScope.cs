namespace StackDuel.Domain.SeedWork;

public interface ITransactionScope : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}