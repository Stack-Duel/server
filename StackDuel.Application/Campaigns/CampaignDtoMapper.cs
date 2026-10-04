using StackDuel.Application.Campaigns.Dtos;
using StackDuel.Domain.Campaigns.Entities;

namespace StackDuel.Application.Campaigns;

internal static class CampaignDtoMapper
{
    public static CampaignDto ToDto(Campaign campaign) =>
        new(
            campaign.Id,
            campaign.Slug,
            campaign.Title,
            campaign.Description,
            campaign.Difficulty,
            campaign.Status,
            campaign.IconKey,
            campaign.SortOrder,
            [.. campaign.Prerequisites.Select(p => p.RequiredCampaignId)],
            [
                .. campaign
                    .Modules.OrderBy(m => m.SortOrder)
                    .Select(module => new CampaignModuleDto(
                        module.Id,
                        module.Title,
                        module.Description,
                        module.SortOrder,
                        [
                            .. module
                                .Units.OrderBy(u => u.SortOrder)
                                .Select(unit => new CampaignUnitDto(
                                    unit.Id,
                                    unit.Title,
                                    unit.Content,
                                    unit.UnitType,
                                    unit.SortOrder,
                                    unit.EstimatedMinutes,
                                    [
                                        .. unit
                                            .Problems.OrderBy(p => p.SortOrder)
                                            .Select(p => new CampaignUnitProblemDto(p.ProblemId, p.SortOrder)),
                                    ]
                                )),
                        ]
                    )),
            ]
        );

    public static CampaignSummaryDto ToSummaryDto(Campaign campaign)
    {
        int unitCount = campaign.Modules.SelectMany(m => m.Units).Count();
        int estimatedMinutes = campaign.Modules.SelectMany(m => m.Units).Sum(u => u.EstimatedMinutes);

        return new CampaignSummaryDto(
            campaign.Id,
            campaign.Slug,
            campaign.Title,
            campaign.Description,
            campaign.Difficulty,
            campaign.IconKey,
            campaign.SortOrder,
            campaign.Modules.Count,
            unitCount,
            estimatedMinutes,
            [.. campaign.Prerequisites.Select(p => p.RequiredCampaignId)]
        );
    }

    public static AdminCampaignListItemDto ToAdminListItemDto(Campaign campaign) =>
        new(
            campaign.Id,
            campaign.Slug,
            campaign.Title,
            campaign.Difficulty,
            campaign.Status,
            campaign.Modules.Count,
            campaign.Modules.SelectMany(m => m.Units).Count(),
            campaign.SortOrder
        );
}