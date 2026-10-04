using Ardalis.Result;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Tracks;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Tracks.Entities;

namespace StackDuel.Application.Queries.Games.GetGameState;

internal sealed class GetGameStateHandler(
    IGameReadRepository gameReadRepository,
    IGameWriteRepository gameWriteRepository,
    IGameExpiryCanceller gameExpiryCanceller,
    IDomainEventDispatcher domainEventDispatcher,
    IUserReadRepository userReadRepository,
    ITrackReadRepository trackReadRepository
) : IQueryHandler<GetGameStateQuery, GameStateDto>
{
    public async Task<Result<GameStateDto>> Handle(GetGameStateQuery request, CancellationToken cancellationToken)
    {
        Game? game = await gameReadRepository.FindGameByIdAsync(request.GameId, cancellationToken);

        if (game is null)
            return Result<GameStateDto>.NotFound();

        bool isParticipant = game.Participants.Any(p => p.UserId == request.RequestedByUserId);
        if (!isParticipant)
            return Result<GameStateDto>.Forbidden();

        if (game.CompleteIfExpired(DateTime.UtcNow))
        {
            await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
            await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);
        }
        else if (game.CompleteIfEveryoneStopped())
        {
            // Self-healing for the race where two participants stop playing (forfeit/finish) via
            // concurrent requests and each writes before seeing the other's — neither call
            // completes the game, so it's left Running with nobody left to act in it until the
            // next read notices. Early completion (not via the clock), so cancel the now-moot
            // scheduled expiry too, same as Forfeit/CompleteProblem do for their own completions.
            await gameExpiryCanceller.CancelIfScheduledAsync(game, cancellationToken);
            await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
            await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);
        }

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
                userMap.TryGetValue(p.UserId, out UserDto? user);

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

        return Result<GameStateDto>.Success(
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