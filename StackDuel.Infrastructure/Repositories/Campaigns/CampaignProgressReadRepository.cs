using Microsoft.EntityFrameworkCore;
using StackDuel.Application.Campaigns;
using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Campaigns.Enums;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.Infrastructure.Repositories.Campaigns;

internal sealed class CampaignProgressReadRepository(StackDuelDbContext context) : ICampaignProgressReadRepository
{
    public async Task<CampaignEnrollment?> FindEnrollmentAsync(
        Guid userId,
        Guid campaignId,
        CancellationToken cancellationToken = default
    ) =>
        await context
            .CampaignEnrollments.AsNoTracking()
            .FirstOrDefaultAsync(e => e.UserId == userId && e.CampaignId == campaignId, cancellationToken);

    public async Task<IReadOnlyList<CampaignEnrollment>> GetEnrollmentsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    ) => await context.CampaignEnrollments.AsNoTracking().Where(e => e.UserId == userId).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> GetCompletedUnitIdsAsync(
        Guid userId,
        Guid campaignId,
        CancellationToken cancellationToken = default
    )
    {
        IQueryable<Guid> unitIdsInCampaign = context
            .CampaignUnits.Where(u =>
                context
                    .CampaignModules.Where(m => m.CampaignId == campaignId)
                    .Select(m => m.Id)
                    .Contains(u.CampaignModuleId)
            )
            .Select(u => u.Id);

        return await context
            .UnitCompletions.AsNoTracking()
            .Where(uc => uc.UserId == userId && unitIdsInCampaign.Contains(uc.CampaignUnitId))
            .Select(uc => uc.CampaignUnitId)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasCompletedUnitAsync(
        Guid userId,
        Guid unitId,
        CancellationToken cancellationToken = default
    ) =>
        await context
            .UnitCompletions.AsNoTracking()
            .AnyAsync(uc => uc.UserId == userId && uc.CampaignUnitId == unitId, cancellationToken);

    public async Task<IReadOnlyList<UnitType>> GetCompletedUnitTypesAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await context
            .UnitCompletions.AsNoTracking()
            .Where(uc => uc.UserId == userId)
            .Join(context.CampaignUnits, uc => uc.CampaignUnitId, u => u.Id, (uc, u) => u.UnitType)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DateTime>> GetCompletionTimestampsAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    ) =>
        await context
            .UnitCompletions.AsNoTracking()
            .Where(uc => uc.UserId == userId)
            .Select(uc => uc.CompletedAt)
            .ToListAsync(cancellationToken);
}