namespace StackDuel.Application.Problems.Dtos;

public sealed record ProblemDeletionImpactDto(
    int SubmissionCount,
    int GameRoundCount,
    int UnfinishedGameCount,
    int DailyChallengeCount,
    int CampaignUnitCount,
    int PoolCount
)
{
    public static readonly ProblemDeletionImpactDto Unused = new(0, 0, 0, 0, 0, 0);

    public bool IsInPlay => UnfinishedGameCount > 0;

    public bool HasHistory =>
        SubmissionCount > 0
        || GameRoundCount > 0
        || DailyChallengeCount > 0
        || CampaignUnitCount > 0
        || PoolCount > 0;
}