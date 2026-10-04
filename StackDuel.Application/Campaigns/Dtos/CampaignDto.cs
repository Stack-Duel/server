using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Application.Campaigns.Dtos;

public sealed record CampaignDto(
    Guid Id,
    string Slug,
    string Title,
    string Description,
    CampaignDifficulty Difficulty,
    CampaignStatus Status,
    string? IconKey,
    int SortOrder,
    IReadOnlyList<Guid> PrerequisiteCampaignIds,
    IReadOnlyList<CampaignModuleDto> Modules
);