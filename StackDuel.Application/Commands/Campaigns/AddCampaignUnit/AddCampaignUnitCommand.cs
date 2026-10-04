using StackDuel.Application.Commands;
using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Application.Commands.Campaigns.AddCampaignUnit;

public sealed record AddCampaignUnitCommand(
    Guid CampaignId,
    Guid ModuleId,
    string Title,
    string Content,
    UnitType UnitType,
    int EstimatedMinutes
) : ICommand<Guid>;