using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Campaigns.Entities;

public sealed class UnitCompletion : AggregateRoot
{
    public UnitCompletion(Guid userId, Guid campaignUnitId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id must not be empty.", nameof(userId));

        if (campaignUnitId == Guid.Empty)
            throw new ArgumentException("Campaign unit id must not be empty.", nameof(campaignUnitId));

        UserId = userId;
        CampaignUnitId = campaignUnitId;
        CompletedAt = DateTime.UtcNow;
    }

    private UnitCompletion() { }

    public Guid UserId { get; private set; }
    public Guid CampaignUnitId { get; private set; }
    public DateTime CompletedAt { get; private set; }
}