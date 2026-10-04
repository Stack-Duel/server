using StackDuel.Domain.Campaigns.Entities;

namespace StackDuel.Domain.Campaigns;

public interface ICampaignWriteRepository
{
    Task AddAsync(Campaign entity, CancellationToken cancellationToken = default);
    Task<Campaign?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(Campaign entity, CancellationToken cancellationToken = default);
}