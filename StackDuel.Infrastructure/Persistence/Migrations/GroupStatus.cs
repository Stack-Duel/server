namespace StackDuel.Infrastructure.Persistence.Migrations;

public sealed record GroupStatus
{
    public required ScriptGroup Group { get; init; }

    public required IReadOnlyList<string> AppliedScripts { get; init; }

    public required IReadOnlyList<string> PendingScripts { get; init; }

    public required IReadOnlyList<string> OrphanedScripts { get; init; }

    public bool IsUpToDate => PendingScripts.Count == 0;
}
