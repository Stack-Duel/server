using StackDuel.Application.Campaigns.Dtos;
using StackDuel.Application.Queries;

namespace StackDuel.Application.Queries.Campaigns.GetCampaignBySlug;

public sealed record GetCampaignBySlugQuery(string Slug) : IQuery<CampaignDto>;