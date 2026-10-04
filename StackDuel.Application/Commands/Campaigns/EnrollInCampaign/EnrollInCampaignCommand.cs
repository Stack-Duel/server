using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Campaigns.EnrollInCampaign;

public sealed record EnrollInCampaignCommand(Guid CampaignId, Guid UserId) : ICommand<Guid>;