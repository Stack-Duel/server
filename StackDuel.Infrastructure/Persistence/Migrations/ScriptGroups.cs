using System.Reflection;

namespace StackDuel.Infrastructure.Persistence.Migrations;

public static class ScriptGroups
{
    private const string ResourceRoot = "StackDuel.Infrastructure.Persistence.Migrations.Scripts.";

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

    public static readonly ScriptGroup Content = ScriptGroup.InAssembly(
        "content",
        "The published problem catalog, its test suites, and the campaigns built from it.",
        ResourceRoot + "Content.",
        ThisAssembly
    );

    public static IReadOnlyList<ScriptGroup> All { get; } = [Schema, Reference, Content];

    private static Assembly ThisAssembly => typeof(ScriptGroups).Assembly;
}