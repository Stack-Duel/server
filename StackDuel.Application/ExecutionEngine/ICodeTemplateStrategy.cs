using System.Text.Json;

namespace StackDuel.Application.ExecutionEngine;

public sealed record CodeTemplateSourceFile(string Path, string Content);

public sealed record CodeTemplateContext(
    string UserCode,
    string? FunctionName,
    IReadOnlyList<CodeTemplateInput> Inputs,
    IReadOnlyList<CodeTemplateSourceFile>? AdditionalFiles = null
);

public sealed record CodeTemplateInput(string Value, string ValueType);

public sealed record ParsedExecutionOutput(string? UserLogs, string? ActualResult);

public interface ICodeTemplateStrategy
{
    string LanguageName { get; }

    string Render(CodeTemplateContext context);

    byte[]? BuildAdditionalFiles(CodeTemplateContext context) => null;

    ParsedExecutionOutput ParseOutput(string? stdout)
    {
        if (string.IsNullOrEmpty(stdout))
            return new ParsedExecutionOutput(UserLogs: null, ActualResult: null);

        string[] lines = stdout.TrimEnd('\r', '\n').Split(["\r\n", "\r", "\n"], StringSplitOptions.None);

        if (lines.Length == 1)
            return new ParsedExecutionOutput(UserLogs: null, ActualResult: lines[0]);

        string actualResult = lines[^1];
        string[] logLines = lines[..^1];

        string? userLogs = logLines.Length == 0 ? null : string.Join(",", logLines);

        return new ParsedExecutionOutput(
            UserLogs: userLogs,
            ActualResult: string.IsNullOrWhiteSpace(actualResult) ? null : actualResult
        );
    }

    string BuildStdin(IReadOnlyList<CodeTemplateInput> inputs)
    {
        var values = inputs.Select(ToJsonElement).ToArray();
        return JsonSerializer.Serialize(values);
    }

    private static JsonElement ToJsonElement(CodeTemplateInput input)
    {
        try
        {
            return JsonSerializer.Deserialize<JsonElement>(input.Value);
        }
        catch (JsonException)
        {
            return JsonSerializer.SerializeToElement(input.Value);
        }
    }
}