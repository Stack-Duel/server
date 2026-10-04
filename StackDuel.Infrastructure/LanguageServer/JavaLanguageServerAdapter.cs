using StackDuel.Application.Configuration;
using StackDuel.Application.LanguageServer;

namespace StackDuel.Infrastructure.LanguageServer;

internal sealed class JavaLanguageServerAdapter(LanguageServerOptions options) : ILanguageServerAdapter
{
    public string LanguageSlug => "java";

    public string DocumentFileName => "Solution.java";

    public Task PrepareWorkspaceAsync(string workspaceDirectory, CancellationToken cancellationToken)
    {
        string filePath = Path.Combine(workspaceDirectory, DocumentFileName);
        return File.WriteAllTextAsync(filePath, "class Solution {\n}\n", cancellationToken);
    }

    public LanguageServerProcessStartInfo BuildProcessStartInfo(string workspaceDirectory)
    {
        JavaLanguageServerOptions javaOptions = options.Java;
        string sessionRootDirectory =
            Path.GetDirectoryName(Path.GetFullPath(workspaceDirectory))
            ?? throw new InvalidOperationException($"Could not resolve a parent directory for '{workspaceDirectory}'.");
        string dataDirectory = Path.Combine(sessionRootDirectory, "data");

        List<string> arguments =
        [
            "-Declipse.application=org.eclipse.jdt.ls.core.id1",
            "-Dosgi.bundles.defaultStartLevel=4",
            "-Declipse.product=org.eclipse.jdt.ls.core.product",
            "-Dosgi.checkConfiguration=true",
            $"-Dosgi.sharedConfiguration.area={javaOptions.JdtlsConfigDirectory}",
            "-Dosgi.sharedConfiguration.area.readOnly=true",
            "-Dosgi.configuration.cascaded=true",
            "-Xms1G",
            "--add-modules=ALL-SYSTEM",
            "--add-opens",
            "java.base/java.util=ALL-UNNAMED",
            "--add-opens",
            "java.base/java.lang=ALL-UNNAMED",
            "-jar",
            javaOptions.JdtlsLauncherJarPath,
            "-data",
            dataDirectory,
        ];

        string javaBinaryName = OperatingSystem.IsWindows() ? "java.exe" : "java";
        string javaExecutable = string.IsNullOrWhiteSpace(javaOptions.JdtlsRuntimeJavaHome)
            ? "java"
            : Path.Combine(javaOptions.JdtlsRuntimeJavaHome, "bin", javaBinaryName);

        return new LanguageServerProcessStartInfo(javaExecutable, arguments, workspaceDirectory);
    }

    public object? BuildInitializationOptions(string workspaceDirectory)
    {
        string javaHome = options.Java.JavaHome;
        if (string.IsNullOrWhiteSpace(javaHome))
            return null;

        return new
        {
            settings = new
            {
                java = new
                {
                    home = javaHome,
                    configuration = new
                    {
                        runtimes = new object[]
                        {
                            new
                            {
                                name = "JavaSE-17",
                                path = javaHome,
                                @default = true,
                            },
                        },
                    },
                },
            },
        };
    }
}