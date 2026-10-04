namespace StackDuel.Application.Settings;

public sealed class ConnectionStringOptions : IOption
{
    public static string SectionName => "ConnectionStrings";

    public required string DefaultConnection { get; init; }
}