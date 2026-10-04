using Ardalis.Result;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Pagination;
using StackDuel.Application.Tracks;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Tracks.Entities;

namespace StackDuel.Application.Queries.Games.GetOpenGames;

internal sealed class GetOpenGamesHandler(
    IGameReadRepository gameReadRepository,
    IUserReadRepository userReadRepository,
    ITrackReadRepository trackReadRepository
) : IQueryHandler<GetOpenGamesQuery, PageResult<GameLobbySummaryDto>>
{
    public async Task<Result<PageResult<GameLobbySummaryDto>>> Handle(
        GetOpenGamesQuery request,
        CancellationToken cancellationToken
    )
    {
        PageResult<Game> page = await gameReadRepository.GetPendingGamesAsync(
            request.GameModeKey,
            request.PaginationRequest,
            cancellationToken
        );

        Dictionary<Guid, GameMode> gameModesById = [];
        foreach (Guid gameModeId in page.Results.Select(g => g.GameModeId).Distinct())
        {
            GameMode? gameMode = await gameReadRepository.FindGameModeByIdAsync(gameModeId, cancellationToken);
            if (gameMode is not null)
                gameModesById[gameModeId] = gameMode;
        }

        IReadOnlyDictionary<Guid, UserDto> hostsByUserId = await userReadRepository.FindByIdsAsync(
            page.Results.Select(g => g.HostUserId).Where(id => id is not null).Select(id => id!.Value),
            cancellationToken
        );

        IReadOnlyList<Track> tracks = await trackReadRepository.FindByIdsAsync(
            page.Results.SelectMany(g => g.Tracks.Select(t => t.TrackId)).Distinct(),
            cancellationToken
        );
        Dictionary<Guid, string> trackNamesById = tracks.ToDictionary(t => t.Id, t => t.Name);

        var summaries = page
            .Results.Where(g => gameModesById.ContainsKey(g.GameModeId))
            .Select(g =>
            {
                GameMode gameMode = gameModesById[g.GameModeId];
                Guid? hostUserId = g.HostUserId;
                string hostUsername =
                    hostUserId is Guid id && hostsByUserId.TryGetValue(id, out UserDto? host)
                        ? host.Username
                        : string.Empty;

                return new GameLobbySummaryDto(
                    g.Id,
                    gameMode.Key,
                    gameMode.Name,
                    g.TimeLimitInSeconds,
                    g.CreatedAt,
                    gameMode.MinPlayers,
                    gameMode.MaxPlayers,
                    g.Participants.Count,
                    hostUsername,
                    request.RequestedByUserId is Guid requestedByUserId && hostUserId == requestedByUserId,
                    request.RequestedByUserId is Guid participantUserId
                        && g.Participants.Any(p => p.UserId == participantUserId),
                    g.Tracks.Select(t => trackNamesById.GetValueOrDefault(t.TrackId))
                        .Where(name => name is not null)
                        .Select(name => name!)
                        .ToList()
                );
            })
            .ToList();

        return Result<PageResult<GameLobbySummaryDto>>.Success(
            new PageResult<GameLobbySummaryDto>
            {
                Results = summaries,
                Total = page.Total,
                Page = page.Page,
                Size = page.Size,
            }
        );
    }
}