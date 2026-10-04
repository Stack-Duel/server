using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using Ardalis.Result;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace StackDuel.Application.Commands.Games.CompleteExpiredGame;

internal sealed partial class CompleteExpiredGameHandler(
    IGameReadRepository gameReadRepository,
    IGameWriteRepository gameWriteRepository,
    IValidator<CompleteExpiredGameCommand> validator,
    IDomainEventDispatcher domainEventDispatcher,
    ILogger<CompleteExpiredGameHandler> logger
) : AbstractCommandHandler<CompleteExpiredGameCommand, CompleteExpiredGameResult>(validator)
{
    private static readonly TimeSpan MaxPlausibleDelay = TimeSpan.FromHours(6);

    protected override async Task<Result<CompleteExpiredGameResult>> HandleValidated(
        CompleteExpiredGameCommand request,
        CancellationToken cancellationToken
    )
    {
        Game? game = await gameReadRepository.FindGameByIdAsync(request.GameId, cancellationToken);

        if (game is null)
            return Result<CompleteExpiredGameResult>.Success(
                new CompleteExpiredGameResult(CompleteExpiredGameOutcome.Stale_Dropped)
            );

        if (game.Status != GameStatus.Running)
        {
            LogAlreadyFinalized(game.Id, game.Status);
            return Result<CompleteExpiredGameResult>.Success(
                new CompleteExpiredGameResult(CompleteExpiredGameOutcome.AlreadyFinalized_NoOp)
            );
        }

        if (game.StartedAt is null || game.StartedAt.Value != request.ExpectedStartedAt)
        {
            LogStale(game.Id, request.ExpectedStartedAt, game.StartedAt);
            return Result<CompleteExpiredGameResult>.Success(
                new CompleteExpiredGameResult(CompleteExpiredGameOutcome.Stale_Dropped)
            );
        }

        DateTime actualEndTimeUtc = game.StartedAt.Value.AddSeconds(game.TimeLimitInSeconds);
        DateTime nowUtc = DateTime.UtcNow;

        if (nowUtc < actualEndTimeUtc)
        {
            TimeSpan remaining = actualEndTimeUtc - nowUtc;

            if (request.RescheduleCount >= Messaging.Messages.GameTimeExpiredMessage.MaxReschedules)
            {
                LogRescheduleLimitReached(game.Id, request.RescheduleCount);
                game.Complete();
                await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
                await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);
                return Result<CompleteExpiredGameResult>.Success(
                    new CompleteExpiredGameResult(
                        CompleteExpiredGameOutcome.NotYetExpired_RescheduleLimitReached_CompletedAnyway
                    )
                );
            }

            if (remaining > MaxPlausibleDelay)
            {
                LogImplausibleDelay(game.Id, remaining);
                return Result<CompleteExpiredGameResult>.Success(
                    new CompleteExpiredGameResult(CompleteExpiredGameOutcome.NotYetExpired_ImplausibleDelay_Dropped)
                );
            }

            return Result<CompleteExpiredGameResult>.Success(
                new CompleteExpiredGameResult(
                    CompleteExpiredGameOutcome.NotYetExpired_Rescheduled,
                    RescheduleForUtc: actualEndTimeUtc
                )
            );
        }

        game.Complete();
        await gameWriteRepository.SaveChangesAsync(game, cancellationToken);
        await domainEventDispatcher.DispatchAsync(game.PopDomainEvents(), cancellationToken);

        return Result<CompleteExpiredGameResult>.Success(
            new CompleteExpiredGameResult(CompleteExpiredGameOutcome.Completed)
        );
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Game {GameId} already {Status}; GameTimeExpiredMessage is a no-op."
    )]
    private partial void LogAlreadyFinalized(Guid gameId, GameStatus status);

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Game {GameId} StartedAt has changed since this message was scheduled (expected {ExpectedStartedAt}, actual {ActualStartedAt}); dropping stale message."
    )]
    private partial void LogStale(Guid gameId, DateTime expectedStartedAt, DateTime? actualStartedAt);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Game {GameId} reached reschedule limit ({RescheduleCount}); completing anyway instead of rescheduling again."
    )]
    private partial void LogRescheduleLimitReached(Guid gameId, int rescheduleCount);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Game {GameId} implausible remaining delay ({Remaining}) — StartedAt/TimeLimitInSeconds may be corrupted. Refusing to reschedule."
    )]
    private partial void LogImplausibleDelay(Guid gameId, TimeSpan remaining);
}