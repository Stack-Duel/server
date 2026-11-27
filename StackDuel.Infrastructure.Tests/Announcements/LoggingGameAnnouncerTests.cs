using Castle.Core.Logging;
using Microsoft.Extensions.Logging;
using Moq;
using StackDuel.Infrastructure.Announcements;

namespace StackDuel.Infrastructure.Tests.Announcements;

public class LoggingGameAnnouncerTests
{
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<LoggingGameAnnouncer>> _logger = new();

    [Fact]
    public async Task AnnounceGameCreatedAsync_CompletesWithoutAnOutboundChannel()
    {
        LoggingGameAnnouncer announcer = new(_logger.Object);

        await announcer.AnnounceGameCreatedAsync(Guid.NewGuid());

        _logger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("reached the announcer")),
                null,
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()
            ),
            Times.Once
        );
    }
}