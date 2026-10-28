using Microsoft.EntityFrameworkCore.Storage;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Infrastructure.Persistence;

internal sealed class EfTransactionScope(IDbContextTransaction transaction) : ITransactionScope
{
    public Task CommitAsync(CancellationToken cancellationToken = default) =>
        transaction.CommitAsync(cancellationToken);

    public ValueTask DisposeAsync() => transaction.DisposeAsync();
}