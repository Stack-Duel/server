using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Queries;
using StackDuel.Domain.Games.Entities;
using Ardalis.Result;

namespace StackDuel.Application.Queries.Games.GetGameModes;

internal sealed class GetGameModesHandler(IGameReadRepository gameReadRepository)
    : IQueryHandler<GetGameModesQuery, IReadOnlyList<GameModeDto>>
{
    public async Task<Result<IReadOnlyList<GameModeDto>>> Handle(
        GetGameModesQuery request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<GameMode> modes = await gameReadRepository.GetActiveGameModesAsync(cancellationToken);

        IReadOnlyList<GameModeDto> dtos =
        [
            .. modes.Select(m => new GameModeDto(
                m.Id,
                m.Key,
                m.Name,
                m.Description,
                m.MinPlayers,
                m.MaxPlayers,
                [
                    .. m
                        .TimeOptions.OrderBy(o => o.DurationSeconds)
                        .Select(o => new GameModeTimeOptionDto(o.DurationSeconds, o.IsDefault)),
                ]
            )),
        ];

        return Result<IReadOnlyList<GameModeDto>>.Success(dtos);
    }
}