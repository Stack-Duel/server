namespace StackDuel.Data.Migrations;

public sealed record GroupStatus
{
    public required ScriptGroup Group { get; init; }

    public required IReadOnlyList<string> AppliedScripts { get; init; }

    public required IReadOnlyList<string> PendingScripts { get; init; }

    public required IReadOnlyList<string> RepeatableScripts { get; init; }

    public required IReadOnlyList<string> OrphanedScripts { get; init; }

    public int ScriptsToRun => PendingScripts.Count + RepeatableScripts.Count;

    public bool IsUpToDate => ScriptsToRun == 0;
}