using StackDuel.Domain.Campaigns.Enums;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Campaigns.Entities;

public sealed class Campaign : AggregateRoot
{
    public Campaign(string slug, string title, string description, CampaignDifficulty difficulty)
    {
        Slug = !string.IsNullOrWhiteSpace(slug)
            ? slug.Trim().ToLowerInvariant()
            : throw new ArgumentException("Campaign slug must not be empty.", nameof(slug));

        SetDetails(title, description, difficulty, null);

        Status = CampaignStatus.Draft;
        SortOrder = 0;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string title, string description, CampaignDifficulty difficulty, string? iconKey) =>
        SetDetails(title, description, difficulty, iconKey);

    public void Publish()
    {
        if (Status != CampaignStatus.Draft)
            throw new InvalidOperationException("Only draft campaigns can be published.");

        Status = CampaignStatus.Published;
        PublishedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        if (Status == CampaignStatus.Archived)
            throw new InvalidOperationException("Campaign is already archived.");

        Status = CampaignStatus.Archived;
    }

    public void SetSortOrder(int sortOrder) => SortOrder = sortOrder;

    public CampaignModule AddModule(string title, string description)
    {
        CampaignModule module = new(Id, title, description, _modules.Count);
        _modules.Add(module);
        return module;
    }

    public void SetPrerequisites(IEnumerable<Guid> requiredCampaignIds)
    {
        Guid[] distinctIds =
            requiredCampaignIds?.Where(id => id != Guid.Empty).Distinct().ToArray()
            ?? throw new ArgumentNullException(nameof(requiredCampaignIds));

        if (distinctIds.Contains(Id))
            throw new ArgumentException("A campaign cannot be its own prerequisite.", nameof(requiredCampaignIds));

        _prerequisites.Clear();

        foreach (Guid requiredCampaignId in distinctIds)
            _prerequisites.Add(new CampaignPrerequisite(Id, requiredCampaignId));
    }

    private void SetDetails(string title, string description, CampaignDifficulty difficulty, string? iconKey)
    {
        Title = !string.IsNullOrWhiteSpace(title)
            ? title.Trim()
            : throw new ArgumentException("Campaign title must not be empty.", nameof(title));

        Description = description ?? throw new ArgumentNullException(nameof(description));
        Difficulty = difficulty;
        IconKey = string.IsNullOrWhiteSpace(iconKey) ? null : iconKey.Trim();
    }

    private Campaign() { }

    public string Slug { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public CampaignDifficulty Difficulty { get; private set; }
    public CampaignStatus Status { get; private set; }
    public string? IconKey { get; private set; }
    public int SortOrder { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? PublishedAt { get; private set; }

    public IReadOnlyCollection<CampaignModule> Modules => _modules.AsReadOnly();
    public IReadOnlyCollection<CampaignPrerequisite> Prerequisites => _prerequisites.AsReadOnly();

    private readonly List<CampaignModule> _modules = [];
    private readonly List<CampaignPrerequisite> _prerequisites = [];
}