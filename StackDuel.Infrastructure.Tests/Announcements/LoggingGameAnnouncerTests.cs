using Microsoft.Extensions.Logging;
using Moq;
using StackDuel.Infrastructure.Announcements;

namespace StackDuel.Infrastructure.Tests.Announcements;

public class LoggingGameAnnouncerTests
{
    private readonly RecordingLogger<LoggingGameAnnouncer> _logger = new();

    [Fact]
    public async Task AnnounceGameCreatedAsync_CompletesWithoutAnOutboundChannel()
    {
        LoggingGameAnnouncer announcer = new(_logger);
        Guid gameId = Guid.NewGuid();

        await announcer.AnnounceGameCreatedAsync(gameId);

        var log = Assert.Single(_logger.Entries);
        Assert.Equal(LogLevel.Information, log.Level);
        Assert.Equal(
            $"Game {gameId} reached the announcer, but no outbound channel is configured yet.",
            log.Message);
    }

    private sealed class RecordingLogger<T> : ILogger<T>
    {
        public List<(LogLevel Level, string Message)> Entries { get; } = [];

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Entries.Add((logLevel, formatter(state, exception)));
        }
    }
}