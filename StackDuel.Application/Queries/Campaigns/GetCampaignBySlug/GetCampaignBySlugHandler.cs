using StackDuel.Application.Campaigns;
using StackDuel.Application.Campaigns.Dtos;
using StackDuel.Domain.Campaigns.Entities;
using Ardalis.Result;

namespace StackDuel.Application.Queries.Campaigns.GetCampaignBySlug;

internal sealed class GetCampaignBySlugHandler(ICampaignReadRepository campaignReadRepository)
    : IQueryHandler<GetCampaignBySlugQuery, CampaignDto>
{
    public async Task<Result<CampaignDto>> Handle(GetCampaignBySlugQuery request, CancellationToken cancellationToken)
    {
        Campaign? campaign = await campaignReadRepository.FindPublishedBySlugAsync(request.Slug, cancellationToken);

        if (campaign is null)
            return Result.NotFound();

        return Result.Success(CampaignDtoMapper.ToDto(campaign));
    }
}