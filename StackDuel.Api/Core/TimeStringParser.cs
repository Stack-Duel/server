namespace StackDuel.Api.Core;

public static class TimeStringParser
{
    /// Parses shorthand time strings into seconds and TimeSpan.
    /// Supports: "30s", "5m", "2h"
    public static int ToSeconds(string input)
    {
        input = input.Trim().ToLowerInvariant();

        if (input.EndsWith('s') && int.TryParse(input[..^1], out int secs))
            return secs;

        if (input.EndsWith('m') && int.TryParse(input[..^1], out int mins))
            return mins * 60;

        if (input.EndsWith('h') && int.TryParse(input[..^1], out int hrs))
            return hrs * 3600;

        throw new ArgumentException($"Unrecognised time format '{input}'. Use '30s', '5m', or '2h'.");
    }

    public static TimeSpan ToTimeSpan(string input) => TimeSpan.FromSeconds(ToSeconds(input));
}