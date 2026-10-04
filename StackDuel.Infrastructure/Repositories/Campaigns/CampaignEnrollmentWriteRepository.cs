using Microsoft.EntityFrameworkCore;
using StackDuel.Domain.Campaigns;
using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Campaigns;

internal sealed class CampaignEnrollmentWriteRepository(StackDuelDbContext context) : ICampaignEnrollmentWriteRepository
{
    public async Task AddAsync(CampaignEnrollment entity, CancellationToken cancellationToken = default)
    {
        await context.CampaignEnrollments.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<CampaignEnrollment?> FindByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await context.CampaignEnrollments.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public async Task SaveChangesAsync(CampaignEnrollment entity, CancellationToken cancellationToken = default)
    {
        await context.SaveChangesAsync(cancellationToken);
    }
}