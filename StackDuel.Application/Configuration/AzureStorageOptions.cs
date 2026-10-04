using StackDuel.Application.Settings;

namespace StackDuel.Application.Configuration;

public sealed class AzureStorageOptions : IOption
{
    public static string SectionName => "AzureStorage";

    public string ConnectionString { get; init; } = string.Empty;

    public string AvatarContainerName { get; init; } = "avatars";

    public bool AutoCreateContainer { get; init; } = false;
}