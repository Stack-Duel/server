namespace StackDuel.Application.LanguageServer;

public sealed record LanguageServerSession(
    Guid Id,
    Guid OwnerUserId,
    string LanguageSlug,
    string RootUri,
    string DocumentUri,
    object? InitializationOptions
);

public enum LanguageServerSessionStartError
{
    Disabled,
    UnsupportedLanguage,
    AtCapacity,
}

public sealed record LanguageServerSessionStartResult(
    LanguageServerSession? Session,
    LanguageServerSessionStartError? Error
)
{
    public static LanguageServerSessionStartResult Success(LanguageServerSession session) => new(session, null);

    public static LanguageServerSessionStartResult Failure(LanguageServerSessionStartError error) => new(null, error);
}

public interface ILanguageServerSessionManager
{
    Task<LanguageServerSessionStartResult> StartSessionAsync(
        Guid userId,
        string languageSlug,
        CancellationToken cancellationToken
    );

    LanguageServerSession? TryGetSession(Guid sessionId, Guid userId);

    Task EndSessionAsync(Guid sessionId, CancellationToken cancellationToken);

    ILanguageServerProcess? TryAttach(Guid sessionId, Guid userId);

    void Detach(Guid sessionId);

    void Touch(Guid sessionId);

    Task ReapIdleSessionsAsync(TimeSpan idleTimeout, CancellationToken cancellationToken);

    Task SweepOrphanedWorkspacesAsync(CancellationToken cancellationToken);
}