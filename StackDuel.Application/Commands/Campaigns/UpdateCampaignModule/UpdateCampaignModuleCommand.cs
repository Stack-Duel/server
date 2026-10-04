using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Campaigns.UpdateCampaignModule;

public sealed record UpdateCampaignModuleCommand(Guid CampaignId, Guid ModuleId, string Title, string Description)
    : ICommand;