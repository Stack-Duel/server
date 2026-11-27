using Microsoft.Extensions.Logging;
using StackDuel.Application.Announcements;

namespace StackDuel.Infrastructure.Announcements;

internal sealed partial class LoggingGameAnnouncer(ILogger<LoggingGameAnnouncer> logger) : IGameAnnouncer
{
    public Task AnnounceGameCreatedAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        LogAnnouncementSkipped(gameId);

        return Task.CompletedTask;
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Game {GameId} reached the announcer, but no outbound channel is configured yet."
    )]
    private partial void LogAnnouncementSkipped(Guid gameId);
}