namespace StackDuel.Application.Campaigns.Dtos;

public sealed record UserLearningStatsDto(
    int TotalXp,
    int Level,
    int XpIntoLevel,
    int XpForNextLevel,
    int CurrentStreakDays,
    int CompletedCampaignsCount
);