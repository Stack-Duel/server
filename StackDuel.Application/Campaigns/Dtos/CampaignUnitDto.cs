using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Application.Campaigns.Dtos;

public sealed record CampaignUnitDto(
    Guid Id,
    string Title,
    string Content,
    UnitType UnitType,
    int SortOrder,
    int EstimatedMinutes,
    IReadOnlyList<CampaignUnitProblemDto> Problems
);