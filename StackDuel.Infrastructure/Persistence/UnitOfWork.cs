using StackDuel.Domain.SeedWork;

namespace StackDuel.Infrastructure.Persistence;

internal sealed class UnitOfWork(StackDuelDbContext context) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}