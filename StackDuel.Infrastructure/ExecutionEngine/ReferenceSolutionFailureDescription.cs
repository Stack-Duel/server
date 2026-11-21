using StackDuel.Application.ExecutionEngine;

namespace StackDuel.Infrastructure.ExecutionEngine;

internal static class ReferenceSolutionFailureDescription
{
    private const int MaxDiagnosticLength = 300;
    private const int MaxValueLength = 120;
    private const string NoOutput = "no output";

    private static readonly string[] NoiseMarkers =
    [
        "[MODULE_TYPELESS_PACKAGE_JSON]",
        "--trace-warnings",
        "DeprecationWarning",
    ];

    public static string Describe(string caseName, ExecutionEngineResult? result, string? expected, string? actual)
    {
        string status = result is null ? "no result" : Humanize(result.Status);
        string summary =
            $"Case '{caseName}': {status}. Expected {Shorten(expected, MaxValueLength)}, "
            + $"got {Shorten(actual, MaxValueLength)}";

        string? diagnostic = FirstMeaningfulLine(result?.CompileOutput) ?? FirstMeaningfulLine(result?.Stderr);

        return diagnostic is null ? $"{summary}." : $"{summary}. {diagnostic}";
    }

    private static string Humanize(ExecutionEngineResultStatus status) =>
        status switch
        {
            ExecutionEngineResultStatus.WrongAnswer => "wrong answer",
            ExecutionEngineResultStatus.CompilationError => "compile error",
            ExecutionEngineResultStatus.RuntimeError => "runtime error",
            ExecutionEngineResultStatus.TimeLimitExceeded => "timed out",
            _ => status.ToString(),
        };

    private static string Shorten(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return NoOutput;

        string collapsed = value.Trim();

        return collapsed.Length > maxLength ? collapsed[..maxLength] + "..." : collapsed;
    }

    private static string? FirstMeaningfulLine(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
            return null;

        string[] candidates =
        [
            .. output
                .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(IsMeaningful),
        ];

        string? errorLine = candidates.FirstOrDefault(LooksLikeAnError);
        string? chosen = errorLine ?? candidates.FirstOrDefault();

        return chosen is null ? null : Shorten(chosen, MaxDiagnosticLength);
    }

    private static bool LooksLikeAnError(string line) =>
        line.Contains("Error", StringComparison.Ordinal)
        || line.Contains("Exception", StringComparison.Ordinal)
        || line.Contains("error:", StringComparison.OrdinalIgnoreCase);

    private static bool IsMeaningful(string line) =>
        line.Length > 0
        && line.Trim('^').Trim().Length > 0
        && !line.StartsWith("at ", StringComparison.Ordinal)
        && !line.StartsWith("(node:", StringComparison.Ordinal)
        && !line.StartsWith("(Use ", StringComparison.Ordinal)
        && !line.StartsWith("Warning:", StringComparison.Ordinal)
        && !NoiseMarkers.Any(marker => line.Contains(marker, StringComparison.Ordinal));
}