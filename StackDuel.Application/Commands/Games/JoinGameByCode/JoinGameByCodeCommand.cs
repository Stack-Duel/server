using StackDuel.Application.Commands;

namespace StackDuel.Application.Commands.Games.JoinGameByCode;

internal sealed record JoinGameByCodeCommand(string JoinCode, Guid RequestedByUserId) : ICommand<Guid>;