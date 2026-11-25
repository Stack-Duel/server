using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using StackDuel.Infrastructure.Persistence;

namespace StackDuel.IntegrationTests;

[Collection(IntegrationTestCollection.Name)]
public sealed class SchemaMatchesEfModelTests(IntegrationTestEnvironment environment)
{
    [Fact]
    public async Task EveryMappedTableAndColumnExistsInTheMigratedDatabase()
    {
        using IServiceScope scope = environment.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<StackDuelDbContext>();

        await using NpgsqlConnection connection = new(environment.ConnectionString);
        await connection.OpenAsync();

        List<string> failures = [];

        foreach (string probe in BuildProbes(context.Model))
        {
            await using NpgsqlCommand command = connection.CreateCommand();
            command.CommandText = probe;

            try
            {
                await command.ExecuteNonQueryAsync();
            }
            catch (PostgresException exception)
            {
                failures.Add($"{probe}\n    -> {exception.MessageText}");
            }
        }

        Assert.True(
            failures.Count == 0,
            $"The SQL scripts and the EF model disagree:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}"
        );
    }

    private static IEnumerable<string> BuildProbes(IModel model)
    {
        foreach (IEntityType entityType in model.GetEntityTypes())
        {
            string? table = entityType.GetTableName();

            if (table is null)
                continue;

            string schema = entityType.GetSchema() ?? "public";
            StoreObjectIdentifier storeObject = StoreObjectIdentifier.Table(table, entityType.GetSchema());

            string[] columns =
            [
                .. entityType
                    .GetProperties()
                    .Select(property => property.GetColumnName(storeObject))
                    .Where(column => !string.IsNullOrEmpty(column))
                    .Distinct()
                    .Select(column => $"\"{column}\""),
            ];

            if (columns.Length == 0)
                continue;

            yield return $"SELECT {string.Join(", ", columns)} FROM \"{schema}\".\"{table}\" LIMIT 0";
        }
    }
}