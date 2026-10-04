using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Application.Campaigns.Dtos;

public sealed record AdminCampaignListItemDto(
    Guid Id,
    string Slug,
    string Title,
    CampaignDifficulty Difficulty,
    CampaignStatus Status,
    int ModuleCount,
    int UnitCount,
    int SortOrder
);