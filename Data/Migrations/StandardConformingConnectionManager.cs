using DbUp.Postgresql;

namespace Data.Migrations;

internal sealed class StandardConformingConnectionManager : PostgresqlConnectionManager
{
    public StandardConformingConnectionManager(string connectionString)
        : base(connectionString)
    {
        StandardConformingStrings = true;
    }
}