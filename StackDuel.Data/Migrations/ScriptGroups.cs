using System.Reflection;

namespace StackDuel.Data.Migrations;

public static class ScriptGroups
{
    internal const string ResourceRoot = "StackDuel.Data.Scripts.";

    public static readonly ScriptGroup Schema = ScriptGroup.InAssembly(
        "schema",
        "Table, index, and constraint definitions.",
        ResourceRoot + "Schema.",
        ThisAssembly
    );

    public static readonly ScriptGroup Reference = ScriptGroup.InAssembly(
        "reference",
        "Lookup data the application cannot start without: permissions, roles, feature flags, languages, game modes.",
        ResourceRoot + "Reference.",
        ThisAssembly
    );

    public static IReadOnlyList<ScriptGroup> All { get; } = [Schema, Reference];

    private static Assembly ThisAssembly => typeof(ScriptGroups).Assembly;
}