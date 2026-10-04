using StackDuel.Application.Settings;

namespace StackDuel.Application.Configuration;

public sealed class LanguageServerOptions : IOption
{
    public static string SectionName => "LanguageServer";

    public bool Enabled { get; init; } = false;
    public int MaxConcurrentSessions { get; init; } = 10;
    public int IdleTimeoutMinutes { get; init; } = 10;
    public JavaLanguageServerOptions Java { get; init; } = new();
}

public sealed class JavaLanguageServerOptions
{
    public string JdtlsLauncherJarPath { get; init; } = string.Empty;
    public string JdtlsConfigDirectory { get; init; } = string.Empty;
    public string JdtlsRuntimeJavaHome { get; init; } = string.Empty;
    public string JavaHome { get; init; } = string.Empty;
}