using Npgsql;

namespace StackDuel.Data.Seeding;

internal static class FileBundleSeeder
{
    private static readonly Guid ChinookBundleId = Guid.Parse("18ce3b58-23ab-487d-bae4-dd885fe7f88a");

    private const string ChinookBundleName = "chinook-sqlite-db";

    private const string ChinookResourceName = "StackDuel.Data.Assets.chinook-sqlite-db.zip";

    public static bool EnsureChinookBundle(string connectionString)
    {
        byte[] content = ReadAsset(ChinookResourceName);

        using NpgsqlConnection connection = new(connectionString);
        connection.Open();

        using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO additional_file_bundles (id, name, content, created_at)
            VALUES (@id, @name, @content, NOW())
            ON CONFLICT (name) DO NOTHING
            """;
        command.Parameters.AddWithValue("id", ChinookBundleId);
        command.Parameters.AddWithValue("name", ChinookBundleName);
        command.Parameters.AddWithValue("content", content);

        return command.ExecuteNonQuery() > 0;
    }

    private static byte[] ReadAsset(string resourceName)
    {
        using Stream stream =
            typeof(FileBundleSeeder).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException(
                $"The embedded asset '{resourceName}' is missing from the Data assembly."
            );

        using MemoryStream buffer = new();
        stream.CopyTo(buffer);

        return buffer.ToArray();
    }
}