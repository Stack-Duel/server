using StackDuel.Application.Settings;

namespace StackDuel.Application.Configuration;

public sealed class DiscordOptions : IOption
{
    public static string SectionName => "Discord";

    public string WebhookUrl { get; init; } = string.Empty;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(WebhookUrl);
}