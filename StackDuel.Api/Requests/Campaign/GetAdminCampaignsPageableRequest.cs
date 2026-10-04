namespace StackDuel.Api.Requests.Campaign;

public sealed record GetAdminCampaignsPageableRequest(int Page, int Size, DateTime Timestamp, string? Search);