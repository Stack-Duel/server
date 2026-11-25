using StackDuel.Infrastructure.Persistence.Migrations;

namespace StackDuel.Infrastructure.Tests.Persistence.Migrations;

public class UpgradeResultTests
{
    private static ScriptGroup Group() =>
        ScriptGroup.InAssembly("schema", "Table definitions.", "Root.Scripts.Schema.", typeof(ScriptGroups).Assembly);

    [Fact]
    public void AnAppliedResultCarriesNoError()
    {
        GroupUpgradeResult result = GroupUpgradeResult.Applied(Group(), ["202611210000_Baseline.sql"]);

        Assert.True(result.Succeeded);
        Assert.Null(result.Error);
        Assert.Equal(["202611210000_Baseline.sql"], result.ExecutedScripts);
    }

    [Fact]
    public void AFailedResultKeepsTheScriptsThatDidRunAlongsideTheError()
    {
        InvalidOperationException error = new("boom");

        GroupUpgradeResult result = GroupUpgradeResult.Failed(Group(), ["202611210000_Baseline.sql"], error);

        Assert.False(result.Succeeded);
        Assert.Same(error, result.Error);
        Assert.Equal(["202611210000_Baseline.sql"], result.ExecutedScripts);
    }

    [Fact]
    public void AGroupWithNothingOutstandingIsUpToDate()
    {
        GroupStatus status = new()
        {
            Group = Group(),
            AppliedScripts = ["202611210000_Baseline.sql"],
            PendingScripts = [],
            RepeatableScripts = [],
            OrphanedScripts = [],
        };

        Assert.Equal(0, status.ScriptsToRun);
        Assert.True(status.IsUpToDate);
    }

    [Fact]
    public void PendingAndRepeatableScriptsBothCountAsWorkToDo()
    {
        GroupStatus status = new()
        {
            Group = Group(),
            AppliedScripts = [],
            PendingScripts = ["202611210000_Baseline.sql"],
            RepeatableScripts = ["Repeatable.Permissions.sql", "Repeatable.Flags.sql"],
            OrphanedScripts = ["202501010000_Removed.sql"],
        };

        Assert.Equal(3, status.ScriptsToRun);
        Assert.False(status.IsUpToDate);
        Assert.Single(status.OrphanedScripts);
    }
}