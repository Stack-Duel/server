using StackDuel.Application.Campaigns.Dtos;
using StackDuel.Application.Queries;

namespace StackDuel.Application.Queries.Campaigns.GetCampaignPath;

public sealed record GetCampaignPathQuery : IQuery<IReadOnlyList<CampaignSummaryDto>>;