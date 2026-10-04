using StackDuel.Application.Commands;
using StackDuel.Domain.Campaigns.Enums;

namespace StackDuel.Application.Commands.Campaigns.CreateCampaign;

public sealed record CreateCampaignCommand(string Title, string Description, CampaignDifficulty Difficulty)
    : ICommand<Guid>;