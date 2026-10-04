using StackDuel.Application.Campaigns.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Queries;

namespace StackDuel.Application.Queries.Campaigns.GetAdminCampaignsPageable;

public sealed record GetAdminCampaignsPageableQuery(PaginationRequest PaginationRequest, string? Search)
    : IQuery<PageResult<AdminCampaignListItemDto>>;