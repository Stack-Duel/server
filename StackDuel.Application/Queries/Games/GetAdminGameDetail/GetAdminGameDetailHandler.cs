using Ardalis.Result;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Tracks;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Tracks.Entities;

namespace StackDuel.Application.Queries.Games.GetAdminGameDetail;

internal sealed class GetAdminGameDetailHandler(
    IGameReadRepository gameReadRepository,
    IUserReadRepository userReadRepository,
    ITrackReadRepository trackReadRepository
) : IQueryHandler<GetAdminGameDetailQuery, GameStateDto>
{
    public async Task<Result<GameStateDto>> Handle(GetAdminGameDetailQuery request, CancellationToken cancellationToken)
    {
        Game? game = await gameReadRepository.FindGameByIdAsync(request.GameId, cancellationToken);

        if (game is null)
            return Result<GameStateDto>.NotFound();

        GameMode? gameMode = await gameReadRepository.FindGameModeByIdIncludingInactiveAsync(
            game.GameModeId,
            cancellationToken
        );

        if (gameMode is null)
            return Result<GameStateDto>.Error("Game references a mode that no longer exists.");

        IReadOnlyDictionary<Guid, UserDto> userMap = await userReadRepository.FindByIdsAsync(
            game.Participants.Select(p => p.UserId),
            cancellationToken
        );

        IReadOnlyList<Track> tracks = await trackReadRepository.FindByIdsAsync(
            game.Tracks.Select(t => t.TrackId),
            cancellationToken
        );
        Dictionary<Guid, string> trackNamesById = tracks.ToDictionary(t => t.Id, t => t.Name);

        var participants = game
            .Participants.Select(p =>
            {
                userMap.TryGetValue(p.UserId, out var user);

                GameCurrentProblemDto? currentProblem =
                    p.ProblemSession != null ? new GameCurrentProblemDto(p.ProblemSession.CurrentProblemId) : null;

                return new GameParticipantDto(
                    p.UserId,
                    user?.Username ?? string.Empty,
                    user?.ImageUrl,
                    p.SeatNo,
                    p.JoinedAt,
                    p.Score,
                    currentProblem,
                    p.HasForfeited,
                    p.HasFinishedProblems,
                    p.SkipsRemaining
                );
            })
            .ToArray();

        return Result.Success(
            new GameStateDto(
                game.Id,
                game.GameModeId,
                gameMode.Key,
                game.Status,
                game.TimeLimitInSeconds,
                game.CreatedAt,
                game.StartedAt,
                game.EndedAt,
                participants,
                [
                    .. game
                        .Tracks.Select(t => trackNamesById.GetValueOrDefault(t.TrackId))
                        .Where(name => name is not null)
                        .Select(name => name!),
                ],
                game.JoinCode,
                game.SkipsEnabled
            )
        );
    }
}