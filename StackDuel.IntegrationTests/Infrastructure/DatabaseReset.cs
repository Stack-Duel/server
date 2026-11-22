using Npgsql;

namespace StackDuel.IntegrationTests.Infrastructure;

public static class DatabaseReset
{
    public static async Task TruncateEverythingAsync(
        string connectionString,
        CancellationToken cancellationToken = default
    )
    {
        await using NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = """
            DO $$
            DECLARE tables text;
            BEGIN
                SELECT string_agg(format('%I.%I', schemaname, tablename), ', ')
                INTO tables
                FROM pg_tables
                WHERE schemaname = 'public' AND tablename NOT LIKE 'dbup\_%';

                IF tables IS NOT NULL THEN
                    EXECUTE 'TRUNCATE TABLE ' || tables || ' RESTART IDENTITY CASCADE';
                END IF;
            END $$;
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
