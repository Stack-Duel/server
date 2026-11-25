using DbUp.Engine.Output;

namespace StackDuel.IntegrationTests.Infrastructure;

internal sealed class SilentUpgradeLog : IUpgradeLog
{
    public void LogTrace(string format, params object[] args) { }

    public void LogDebug(string format, params object[] args) { }

    public void LogInformation(string format, params object[] args) { }

    public void LogWarning(string format, params object[] args) { }

    public void LogError(string format, params object[] args) { }

    public void LogError(Exception ex, string format, params object[] args) { }
}