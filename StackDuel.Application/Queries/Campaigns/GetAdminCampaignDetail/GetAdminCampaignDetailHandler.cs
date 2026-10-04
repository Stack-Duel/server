using Ardalis.Result;
using StackDuel.Application.Campaigns;
using StackDuel.Application.Campaigns.Dtos;
using StackDuel.Domain.Campaigns.Entities;

namespace StackDuel.Application.Queries.Campaigns.GetAdminCampaignDetail;

internal sealed class GetAdminCampaignDetailHandler(ICampaignReadRepository campaignReadRepository)
    : IQueryHandler<GetAdminCampaignDetailQuery, CampaignDto>
{
    public async Task<Result<CampaignDto>> Handle(
        GetAdminCampaignDetailQuery request,
        CancellationToken cancellationToken
    )
    {
        Campaign? campaign = await campaignReadRepository.FindByIdAsync(request.CampaignId, cancellationToken);

        if (campaign is null)
            return Result.NotFound();

        return Result.Success(CampaignDtoMapper.ToDto(campaign));
    }
}