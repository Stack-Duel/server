using StackDuel.Application.Campaigns.Dtos;
using StackDuel.Application.Queries;

namespace StackDuel.Application.Queries.Campaigns.GetAdminCampaignDetail;

public sealed record GetAdminCampaignDetailQuery(Guid CampaignId) : IQuery<CampaignDto>;