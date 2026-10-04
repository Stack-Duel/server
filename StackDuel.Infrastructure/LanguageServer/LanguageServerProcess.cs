using StackDuel.Application.LanguageServer;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Text;

namespace StackDuel.Infrastructure.LanguageServer;

internal sealed class LanguageServerProcess : ILanguageServerProcess
{
    private static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false);

    private readonly Process _process;

    private LanguageServerProcess(Process process, ILogger logger)
    {
        _process = process;
        Completion = process.WaitForExitAsync();
        _ = DrainStandardErrorAsync(process, logger);
    }

    public static LanguageServerProcess Start(LanguageServerProcessStartInfo startInfo, ILogger logger)
    {
        ProcessStartInfo psi = new(startInfo.FileName)
        {
            WorkingDirectory = startInfo.WorkingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Utf8NoBom,
            StandardInputEncoding = Utf8NoBom,
        };

        foreach (string argument in startInfo.Arguments)
            psi.ArgumentList.Add(argument);

        Process process =
            Process.Start(psi)
            ?? throw new InvalidOperationException($"Failed to start process '{startInfo.FileName}'.");

        return new LanguageServerProcess(process, logger);
    }

    public Stream Input => _process.StandardInput.BaseStream;

    public Stream Output => _process.StandardOutput.BaseStream;

    public Task Completion { get; }

    public bool HasExited => _process.HasExited;

    public int ProcessId => _process.Id;

    public async ValueTask DisposeAsync()
    {
        if (!_process.HasExited)
        {
            try
            {
                _process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException) { }
        }

        _process.Dispose();
        await Task.CompletedTask;
    }

    private static async Task DrainStandardErrorAsync(Process process, ILogger logger)
    {
        try
        {
            string? line;
            while ((line = await process.StandardError.ReadLineAsync()) is not null)
                logger.LogDebug("Language server stderr: {Line}", line);
        }
        catch (Exception ex) when (ex is IOException or ObjectDisposedException) { }
    }
}