using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Campaigns.Entities;

public sealed class UnitProblem : Entity
{
    public UnitProblem(Guid campaignUnitId, Guid problemId, int sortOrder)
    {
        if (campaignUnitId == Guid.Empty)
            throw new ArgumentException("Campaign unit id must not be empty.", nameof(campaignUnitId));

        if (problemId == Guid.Empty)
            throw new ArgumentException("Problem id must not be empty.", nameof(problemId));

        CampaignUnitId = campaignUnitId;
        ProblemId = problemId;
        SortOrder = sortOrder;
    }

    private UnitProblem() { }

    public Guid CampaignUnitId { get; private set; }
    public Guid ProblemId { get; private set; }
    public int SortOrder { get; private set; }
}