using StackDuel.Application.Commands.Games.CompleteExpiredGame;
using StackDuel.Application.Games;
using StackDuel.Domain.Games.Entities;
using MediatR;
using Microsoft.Extensions.Logging;

namespace StackDuel.Infrastructure.Jobs.Games;

/// <summary>
/// Backstop for the GameTimeExpiredMessage path. Finds Running games that are past their
/// expiry with no corresponding message ever having arrived (lost message, scheduling failure,
/// broker outage, etc.) and finalizes them via the same CompleteExpiredGameCommand the message
/// consumer uses. Intended to run infrequently — this only exists to catch the rare miss, not
/// to be the primary expiry path.
/// </summary>
public interface IGameExpirySweepService
{
    Task RunAsync(CancellationToken cancellationToken = default);
}

public sealed partial class GameExpirySweepService(
    IGameReadRepository gameReadRepository,
    IMediator mediator,
    ILogger<GameExpirySweepService> logger
) : IGameExpirySweepService
{
    // Only sweep games that are past expiry by more than this — keeps the sweep from racing
    // the normal GameTimeExpiredMessage path for a game that simply hasn't hit its scheduled
    // delivery yet. This job is a backstop for lost/failed messages, not a faster alternative.
    private static readonly TimeSpan GracePeriod = TimeSpan.FromMinutes(5);

    // Upper bound per run so one sweep can't blow up if something has gone badly wrong (e.g.
    // scheduling was broken for hours). Remaining stragglers just get picked up next run.
    private const int MaxBatchSize = 200;

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        LogRunning();

        DateTime cutoffUtc = DateTime.UtcNow - GracePeriod;

        IReadOnlyList<Game> candidates = await gameReadRepository.GetRunningGamesPastExpiryAsync(
            cutoffUtc,
            MaxBatchSize,
            cancellationToken
        );

        if (candidates.Count == 0)
        {
            LogNoneFound();
            return;
        }

        LogFound(candidates.Count);

        int completed = 0;
        int noOp = 0;
        int other = 0;

        foreach (Game game in candidates)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            try
            {
                // StartedAt is guaranteed non-null by the query (Status == Running games always
                // have a StartedAt). RescheduleCount 0 is fine here: this isn't the reschedule
                // loop the message consumer runs — if the handler ever did decide this game
                // isn't actually expired yet (a race with StartedAt), it's simply picked up
                // again on the next sweep rather than being explicitly rescheduled here.
                var result = await mediator.Send(
                    new CompleteExpiredGameCommand(game.Id, game.StartedAt!.Value, RescheduleCount: 0),
                    cancellationToken
                );

                if (!result.IsSuccess)
                {
                    other++;
                    LogCommandFailed(game.Id);
                    continue;
                }

                switch (result.Value.Outcome)
                {
                    case CompleteExpiredGameOutcome.Completed:
                    case CompleteExpiredGameOutcome.NotYetExpired_RescheduleLimitReached_CompletedAnyway:
                        completed++;
                        break;
                    case CompleteExpiredGameOutcome.AlreadyFinalized_NoOp:
                    case CompleteExpiredGameOutcome.Stale_Dropped:
                        noOp++;
                        break;
                    default:
                        other++;
                        break;
                }
            }
            catch (Exception ex)
            {
                other++;
                LogGameSweepError(ex, game.Id);
            }
        }

        LogSummary(completed, noOp, other);
    }

    [LoggerMessage(Level = LogLevel.Debug, Message = "Running game expiry sweep")]
    private partial void LogRunning();

    [LoggerMessage(Level = LogLevel.Debug, Message = "Game expiry sweep found no stale Running games")]
    private partial void LogNoneFound();

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Game expiry sweep found {Count} Running game(s) past expiry with no message ever having finalized them"
    )]
    private partial void LogFound(int count);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "CompleteExpiredGameCommand returned a failure result for game {GameId} during expiry sweep"
    )]
    private partial void LogCommandFailed(Guid gameId);

    [LoggerMessage(Level = LogLevel.Error, Message = "Error finalizing game {GameId} during expiry sweep")]
    private partial void LogGameSweepError(Exception ex, Guid gameId);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Game expiry sweep complete: {Completed} completed, {NoOp} no-op, {Other} other"
    )]
    private partial void LogSummary(int completed, int noOp, int other);
}