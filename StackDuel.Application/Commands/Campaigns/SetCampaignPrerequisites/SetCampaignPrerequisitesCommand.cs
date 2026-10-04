using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Campaigns.SetCampaignPrerequisites;

public sealed record SetCampaignPrerequisitesCommand(Guid CampaignId, IReadOnlyList<Guid> RequiredCampaignIds)
    : ICommand;