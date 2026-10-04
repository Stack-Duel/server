using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace StackDuel.Application.Events.Games;

/// <summary>
/// Feeds every completed game's final scores into the (gameModeId, timeLimitInSeconds)
/// leaderboard, regardless of which path finalized the game (normal expiry, sweep-job backstop,
/// or forfeit) — mirrors how GameCompletedDomainEventHandler uses GameCompletedDomainEvent as the
/// single trigger point for the notification side.
/// </summary>
internal sealed partial class RecordLeaderboardScoresOnGameCompletedHandler(
    IGameReadRepository gameReadRepository,
    ILeaderboardWriteRepository leaderboardWriteRepository,
    ILogger<RecordLeaderboardScoresOnGameCompletedHandler> logger
) : INotificationHandler<DomainEventNotification<GameCompletedDomainEvent>>
{
    public async Task Handle(
        DomainEventNotification<GameCompletedDomainEvent> notification,
        CancellationToken cancellationToken
    )
    {
        var e = notification.DomainEvent;

        try
        {
            Game? game = await gameReadRepository.FindGameByIdAsync(e.GameId, cancellationToken);
            if (game is null)
                return;

            Leaderboard? leaderboard = await leaderboardWriteRepository.FindByGameModeAndTimeLimitAsync(
                game.GameModeId,
                game.TimeLimitInSeconds,
                cancellationToken
            );

            if (leaderboard is null)
            {
                leaderboard = new Leaderboard(game.GameModeId, game.TimeLimitInSeconds);
                foreach (GameParticipant participant in game.Participants)
                    leaderboard.RecordScore(participant.UserId, participant.Score);

                await leaderboardWriteRepository.AddAsync(leaderboard, cancellationToken);
                return;
            }

            foreach (GameParticipant participant in game.Participants)
                leaderboard.RecordScore(participant.UserId, participant.Score);

            await leaderboardWriteRepository.SaveChangesAsync(leaderboard, cancellationToken);
        }
        catch (Exception ex)
        {
            // Leaderboard standings are a derived projection, not the source of truth for the
            // game itself — a failure here must never fault the command that finalized the game.
            LogUpdateFailed(ex, e.GameId);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Failed to update leaderboard for completed game {GameId}; standings may be stale until the next completed game in that mode."
    )]
    private partial void LogUpdateFailed(Exception ex, Guid gameId);
}