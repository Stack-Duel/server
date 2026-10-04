using StackDuel.Application.Campaigns.Dtos;
using StackDuel.Application.Queries;

namespace StackDuel.Application.Queries.Campaigns.GetMyCampaignProgress;

public sealed record GetMyCampaignProgressQuery(Guid CampaignId, Guid UserId) : IQuery<UserCampaignProgressDto>;