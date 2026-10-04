using Ardalis.Result;
using StackDuel.Application.Campaigns;
using StackDuel.Application.Campaigns.Dtos;
using StackDuel.Domain.Campaigns.Entities;

namespace StackDuel.Application.Queries.Campaigns.GetCampaignPath;

internal sealed class GetCampaignPathHandler(ICampaignReadRepository campaignReadRepository)
    : IQueryHandler<GetCampaignPathQuery, IReadOnlyList<CampaignSummaryDto>>
{
    public async Task<Result<IReadOnlyList<CampaignSummaryDto>>> Handle(
        GetCampaignPathQuery request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<Campaign> campaigns = await campaignReadRepository.GetPublishedPathAsync(cancellationToken);

        IReadOnlyList<CampaignSummaryDto> dtos = [.. campaigns.Select(CampaignDtoMapper.ToSummaryDto)];

        return Result<IReadOnlyList<CampaignSummaryDto>>.Success(dtos);
    }
}