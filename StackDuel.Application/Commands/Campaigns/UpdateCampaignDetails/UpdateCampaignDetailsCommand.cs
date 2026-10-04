using StackDuel.Application.Commands;
using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Application.Commands.Campaigns.UpdateCampaignDetails;

public sealed record UpdateCampaignDetailsCommand(
    Guid CampaignId,
    string Title,
    string Description,
    CampaignDifficulty Difficulty,
    string? IconKey
) : ICommand;