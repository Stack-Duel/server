using Microsoft.Extensions.Logging;

namespace StackDuel.Application.Tests.Support;

/// <summary>
/// Captures the levels a subject logs at. Hand-written rather than mocked because Moq proxies
/// <see cref="ILogger{TCategoryName}"/> through DynamicProxy, which cannot see internal category
/// types, and most subjects under test here are internal.
/// </summary>
internal sealed class RecordingLogger<T> : ILogger<T>
{
    private readonly List<LogLevel> _levels = [];

    public int WarningCount => _levels.Count(level => level == LogLevel.Warning);

    public int ErrorCount => _levels.Count(level => level == LogLevel.Error);

    public int WarningOrErrorCount => WarningCount + ErrorCount;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter
    ) => _levels.Add(logLevel);
}
