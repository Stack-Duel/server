using Ardalis.Result;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
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
    IGameSubmissionProgressApplier gameSubmissionProgressApplier,
    IDomainEventDispatcher domainEventDispatcher,
    IUserReadRepository userReadRepository,
    ITrackReadRepository trackReadRepository,
    ISubmissionReadRepository submissionReadRepository
) : IQueryHandler<GetGameStateQuery, GameStateDto>
{
    /// <summary>
    /// Persists a completion reached by every participant stopping rather than by the clock, so the
    /// now-moot scheduled expiry is cancelled alongside it.
    /// </summary>
    private async Task PersistEarlyCompletionAndCancelScheduledExpiryAsync(
        Game game,
        CancellationToken cancellationToken
    )
    {
        await gameExpiryCanceller.CancelIfScheduledAsync(game, cancellationToken);
        await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
        await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);
    }

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
            await PersistEarlyCompletionAndCancelScheduledExpiryAsync(game, cancellationToken);
        }

        IReadOnlyList<GameProblemSubmissionDto> latestSubmissions =
            await submissionReadRepository.GetLatestGameProblemSubmissionsAsync(request.GameId, cancellationToken);

        GamePendingProgressOutcome pendingProgress = await gameSubmissionProgressApplier.ApplyPendingAsync(
            game,
            latestSubmissions,
            cancellationToken
        );

        if (pendingProgress == GamePendingProgressOutcome.ConflictedNeedsReload)
        {
            game = await gameReadRepository.FindGameByIdAsync(request.GameId, cancellationToken);
            if (game is null)
                return Result<GameStateDto>.NotFound();
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

        GameProblemSubmissionStateDto? OwnLatestSubmissionOnCurrentProblem(GameParticipant participant)
        {
            if (participant.UserId != request.RequestedByUserId || participant.ProblemSession is null)
                return null;

            Guid currentProblemId = participant.ProblemSession.CurrentProblemId;

            GameProblemSubmissionDto? latest = latestSubmissions.FirstOrDefault(s =>
                s.UserId == participant.UserId && s.ProblemId == currentProblemId
            );

            return latest is null
                ? null
                : new GameProblemSubmissionStateDto(latest.SubmissionId, latest.Status, latest.CreatedAt);
        }

        var participants = game
            .Participants.Select(p =>
            {
                userMap.TryGetValue(p.UserId, out UserDto? user);

                GameCurrentProblemDto? currentProblem =
                    p.ProblemSession != null
                        ? new GameCurrentProblemDto(
                            p.ProblemSession.CurrentProblemId,
                            OwnLatestSubmissionOnCurrentProblem(p)
                        )
                        : null;

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