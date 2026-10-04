using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Campaigns.CompleteUnit;

public sealed record CompleteUnitCommand(Guid UnitId, Guid UserId) : ICommand;