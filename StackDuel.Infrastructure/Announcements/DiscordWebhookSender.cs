using Discord;
using Discord.Webhook;
using System.Diagnostics.CodeAnalysis;

namespace StackDuel.Infrastructure.Announcements;

internal interface IDiscordWebhookSender
{
    Task SendAsync(Embed embed, CancellationToken cancellationToken = default);
}

/// <summary>
/// Hands the finished embed to Discord.Net. Excluded from coverage: there is no seam below
/// this point, so exercising it means a live webhook call.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class DiscordWebhookSender(DiscordWebhookClient client) : IDiscordWebhookSender
{
    public Task SendAsync(Embed embed, CancellationToken cancellationToken = default) =>
        client.SendMessageAsync(text: null, embeds: [embed], allowedMentions: AllowedMentions.None);
}