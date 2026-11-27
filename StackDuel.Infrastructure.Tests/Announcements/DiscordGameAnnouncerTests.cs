using Discord;
using StackDuel.Infrastructure.Announcements;

namespace StackDuel.Infrastructure.Tests.Announcements;

public class DiscordGameAnnouncerTests
{
    private static readonly DateTime CreatedAt = new(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc);

    private static string FieldValue(Embed embed, string name) =>
        embed.Fields.Single(field => field.Name == name).Value;

    [Fact]
    public void BuildGameCreatedEmbed_IncludesTheHostModeAndTimeLimit()
    {
        Embed embed = DiscordGameAnnouncer.BuildGameCreatedEmbed("alice", "Duel", 600, CreatedAt);

        Assert.Equal("New game created", embed.Title);
        Assert.Equal("alice", FieldValue(embed, "Host"));
        Assert.Equal("Duel", FieldValue(embed, "Mode"));
        Assert.Equal("10 min", FieldValue(embed, "Time limit"));
        Assert.Equal(CreatedAt, embed.Timestamp!.Value.UtcDateTime);
    }

    [Fact]
    public void BuildGameCreatedEmbed_UnknownGameMode_FallsBackRatherThanShowingNothing()
    {
        Embed embed = DiscordGameAnnouncer.BuildGameCreatedEmbed("alice", null, 600, CreatedAt);

        Assert.Equal("an unknown mode", FieldValue(embed, "Mode"));
    }

    [Fact]
    public void BuildGameCreatedEmbed_TimeLimitThatIsNotWholeMinutes_ReportsSeconds()
    {
        Embed embed = DiscordGameAnnouncer.BuildGameCreatedEmbed("alice", "Duel", 90, CreatedAt);

        Assert.Equal("90 sec", FieldValue(embed, "Time limit"));
    }
}