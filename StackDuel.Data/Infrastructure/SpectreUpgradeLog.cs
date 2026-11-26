using DbUp.Engine.Output;
using Spectre.Console;

namespace StackDuel.Data.Infrastructure;

internal sealed class SpectreUpgradeLog(bool verbose) : IUpgradeLog
{
    public void LogTrace(string format, params object[] args) => WriteIfVerbose("grey39", format, args);

    public void LogDebug(string format, params object[] args) => WriteIfVerbose("grey39", format, args);

    public void LogInformation(string format, params object[] args) => WriteIfVerbose("grey70", format, args);

    public void LogWarning(string format, params object[] args) => Write("yellow", format, args);

    public void LogError(string format, params object[] args) => Write("red", format, args);

    public void LogError(Exception ex, string format, params object[] args)
    {
        Write("red", format, args);
        AnsiConsole.WriteException(ex, ExceptionFormats.ShortenPaths);
    }

    private void WriteIfVerbose(string color, string format, object[] args)
    {
        if (verbose)
            Write(color, format, args);
    }

    private static void Write(string color, string format, object[] args) =>
        AnsiConsole.MarkupLine($"[{color}]{Markup.Escape(LogTemplate.Render(format, args))}[/]");
}