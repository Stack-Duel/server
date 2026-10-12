namespace StackDuel.IntegrationTests;

/// <summary>
/// Binds every API integration test into one xUnit collection. Membership does double duty: it
/// shares the single <see cref="IntegrationTestEnvironment"/>, and because xUnit never runs two
/// tests from the same collection concurrently, it keeps the per-test TRUNCATE in
/// <see cref="ApiIntegrationTestBase"/> from wiping a sibling test's data mid-run.
/// </summary>
[CollectionDefinition(Name)]
public sealed class IntegrationTestCollection : ICollectionFixture<IntegrationTestEnvironment>
{
    public const string Name = "API integration";
}