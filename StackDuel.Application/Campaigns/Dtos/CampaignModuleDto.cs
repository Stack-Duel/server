namespace StackDuel.Application.Campaigns.Dtos;

public sealed record CampaignModuleDto(
    Guid Id,
    string Title,
    string Description,
    int SortOrder,
    IReadOnlyList<CampaignUnitDto> Units
);