using Microsoft.Extensions.Logging;
using StackDuel.Application.Configuration;
using StackDuel.Application.LanguageServer;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace StackDuel.Infrastructure.LanguageServer;

internal sealed class LanguageServerSessionManager(
    IEnumerable<ILanguageServerAdapter> adapters,
    LanguageServerOptions options,
    ILoggerFactory loggerFactory
) : ILanguageServerSessionManager
{
    private static readonly string RootDirectory = Path.Combine(Path.GetTempPath(), "stackduel-lsp");

    private readonly ILogger _logger = loggerFactory.CreateLogger<LanguageServerSessionManager>();
    private readonly ConcurrentDictionary<Guid, InternalSession> _sessions = new();
    private readonly SemaphoreSlim _admissionSemaphore = new(1, 1);

    public async Task<LanguageServerSessionStartResult> StartSessionAsync(
        Guid userId,
        string languageSlug,
        CancellationToken cancellationToken
    )
    {
        if (!options.Enabled)
            return LanguageServerSessionStartResult.Failure(LanguageServerSessionStartError.Disabled);

        ILanguageServerAdapter? adapter = adapters.FirstOrDefault(a =>
            a.LanguageSlug.Equals(languageSlug, StringComparison.OrdinalIgnoreCase)
        );

        if (adapter is null)
            return LanguageServerSessionStartResult.Failure(LanguageServerSessionStartError.UnsupportedLanguage);

        await _admissionSemaphore.WaitAsync(cancellationToken);

        try
        {
            InternalSession? existing = _sessions.Values.FirstOrDefault(s => s.OwnerUserId == userId);
            if (existing is not null)
            {
                _sessions.TryRemove(existing.Id, out _);
                await DisposeSessionAsync(existing);
            }

            if (_sessions.Count >= options.MaxConcurrentSessions)
                return LanguageServerSessionStartResult.Failure(LanguageServerSessionStartError.AtCapacity);

            Guid sessionId = Guid.NewGuid();
            string sessionRootDirectory = Path.Combine(RootDirectory, sessionId.ToString());
            string workspaceDirectory = Path.Combine(sessionRootDirectory, "project");
            Directory.CreateDirectory(workspaceDirectory);

            await adapter.PrepareWorkspaceAsync(workspaceDirectory, cancellationToken);

            LanguageServerProcessStartInfo startInfo = adapter.BuildProcessStartInfo(workspaceDirectory);
            LanguageServerProcess process = LanguageServerProcess.Start(startInfo, _logger);

            await File.WriteAllTextAsync(
                Path.Combine(sessionRootDirectory, ".pid"),
                process.ProcessId.ToString(),
                cancellationToken
            );

            string rootUri = ToFileUri(workspaceDirectory);
            string documentUri = ToFileUri(Path.Combine(workspaceDirectory, adapter.DocumentFileName));
            object? initializationOptions = adapter.BuildInitializationOptions(workspaceDirectory);

            InternalSession session = new(
                sessionId,
                userId,
                languageSlug,
                sessionRootDirectory,
                rootUri,
                documentUri,
                initializationOptions,
                process
            );

            _sessions[sessionId] = session;

            return LanguageServerSessionStartResult.Success(
                new LanguageServerSession(sessionId, userId, languageSlug, rootUri, documentUri, initializationOptions)
            );
        }
        finally
        {
            _admissionSemaphore.Release();
        }
    }

    public LanguageServerSession? TryGetSession(Guid sessionId, Guid userId)
    {
        if (!_sessions.TryGetValue(sessionId, out InternalSession? session) || session.OwnerUserId != userId)
            return null;

        return new LanguageServerSession(
            session.Id,
            session.OwnerUserId,
            session.LanguageSlug,
            session.RootUri,
            session.DocumentUri,
            session.InitializationOptions
        );
    }

    public async Task EndSessionAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        if (_sessions.TryRemove(sessionId, out InternalSession? session))
            await DisposeSessionAsync(session);
    }

    public ILanguageServerProcess? TryAttach(Guid sessionId, Guid userId)
    {
        if (!_sessions.TryGetValue(sessionId, out InternalSession? session) || session.OwnerUserId != userId)
            return null;

        return session.TryMarkAttached() ? session.Process : null;
    }

    public void Detach(Guid sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out InternalSession? session))
            session.MarkDetached();
    }

    public void Touch(Guid sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out InternalSession? session))
            session.Touch();
    }

    public async Task ReapIdleSessionsAsync(TimeSpan idleTimeout, CancellationToken cancellationToken)
    {
        DateTimeOffset cutoff = DateTimeOffset.UtcNow - idleTimeout;

        List<InternalSession> toReap = _sessions
            .Values.Where(s => s.LastActivityUtc < cutoff || s.Process.HasExited)
            .ToList();

        foreach (InternalSession session in toReap)
        {
            if (!_sessions.TryRemove(session.Id, out _))
                continue;

            _logger.LogInformation("Reaping idle language server session {SessionId}", session.Id);
            await DisposeSessionAsync(session);
        }
    }

    public Task SweepOrphanedWorkspacesAsync(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(RootDirectory))
            return Task.CompletedTask;

        foreach (string workspaceDirectory in Directory.EnumerateDirectories(RootDirectory))
        {
            string pidFile = Path.Combine(workspaceDirectory, ".pid");

            if (
                File.Exists(pidFile)
                && int.TryParse(File.ReadAllText(pidFile), out int pid)
                && TryKillIfStillRunning(pid)
            )
            {
                _logger.LogInformation("Killed orphaned language server process {Pid} from a previous run", pid);
            }

            TryDeleteDirectory(workspaceDirectory);
        }

        return Task.CompletedTask;
    }

    private async Task DisposeSessionAsync(InternalSession session)
    {
        await session.Process.DisposeAsync();
        TryDeleteDirectory(session.SessionRootDirectory);
    }

    private void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "Failed to delete language server workspace {Path}", path);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Failed to delete language server workspace {Path}", path);
        }
    }

    private static bool TryKillIfStillRunning(int pid)
    {
        try
        {
            Process process = Process.GetProcessById(pid);
            if (!process.ProcessName.Contains("java", StringComparison.OrdinalIgnoreCase))
                return false;

            process.Kill(entireProcessTree: true);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static string ToFileUri(string path) => new Uri(Path.GetFullPath(path)).AbsoluteUri;

    private sealed class InternalSession(
        Guid id,
        Guid ownerUserId,
        string languageSlug,
        string sessionRootDirectory,
        string rootUri,
        string documentUri,
        object? initializationOptions,
        LanguageServerProcess process
    )
    {
        private int _attached;
        private long _lastActivityTicks = DateTimeOffset.UtcNow.UtcTicks;

        public Guid Id { get; } = id;
        public Guid OwnerUserId { get; } = ownerUserId;
        public string LanguageSlug { get; } = languageSlug;
        public string SessionRootDirectory { get; } = sessionRootDirectory;
        public string RootUri { get; } = rootUri;
        public string DocumentUri { get; } = documentUri;
        public object? InitializationOptions { get; } = initializationOptions;
        public LanguageServerProcess Process { get; } = process;

        public DateTimeOffset LastActivityUtc => new(Interlocked.Read(ref _lastActivityTicks), TimeSpan.Zero);

        public void Touch() => Interlocked.Exchange(ref _lastActivityTicks, DateTimeOffset.UtcNow.UtcTicks);

        public bool TryMarkAttached() => Interlocked.CompareExchange(ref _attached, 1, 0) == 0;

        public void MarkDetached() => Interlocked.Exchange(ref _attached, 0);
    }
}