using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Api.Requests.Campaign;

public sealed record AddCampaignUnitRequest(string Title, string Content, UnitType UnitType, int EstimatedMinutes);