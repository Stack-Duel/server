using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Tracks;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Tracks.Entities;
using Ardalis.Result;

namespace StackDuel.Application.Queries.Games.GetMyActiveGames;

internal sealed class GetMyActiveGamesHandler(
    IGameReadRepository gameReadRepository,
    ITrackReadRepository trackReadRepository,
    IUserReadRepository userReadRepository
) : IQueryHandler<GetMyActiveGamesQuery, IReadOnlyList<MyActiveGameDto>>
{
    public async Task<Result<IReadOnlyList<MyActiveGameDto>>> Handle(
        GetMyActiveGamesQuery request,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<Game> games = await gameReadRepository.GetActiveGamesForUserAsync(
            request.RequestedByUserId,
            cancellationToken
        );

        Dictionary<Guid, GameMode> gameModesById = [];
        foreach (Guid gameModeId in games.Select(g => g.GameModeId).Distinct())
        {
            GameMode? gameMode = await gameReadRepository.FindGameModeByIdIncludingInactiveAsync(
                gameModeId,
                cancellationToken
            );
            if (gameMode is not null)
                gameModesById[gameModeId] = gameMode;
        }

        IReadOnlyList<Track> tracks = await trackReadRepository.FindByIdsAsync(
            games.SelectMany(g => g.Tracks.Select(t => t.TrackId)).Distinct(),
            cancellationToken
        );
        Dictionary<Guid, string> trackNamesById = tracks.ToDictionary(t => t.Id, t => t.Name);

        IReadOnlyDictionary<Guid, UserDto> hostsByUserId = await userReadRepository.FindByIdsAsync(
            games.Select(g => g.HostUserId).Where(id => id is not null).Select(id => id!.Value),
            cancellationToken
        );

        var results = games
            .Where(g => gameModesById.ContainsKey(g.GameModeId))
            .Select(g =>
            {
                GameMode gameMode = gameModesById[g.GameModeId];
                Guid? hostUserId = g.HostUserId;
                string hostUsername =
                    hostUserId is Guid id && hostsByUserId.TryGetValue(id, out UserDto? host)
                        ? host.Username
                        : string.Empty;

                return new MyActiveGameDto(
                    g.Id,
                    gameMode.Key,
                    gameMode.Name,
                    g.Status,
                    g.TimeLimitInSeconds,
                    g.CreatedAt,
                    g.StartedAt,
                    g.Participants.Count,
                    gameMode.MaxPlayers,
                    hostUserId == request.RequestedByUserId,
                    hostUsername,
                    [
                        .. g
                            .Tracks.Select(t => trackNamesById.GetValueOrDefault(t.TrackId))
                            .Where(name => name is not null)
                            .Select(name => name!),
                    ]
                );
            })
            .ToList();

        return Result<IReadOnlyList<MyActiveGameDto>>.Success(results);
    }
}