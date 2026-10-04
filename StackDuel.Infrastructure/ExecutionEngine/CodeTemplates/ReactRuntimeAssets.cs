using System.Collections.Frozen;
using System.Linq;
using System.Reflection;

namespace StackDuel.Infrastructure.ExecutionEngine.CodeTemplates;

internal static class ReactRuntimeAssets
{
    private const string LogicalNamePrefix = "ReactRuntime/";

    public static readonly FrozenDictionary<string, byte[]> ZipEntries = Load();

    private static FrozenDictionary<string, byte[]> Load()
    {
        Assembly assembly = typeof(ReactRuntimeAssets).Assembly;
        Dictionary<string, byte[]> entries = [];

        foreach (
            string resourceName in assembly
                .GetManifestResourceNames()
                .Where(resourceName => resourceName.StartsWith(LogicalNamePrefix, StringComparison.Ordinal))
        )
        {
            string zipPath = resourceName[LogicalNamePrefix.Length..];

            using Stream stream = assembly.GetManifestResourceStream(resourceName)!;
            using MemoryStream buffer = new();
            stream.CopyTo(buffer);
            entries[zipPath] = buffer.ToArray();
        }

        return entries.ToFrozenDictionary();
    }
}