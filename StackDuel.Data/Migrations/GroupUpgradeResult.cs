namespace StackDuel.Data.Migrations;

public sealed record GroupUpgradeResult
{
    public required ScriptGroup Group { get; init; }

    public required IReadOnlyList<string> ExecutedScripts { get; init; }

    public Exception? Error { get; init; }

    public bool Succeeded => Error is null;

    public static GroupUpgradeResult Applied(ScriptGroup group, IReadOnlyList<string> executedScripts) =>
        new() { Group = group, ExecutedScripts = executedScripts };

    public static GroupUpgradeResult Failed(
        ScriptGroup group,
        IReadOnlyList<string> executedScripts,
        Exception error
    ) =>
        new()
        {
            Group = group,
            ExecutedScripts = executedScripts,
            Error = error,
        };
}