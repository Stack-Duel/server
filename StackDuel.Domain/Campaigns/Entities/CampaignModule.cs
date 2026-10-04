using StackDuel.Domain.Campaigns.Enums;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Campaigns.Entities;

public sealed class CampaignModule : Entity
{
    public CampaignModule(Guid campaignId, string title, string description, int sortOrder)
    {
        if (campaignId == Guid.Empty)
            throw new ArgumentException("Campaign id must not be empty.", nameof(campaignId));

        CampaignId = campaignId;

        SetDetails(title, description);

        SortOrder = sortOrder;
    }

    public void UpdateDetails(string title, string description) => SetDetails(title, description);

    public CampaignUnit AddUnit(string title, string content, UnitType unitType, int estimatedMinutes)
    {
        CampaignUnit unit = new(Id, title, content, unitType, estimatedMinutes, _units.Count);
        _units.Add(unit);
        return unit;
    }

    private void SetDetails(string title, string description)
    {
        Title = !string.IsNullOrWhiteSpace(title)
            ? title.Trim()
            : throw new ArgumentException("Module title must not be empty.", nameof(title));

        Description = description ?? throw new ArgumentNullException(nameof(description));
    }

    private CampaignModule() { }

    public Guid CampaignId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public int SortOrder { get; private set; }

    public IReadOnlyCollection<CampaignUnit> Units => _units.AsReadOnly();

    private readonly List<CampaignUnit> _units = [];
}