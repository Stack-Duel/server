using Ardalis.Result;
using StackDuel.Application.Campaigns;
using StackDuel.Application.Campaigns.Dtos;
using StackDuel.Domain.Campaigns.Entities;

namespace StackDuel.Application.Queries.Campaigns.GetMyCampaignProgress;

internal sealed class GetMyCampaignProgressHandler(
    ICampaignReadRepository campaignReadRepository,
    ICampaignProgressReadRepository campaignProgressReadRepository
) : IQueryHandler<GetMyCampaignProgressQuery, UserCampaignProgressDto>
{
    public async Task<Result<UserCampaignProgressDto>> Handle(
        GetMyCampaignProgressQuery request,
        CancellationToken cancellationToken
    )
    {
        Campaign? campaign = await campaignReadRepository.FindByIdAsync(request.CampaignId, cancellationToken);

        if (campaign is null)
            return Result.NotFound();

        CampaignEnrollment? enrollment = await campaignProgressReadRepository.FindEnrollmentAsync(
            request.UserId,
            request.CampaignId,
            cancellationToken
        );

        IReadOnlyList<Guid> completedUnitIds = await campaignProgressReadRepository.GetCompletedUnitIdsAsync(
            request.UserId,
            request.CampaignId,
            cancellationToken
        );

        int totalUnits = campaign.Modules.SelectMany(m => m.Units).Count();

        return Result.Success(
            new UserCampaignProgressDto(
                campaign.Id,
                enrollment is not null,
                enrollment?.Status,
                totalUnits,
                completedUnitIds.Count,
                completedUnitIds
            )
        );
    }
}