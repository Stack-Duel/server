using StackDuel.Infrastructure.Persistence.Migrations;
using System.Reflection;

namespace StackDuel.Infrastructure.Tests.Persistence.Migrations;

public class ScriptGroupTests
{
    private static readonly Assembly ScriptAssembly = typeof(ScriptGroups).Assembly;

    private static ScriptGroup Group() =>
        ScriptGroup.InAssembly("schema", "Table definitions.", "Root.Scripts.Schema.", ScriptAssembly);

    [Fact]
    public void TheJournalTableIsDerivedFromTheGroupName()
    {
        Assert.Equal("dbup_schema_journal", Group().JournalTable);
    }

    [Fact]
    public void AGroupOwnsOnlyResourcesUnderItsPrefix()
    {
        ScriptGroup group = Group();

        Assert.True(group.Owns("Root.Scripts.Schema.202611210000_Baseline.sql"));
        Assert.False(group.Owns("Root.Scripts.Reference.202611210001_Authorization.sql"));
    }

    [Fact]
    public void TheScriptNameDropsTheResourcePrefix()
    {
        Assert.Equal(
            "202611210000_Baseline.sql",
            Group().ToScriptName("Root.Scripts.Schema.202611210000_Baseline.sql")
        );
    }

    [Theory]
    [InlineData("Repeatable.202611210001_Permissions.sql", true)]
    [InlineData("202611210000_Baseline.sql", false)]
    [InlineData("Schema.Repeatable.Thing.sql", false)]
    public void OnlyScriptsUnderRepeatableReRunEveryTime(string scriptName, bool expected)
    {
        Assert.Equal(expected, ScriptGroup.IsRepeatable(scriptName));
    }

    [Fact]
    public void TheConfiguredGroupsCoverSchemaReferenceAndContent()
    {
        Assert.Equal(["schema", "reference", "content"], ScriptGroups.All.Select(group => group.Name));
        Assert.All(ScriptGroups.All, group => Assert.NotEmpty(group.Description));
        Assert.All(ScriptGroups.All, group => Assert.Equal(ScriptAssembly, group.ScriptAssembly));
    }
}