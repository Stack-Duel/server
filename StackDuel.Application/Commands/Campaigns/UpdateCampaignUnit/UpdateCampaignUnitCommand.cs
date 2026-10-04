using StackDuel.Application.Commands;
using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Application.Commands.Campaigns.UpdateCampaignUnit;

public sealed record UpdateCampaignUnitCommand(
    Guid CampaignId,
    Guid ModuleId,
    Guid UnitId,
    string Title,
    string Content,
    UnitType UnitType,
    int EstimatedMinutes
) : ICommand;