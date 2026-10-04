using StackDuel.Domain.Campaigns.Entities;

namespace StackDuel.Domain.Campaigns;

public interface ICampaignEnrollmentWriteRepository
{
    Task AddAsync(CampaignEnrollment entity, CancellationToken cancellationToken = default);
    Task<CampaignEnrollment?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CampaignEnrollment entity, CancellationToken cancellationToken = default);
}