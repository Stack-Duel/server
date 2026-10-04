using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Campaigns.SetUnitProblems;

public sealed record SetUnitProblemsCommand(Guid CampaignId, Guid ModuleId, Guid UnitId, IReadOnlyList<Guid> ProblemIds)
    : ICommand;