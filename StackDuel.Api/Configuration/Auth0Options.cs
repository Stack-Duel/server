using StackDuel.Application.Settings;

namespace StackDuel.Api.Settings;

public sealed class Auth0Options : IOption
{
    public static string SectionName => "Auth0";

    public required string Domain { get; init; }
    public required string Audience { get; init; }
}