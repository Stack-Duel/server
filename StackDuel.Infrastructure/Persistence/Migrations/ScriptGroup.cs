using System.Reflection;

namespace StackDuel.Infrastructure.Persistence.Migrations;

public sealed record ScriptGroup
{
    private ScriptGroup(string name, string description, string resourcePrefix, Assembly scriptAssembly)
    {
        Name = name;
        Description = description;
        ResourcePrefix = resourcePrefix;
        ScriptAssembly = scriptAssembly;
        JournalTable = $"dbup_{name}_journal";
    }

    public string Name { get; }

    public string Description { get; }

    public string ResourcePrefix { get; }

    public Assembly ScriptAssembly { get; }

    public string JournalTable { get; }

    public static ScriptGroup InAssembly(
        string name,
        string description,
        string resourcePrefix,
        Assembly scriptAssembly
    ) => new(name, description, resourcePrefix, scriptAssembly);

    public bool Owns(string resourceName) => resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal);

    public string ToScriptName(string resourceName) => resourceName[ResourcePrefix.Length..];
}
