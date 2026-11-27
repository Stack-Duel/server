using Microsoft.Extensions.Logging.Abstractions;
using StackDuel.Infrastructure.Announcements;

namespace StackDuel.Infrastructure.Tests.Announcements;

public class LoggingGameAnnouncerTests
{
    [Fact]
    public async Task AnnounceGameCreatedAsync_CompletesWithoutAnOutboundChannel()
    {
        LoggingGameAnnouncer announcer = new(NullLogger<LoggingGameAnnouncer>.Instance);

        await announcer.AnnounceGameCreatedAsync(Guid.NewGuid());
    }
}