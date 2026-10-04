namespace StackDuel.Infrastructure.Persistence.Seeders;

internal interface ISeeder
{
    Task SeedAsync(CancellationToken cancellationToken = default);
}

/// <summary>Reference data — languages, problems, tracks. Idempotent, runs on every deploy.</summary>
internal interface IStaticSeeder : ISeeder;

/// <summary>Demo/sample data. Development and staging only.</summary>
internal interface IDemoSeeder : ISeeder;