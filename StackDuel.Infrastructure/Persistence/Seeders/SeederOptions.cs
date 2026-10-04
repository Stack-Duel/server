namespace StackDuel.Infrastructure.Persistence.Seeders;

public sealed class SeederOptions
{
    /// <summary>
    /// Seeds reference data (languages, versions, problems). Idempotent — safe to run on every deploy.
    /// </summary>
    public bool SeedStaticData { get; init; }

    /// <summary>
    /// Seeds demo/sample data (example problems, test suites).
    /// Intended for development and staging environments only.
    /// </summary>
    public bool SeedDemoData { get; init; }
}