using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Api.Requests.Campaign;

public sealed record CreateCampaignRequest(string Title, string Description, CampaignDifficulty Difficulty);