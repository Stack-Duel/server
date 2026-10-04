using StackDuel.Application.Commands;
using StackDuel.Application.Games;
using MediatR;

namespace StackDuel.Application.Commands.Games.CreateGame;

internal sealed record CreateGameCommand(
    string GameModeKey,
    IReadOnlyList<TrackLanguageSelection> TrackSelections,
    int TimeLimitInSeconds,
    Guid CreatedByUserId,
    bool SkipsEnabled = true
) : ICommand<Guid>;