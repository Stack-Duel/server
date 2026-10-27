using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Infrastructure.Persistence;

internal sealed class UnitOfWork(StackDuelDbContext context) : IUnitOfWork
{
    public async Task<ITransactionScope> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        return new EfTransactionScope(transaction);
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}