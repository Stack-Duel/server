namespace StackDuel.Application.LanguageServer;

public interface ILanguageServerProcess : IAsyncDisposable
{
    Stream Input { get; }

    Stream Output { get; }

    Task Completion { get; }

    bool HasExited { get; }
}