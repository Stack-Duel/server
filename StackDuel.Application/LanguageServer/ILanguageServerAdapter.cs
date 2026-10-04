namespace StackDuel.Application.LanguageServer;

public interface ILanguageServerAdapter
{
    string LanguageSlug { get; }

    string DocumentFileName { get; }

    Task PrepareWorkspaceAsync(string workspaceDirectory, CancellationToken cancellationToken);

    LanguageServerProcessStartInfo BuildProcessStartInfo(string workspaceDirectory);

    object? BuildInitializationOptions(string workspaceDirectory);
}

public sealed record LanguageServerProcessStartInfo(
    string FileName,
    IReadOnlyList<string> Arguments,
    string WorkingDirectory
);