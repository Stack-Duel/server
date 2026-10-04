using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Campaigns.Entities;

public sealed class CampaignPrerequisite : Entity
{
    public CampaignPrerequisite(Guid campaignId, Guid requiredCampaignId)
    {
        if (campaignId == Guid.Empty)
            throw new ArgumentException("Campaign id must not be empty.", nameof(campaignId));

        if (requiredCampaignId == Guid.Empty)
            throw new ArgumentException("Required campaign id must not be empty.", nameof(requiredCampaignId));

        CampaignId = campaignId;
        RequiredCampaignId = requiredCampaignId;
    }

    private CampaignPrerequisite() { }

    public Guid CampaignId { get; private set; }
    public Guid RequiredCampaignId { get; private set; }
}