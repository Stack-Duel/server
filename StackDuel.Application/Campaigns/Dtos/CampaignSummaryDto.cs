using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Application.Campaigns.Dtos;

public sealed record CampaignSummaryDto(
    Guid Id,
    string Slug,
    string Title,
    string Description,
    CampaignDifficulty Difficulty,
    string? IconKey,
    int SortOrder,
    int ModuleCount,
    int UnitCount,
    int EstimatedMinutes,
    IReadOnlyList<Guid> PrerequisiteCampaignIds
);