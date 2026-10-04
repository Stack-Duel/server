namespace StackDuel.Api.Requests.Campaign;

public sealed record SetCampaignPrerequisitesRequest(IReadOnlyList<Guid> RequiredCampaignIds);