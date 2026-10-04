using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Api.Requests.Campaign;

public sealed record UpdateCampaignUnitRequest(string Title, string Content, UnitType UnitType, int EstimatedMinutes);