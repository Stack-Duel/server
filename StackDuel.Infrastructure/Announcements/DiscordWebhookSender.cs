using Discord;
using Discord.Webhook;

namespace StackDuel.Infrastructure.Announcements;

internal interface IDiscordWebhookSender
{
    Task SendAsync(Embed embed, CancellationToken cancellationToken = default);
}

internal sealed class DiscordWebhookSender(DiscordWebhookClient client) : IDiscordWebhookSender
{
    public Task SendAsync(Embed embed, CancellationToken cancellationToken = default) =>
        client.SendMessageAsync(text: null, embeds: [embed], allowedMentions: AllowedMentions.None);
}