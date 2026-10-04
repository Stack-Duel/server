using StackDuel.Domain.Campaigns.Enums;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Campaigns.Entities;

public sealed class CampaignUnit : Entity
{
    public CampaignUnit(
        Guid campaignModuleId,
        string title,
        string content,
        UnitType unitType,
        int estimatedMinutes,
        int sortOrder
    )
    {
        if (campaignModuleId == Guid.Empty)
            throw new ArgumentException("Campaign module id must not be empty.", nameof(campaignModuleId));

        CampaignModuleId = campaignModuleId;

        SetDetails(title, content, unitType, estimatedMinutes);

        SortOrder = sortOrder;
    }

    public void UpdateDetails(string title, string content, UnitType unitType, int estimatedMinutes) =>
        SetDetails(title, content, unitType, estimatedMinutes);

    public void SetProblems(IEnumerable<Guid> problemIds)
    {
        Guid[] distinctProblemIds =
            problemIds?.Where(id => id != Guid.Empty).Distinct().ToArray()
            ?? throw new ArgumentNullException(nameof(problemIds));

        _problems.Clear();

        int sortOrder = 0;
        foreach (Guid problemId in distinctProblemIds)
        {
            _problems.Add(new UnitProblem(Id, problemId, sortOrder));
            sortOrder++;
        }
    }

    private void SetDetails(string title, string content, UnitType unitType, int estimatedMinutes)
    {
        Title = !string.IsNullOrWhiteSpace(title)
            ? title.Trim()
            : throw new ArgumentException("Unit title must not be empty.", nameof(title));

        Content = content ?? throw new ArgumentNullException(nameof(content));

        if (estimatedMinutes < 0)
            throw new ArgumentException("Estimated minutes must not be negative.", nameof(estimatedMinutes));

        UnitType = unitType;
        EstimatedMinutes = estimatedMinutes;
    }

    private CampaignUnit() { }

    public Guid CampaignModuleId { get; private set; }
    public string Title { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public UnitType UnitType { get; private set; }
    public int EstimatedMinutes { get; private set; }
    public int SortOrder { get; private set; }

    public IReadOnlyCollection<UnitProblem> Problems => _problems.AsReadOnly();

    private readonly List<UnitProblem> _problems = [];
}