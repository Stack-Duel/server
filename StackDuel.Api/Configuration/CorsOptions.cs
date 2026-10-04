using StackDuel.Application.Settings;

namespace StackDuel.Api.Settings;

public sealed class CorsOptions : IOption
{
    public static string SectionName => "Cors";

    public string[] AllowedOrigins { get; init; } = [];
}