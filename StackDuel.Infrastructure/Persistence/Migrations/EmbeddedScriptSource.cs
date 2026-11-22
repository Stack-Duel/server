using System.Reflection;
using System.Text;
using DbUp.Engine;

namespace StackDuel.Infrastructure.Persistence.Migrations;

internal static class EmbeddedScriptSource
{
    public static IReadOnlyList<SqlScript> Load(ScriptGroup group)
    {
        Assembly assembly = group.ScriptAssembly;

        return
        [
            .. assembly
                .GetManifestResourceNames()
                .Where(resourceName =>
                    group.Owns(resourceName) && resourceName.EndsWith(".sql", StringComparison.OrdinalIgnoreCase)
                )
                .OrderBy(group.ToScriptName, StringComparer.Ordinal)
                .Select(resourceName => Read(assembly, resourceName, group.ToScriptName(resourceName))),
        ];
    }

    private static SqlScript Read(Assembly assembly, string resourceName, string scriptName)
    {
        using Stream stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded script '{resourceName}' could not be opened.");

        return SqlScript.FromStream(scriptName, stream, Encoding.UTF8);
    }
}
