using System.Reflection;

namespace StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

internal static class VanillaJsRuntimeAssets
{
    private const string HarnessResourceName = "VanillaJsRuntime/harness.js";

    public static readonly byte[] Harness = Load();

    private static byte[] Load()
    {
        Assembly assembly = typeof(VanillaJsRuntimeAssets).Assembly;
        using Stream stream = assembly.GetManifestResourceStream(HarnessResourceName)!;
        using MemoryStream buffer = new();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }
}