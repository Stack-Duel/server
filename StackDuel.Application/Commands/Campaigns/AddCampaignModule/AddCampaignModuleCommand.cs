using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Campaigns.AddCampaignModule;

public sealed record AddCampaignModuleCommand(Guid CampaignId, string Title, string Description) : ICommand<Guid>;