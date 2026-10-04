using StackDuel.Application.Settings;

namespace StackDuel.Application.Configuration;

public sealed class ExecutionEngineOptions : IOption
{
    public static string SectionName => "ExecutionEngines";

    public Judge0Options Judge0 { get; init; } = new();

    public JsxTranspilerOptions JsxTranspiler { get; init; } = new();
}

public sealed class JsxTranspilerOptions
{
    public string EsbuildPath { get; init; } = "esbuild";
    public int TimeoutSeconds { get; init; } = 10;
}

public sealed class Judge0Options
{
    public bool Enabled { get; init; } = true;
    public bool RunWorker { get; init; } = true;
    public string BaseUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string Host { get; init; } = string.Empty;
    public bool ShouldWait { get; init; } = false;
    public bool IsEncoded { get; init; } = true;
    public int DefaultTimeoutInSeconds { get; init; } = 10;
    public bool UseCallback { get; init; } = false;
    public string CallbackSecret { get; init; } = string.Empty;
    public string PublicBaseUrl { get; init; } = string.Empty;
}