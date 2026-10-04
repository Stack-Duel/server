using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Campaigns.PublishCampaign;

public sealed record PublishCampaignCommand(Guid CampaignId) : ICommand;