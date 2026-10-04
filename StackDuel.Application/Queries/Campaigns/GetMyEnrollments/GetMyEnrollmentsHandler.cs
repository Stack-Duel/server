using Ardalis.Result;
using StackDuel.Application.Campaigns;
using StackDuel.Application.Campaigns.Dtos;
using StackDuel.Domain.Campaigns.Entities;

namespace StackDuel.Application.Queries.Campaigns.GetMyEnrollments;

internal sealed class GetMyEnrollmentsHandler(
    ICampaignReadRepository campaignReadRepository,
    ICampaignProgressReadRepository campaignProgressReadRepository
) : IQueryHandler<GetMyEnrollmentsQuery, IReadOnlyList<UserCampaignProgressDto>>
{
    public async Task<Result<IReadOnlyList<UserCampaignProgressDto>>> Handle(
        GetMyEnrollmentsQuery request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<CampaignEnrollment> enrollments = await campaignProgressReadRepository.GetEnrollmentsForUserAsync(
            request.UserId,
            cancellationToken
        );

        List<UserCampaignProgressDto> results = [];

        foreach (CampaignEnrollment enrollment in enrollments)
        {
            Campaign? campaign = await campaignReadRepository.FindByIdAsync(enrollment.CampaignId, cancellationToken);

            if (campaign is null)
                continue;

            IReadOnlyList<Guid> completedUnitIds = await campaignProgressReadRepository.GetCompletedUnitIdsAsync(
                request.UserId,
                enrollment.CampaignId,
                cancellationToken
            );

            int totalUnits = campaign.Modules.SelectMany(m => m.Units).Count();

            results.Add(
                new UserCampaignProgressDto(
                    campaign.Id,
                    true,
                    enrollment.Status,
                    totalUnits,
                    completedUnitIds.Count,
                    completedUnitIds
                )
            );
        }

        return Result.Success<IReadOnlyList<UserCampaignProgressDto>>(results);
    }
}