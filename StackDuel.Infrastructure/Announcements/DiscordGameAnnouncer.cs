using Discord;
using Discord.Webhook;
using Microsoft.Extensions.Logging;
using StackDuel.Application.Announcements;
using StackDuel.Application.Games;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games.Entities;
using GameEntity = StackDuel.Domain.Games.Entities.Game;

namespace StackDuel.Infrastructure.Announcements;

internal sealed partial class DiscordGameAnnouncer(
    DiscordWebhookClient webhookClient,
    IGameReadRepository gameReadRepository,
    IUserReadRepository userReadRepository,
    ILogger<DiscordGameAnnouncer> logger
) : IGameAnnouncer
{
    private const string UnknownHost = "someone";
    private const string UnknownGameMode = "an unknown mode";

    public async Task AnnounceGameCreatedAsync(Guid gameId, CancellationToken cancellationToken = default)
    {
        GameEntity? game = await gameReadRepository.FindGameByIdAsync(gameId, cancellationToken);
        if (game is null)
        {
            LogGameMissing(gameId);
            return;
        }

        GameMode? gameMode = await gameReadRepository.FindGameModeByIdIncludingInactiveAsync(
            game.GameModeId,
            cancellationToken
        );

        string hostUsername = await ResolveHostUsernameAsync(game.HostUserId, cancellationToken);

        Embed embed = BuildGameCreatedEmbed(hostUsername, gameMode?.Name, game.TimeLimitInSeconds, game.CreatedAt);

        await webhookClient.SendMessageAsync(text: null, embeds: [embed], allowedMentions: AllowedMentions.None);
    }

    internal static Embed BuildGameCreatedEmbed(
        string hostUsername,
        string? gameModeName,
        int timeLimitInSeconds,
        DateTime createdAt
    ) =>
        new EmbedBuilder()
            .WithTitle("New game created")
            .WithColor(Color.Blue)
            .AddField("Host", hostUsername, inline: true)
            .AddField("Mode", gameModeName ?? UnknownGameMode, inline: true)
            .AddField("Time limit", FormatTimeLimit(timeLimitInSeconds), inline: true)
            .WithTimestamp(createdAt)
            .Build();

    private async Task<string> ResolveHostUsernameAsync(Guid? hostUserId, CancellationToken cancellationToken)
    {
        if (hostUserId is not Guid id)
            return UnknownHost;

        IReadOnlyDictionary<Guid, UserDto> hosts = await userReadRepository.FindByIdsAsync([id], cancellationToken);

        return hosts.TryGetValue(id, out UserDto? host) ? host.Username : UnknownHost;
    }

    private static string FormatTimeLimit(int timeLimitInSeconds) =>
        timeLimitInSeconds % 60 == 0
            ? $"{timeLimitInSeconds / 60} min"
            : $"{timeLimitInSeconds} sec";

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Cannot announce game {GameId}; it no longer exists."
    )]
    private partial void LogGameMissing(Guid gameId);
}