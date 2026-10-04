using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Campaigns.ArchiveCampaign;

public sealed record ArchiveCampaignCommand(Guid CampaignId) : ICommand;