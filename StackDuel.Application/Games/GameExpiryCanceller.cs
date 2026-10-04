using Microsoft.Extensions.Logging;
using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;

namespace StackDuel.Application.Games;

internal sealed partial class GameExpiryCanceller(
    IMessagePublisher messagePublisher,
    ILogger<GameExpiryCanceller> logger
) : IGameExpiryCanceller
{
    public async Task CancelIfScheduledAsync(Game game, CancellationToken cancellationToken = default)
    {
        if (game.Status != GameStatus.Completed || game.ScheduledExpirySequenceNumber is not long sequenceNumber)
            return;

        // Best-effort only: not every transport can cancel by handle (RabbitMQ's
        // delayed-message-exchange plugin can't), and even where it can, the message may already
        // be in flight. Either way the GameTimeExpiredMessage consumer will see this game as
        // already-Completed and no-op, so a failed/unsupported cancel here is never a correctness
        // problem — just a missed cost optimization.
        try
        {
            await messagePublisher.CancelScheduledAsync<GameTimeExpiredMessage>(sequenceNumber, cancellationToken);
        }
        catch (NotSupportedException)
        {
            LogCancelNotSupported(game.Id);
        }
        catch (Exception ex)
        {
            LogCancelFailed(ex, game.Id);
        }

        game.ClearScheduledExpiry();
    }

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "Game {GameId} completed early; active transport doesn't support cancelling the scheduled GameTimeExpiredMessage by handle. The consumer will no-op against it when it arrives."
    )]
    private partial void LogCancelNotSupported(Guid gameId);

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Game {GameId} completed early but cancelling its scheduled GameTimeExpiredMessage failed. The consumer will still no-op against it when it arrives."
    )]
    private partial void LogCancelFailed(Exception ex, Guid gameId);
}