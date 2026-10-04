using Ardalis.Result;
using StackDuel.Application.Campaigns;
using StackDuel.Application.Campaigns.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Domain.Campaigns.Entities;

namespace StackDuel.Application.Queries.Campaigns.GetAdminCampaignsPageable;

internal sealed class GetAdminCampaignsPageableHandler(ICampaignReadRepository campaignReadRepository)
    : IQueryHandler<GetAdminCampaignsPageableQuery, PageResult<AdminCampaignListItemDto>>
{
    public async Task<Result<PageResult<AdminCampaignListItemDto>>> Handle(
        GetAdminCampaignsPageableQuery request,
        CancellationToken cancellationToken
    )
    {
        PageResult<Campaign> page = await campaignReadRepository.GetAdminPagedAsync(
            request.PaginationRequest,
            request.Search,
            cancellationToken
        );

        List<AdminCampaignListItemDto> results = [.. page.Results.Select(CampaignDtoMapper.ToAdminListItemDto)];

        return Result.Success(
            new PageResult<AdminCampaignListItemDto>
            {
                Results = results,
                Total = page.Total,
                Page = page.Page,
                Size = page.Size,
            }
        );
    }
}