using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Campaigns;

internal sealed class UnitCompletionWriteRepository(StackDuelDbContext context) : IUnitCompletionWriteRepository
{
    public async Task AddAsync(UnitCompletion entity, CancellationToken cancellationToken = default)
    {
        await context.UnitCompletions.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }
}