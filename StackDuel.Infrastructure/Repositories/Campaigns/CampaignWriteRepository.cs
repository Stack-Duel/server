using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace StackDuel.Infrastructure.Repositories.Campaigns;

internal sealed class CampaignWriteRepository(StackDuelDbContext context) : ICampaignWriteRepository
{
    public async Task AddAsync(Campaign entity, CancellationToken cancellationToken = default)
    {
        await context.Campaigns.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Campaign?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context
            .Campaigns.Include(c => c.Modules)
                .ThenInclude(m => m.Units)
                    .ThenInclude(u => u.Problems)
            .Include(c => c.Prerequisites)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public async Task SaveChangesAsync(Campaign entity, CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}