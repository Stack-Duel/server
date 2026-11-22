using DbUp.Postgresql;

namespace StackDuel.Infrastructure.Persistence.Migrations;

internal sealed class StandardConformingConnectionManager : PostgresqlConnectionManager
{
    public StandardConformingConnectionManager(string connectionString)
        : base(connectionString)
    {
        StandardConformingStrings = true;
    }
}