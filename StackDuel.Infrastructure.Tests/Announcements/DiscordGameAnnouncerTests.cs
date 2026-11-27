using Discord;
using StackDuel.Domain.Games.Entities;
using StackDuel.Infrastructure.Announcements;

namespace StackDuel.Infrastructure.Tests.Announcements;

public class DiscordGameAnnouncerTests
{
    private static readonly DateTime CreatedAt = new(2026, 11, 27, 12, 0, 0, DateTimeKind.Utc);

    private static string FieldValue(Embed embed, string name) =>
        embed.Fields.Single(field => field.Name == name).Value;

    private static GameMode Mode(string key, string name, int minPlayers, int maxPlayers) =>
        new(key, name, "description", true, minPlayers, maxPlayers, Guid.NewGuid());

    [Fact]
    public void IsWorthAnnouncing_SoloRush_IsFalse()
    {
        Assert.False(DiscordGameAnnouncer.IsWorthAnnouncing(Mode("solo_rush", "Solo Rush", 1, 1)));
    }

    [Fact]
    public void IsWorthAnnouncing_Duel_IsTrue()
    {
        Assert.True(DiscordGameAnnouncer.IsWorthAnnouncing(Mode("duel", "Duel", 2, 2)));
    }

    [Fact]
    public void IsWorthAnnouncing_FreeForAll_IsTrue()
    {
        Assert.True(DiscordGameAnnouncer.IsWorthAnnouncing(Mode("ffa", "FFA", 2, 10)));
    }

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
    public void BuildGameCreatedEmbed_TimeLimitThatIsNotWholeMinutes_ReportsSeconds()
    {
        Embed embed = DiscordGameAnnouncer.BuildGameCreatedEmbed("alice", "Duel", 90, CreatedAt);

        Assert.Equal("90 sec", FieldValue(embed, "Time limit"));
    }
}