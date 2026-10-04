using StackDuel.Domain.Campaigns.Entities;
using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Application.Campaigns;

public interface ICampaignProgressReadRepository
{
    Task<CampaignEnrollment?> FindEnrollmentAsync(
        Guid userId,
        Guid campaignId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<CampaignEnrollment>> GetEnrollmentsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<Guid>> GetCompletedUnitIdsAsync(
        Guid userId,
        Guid campaignId,
        CancellationToken cancellationToken = default
    );

    Task<bool> HasCompletedUnitAsync(Guid userId, Guid unitId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UnitType>> GetCompletedUnitTypesAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<DateTime>> GetCompletionTimestampsAsync(
        Guid userId,
        CancellationToken cancellationToken = default
    );
}