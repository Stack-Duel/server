using StackDuel.Application.Configuration;
using StackDuel.Application.ExecutionEngine;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

public sealed class JsxTranspileException(string message) : Exception(message);

internal sealed class EsbuildJsxTranspiler(JsxTranspilerOptions options) : IJsxTranspiler
{
    public string Transpile(string source)
    {
        bool isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        // Prefer the native esbuild.exe vendored alongside the app (Tools/esbuild.exe) —
        // deployed Windows hosts (e.g. Azure App Service) have no Node/npm available to
        // provide a global "esbuild", so this is the only copy that's guaranteed present.
        string? vendoredPath = isWindows ? Path.Combine(AppContext.BaseDirectory, "Tools", "esbuild.exe") : null;
        bool useVendored = vendoredPath is not null && File.Exists(vendoredPath);

        // A bare "esbuild" resolves to esbuild.cmd when installed via `npm install -g`
        // on Windows, and CreateProcess (what Process.Start calls with
        // UseShellExecute=false) cannot launch .cmd files directly — only cmd.exe's own
        // PATHEXT-aware search can. Routing through cmd.exe /c on Windows only affects
        // resolution, not behavior: it still runs whatever EsbuildPath points to,
        // including an absolute path to esbuild.exe. The vendored copy is a native .exe,
        // so it skips this entirely and launches directly.
        bool needsCmdWrapper = isWindows && !useVendored;
        string esbuildPath = useVendored ? vendoredPath! : options.EsbuildPath;

        ProcessStartInfo startInfo = new()
        {
            FileName = needsCmdWrapper ? "cmd.exe" : esbuildPath,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            StandardInputEncoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        if (needsCmdWrapper)
        {
            startInfo.ArgumentList.Add("/c");
            startInfo.ArgumentList.Add(esbuildPath);
        }
        startInfo.ArgumentList.Add("--loader=jsx");
        startInfo.ArgumentList.Add("--jsx=transform");
        startInfo.ArgumentList.Add("--jsx-factory=React.createElement");

        using Process process = new() { StartInfo = startInfo };
        process.Start();

        process.StandardInput.Write(source);
        process.StandardInput.Close();

        string stdout = process.StandardOutput.ReadToEnd();
        string stderr = process.StandardError.ReadToEnd();

        if (!process.WaitForExit(options.TimeoutSeconds * 1000))
        {
            process.Kill(entireProcessTree: true);
            throw new JsxTranspileException("JSX transpilation timed out.");
        }

        if (process.ExitCode != 0)
            throw new JsxTranspileException(stderr);

        return stdout;
    }
}