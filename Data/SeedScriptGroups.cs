using System.Reflection;
using StackDuel.Infrastructure.Persistence.Migrations;

namespace Data;

internal static class SeedScriptGroups
{
    private const string ResourceRoot = "Data.Scripts.";

    public static readonly ScriptGroup Dev = ScriptGroup.InAssembly(
        "dev",
        "Sample data for working locally. Never applied by the API or a deploy.",
        ResourceRoot + "Dev.",
        ThisAssembly
    );

    public static readonly ScriptGroup E2E = ScriptGroup.InAssembly(
        "e2e",
        "Fixtures the Playwright suites sign in as and assert against.",
        ResourceRoot + "E2E.",
        ThisAssembly
    );

    public static IReadOnlyList<ScriptGroup> All { get; } = [Dev, E2E];

    public static IReadOnlyList<ScriptGroup> Everything { get; } = [.. ScriptGroups.All, .. All];

    private static Assembly ThisAssembly => typeof(SeedScriptGroups).Assembly;
}
