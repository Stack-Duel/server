using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Api.Requests.Campaign;

public sealed record UpdateCampaignDetailsRequest(
    string Title,
    string Description,
    CampaignDifficulty Difficulty,
    string? IconKey
);