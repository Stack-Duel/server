using Npgsql;
using Spectre.Console;
using StackDuel.Data.Infrastructure;
using StackDuel.Domain.Authorization.Rbac;
using System.CommandLine;

namespace StackDuel.Data.Commands;

internal static class GrantAdminCommand
{
    private const string ActorUsername = "cli:data";
    private const string Action = "user.groups.admin-granted";

    public static Command Create()
    {
        Argument<string> username = new("username") { Description = "Username of an existing user." };

        Command command = new("grant-admin", "Add an existing user to the admin group.");
        command.Arguments.Add(username);
        command.Options.Add(SharedOptions.Connection);

        command.SetAction(
            async (parseResult, cancellationToken) =>
            {
                string name = parseResult.GetRequiredValue(username);
                string connectionString = ConnectionStringResolver.Resolve(
                    parseResult.GetValue(SharedOptions.Connection)
                );

                await using NpgsqlConnection connection = new(connectionString);
                await connection.OpenAsync(cancellationToken);

                Guid? userId = await FindUserIdAsync(connection, name, cancellationToken);
                if (!userId.HasValue)
                {
                    AnsiConsole.MarkupLine($"[red]No user found with username '{Markup.Escape(name)}'.[/]");
                    return UpgradeRunner.Failure;
                }

                Guid? groupId = await FindAdminGroupIdAsync(connection, cancellationToken);
                if (!groupId.HasValue)
                {
                    AnsiConsole.MarkupLine(
                        $"[red]The '{WellKnownAuthorization.AdminGroup}' group does not exist.[/] "
                            + "Run migrate first so the reference data is in place."
                    );
                    return UpgradeRunner.Failure;
                }

                await AddToGroupAsync(connection, userId.Value, groupId.Value, cancellationToken);
                await RecordAuditEntryAsync(connection, userId.Value, cancellationToken);

                AnsiConsole.MarkupLine($"[green]Granted the admin group to '{Markup.Escape(name)}'.[/]");

                return UpgradeRunner.Success;
            }
        );

        return command;
    }

    private static async Task<Guid?> FindUserIdAsync(
        NpgsqlConnection connection,
        string username,
        CancellationToken cancellationToken
    )
    {
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM users WHERE username = @username";
        command.Parameters.AddWithValue("username", username);

        return await command.ExecuteScalarAsync(cancellationToken) as Guid?;
    }

    private static async Task<Guid?> FindAdminGroupIdAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken
    )
    {
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM groups WHERE name = @name";
        command.Parameters.AddWithValue("name", WellKnownAuthorization.AdminGroup);

        return await command.ExecuteScalarAsync(cancellationToken) as Guid?;
    }

    private static async Task AddToGroupAsync(
        NpgsqlConnection connection,
        Guid userId,
        Guid groupId,
        CancellationToken cancellationToken
    )
    {
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO user_groups (user_id, group_id)
            VALUES (@userId, @groupId)
            ON CONFLICT DO NOTHING
            """;
        command.Parameters.AddWithValue("userId", userId);
        command.Parameters.AddWithValue("groupId", groupId);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task RecordAuditEntryAsync(
        NpgsqlConnection connection,
        Guid userId,
        CancellationToken cancellationToken
    )
    {
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO audit_log_entries (id, actor_user_id, actor_username, action, target_type, target_id, created_at)
            VALUES (gen_random_uuid(), NULL, @actor, @action, 'user', @targetId, now())
            """;
        command.Parameters.AddWithValue("actor", ActorUsername);
        command.Parameters.AddWithValue("action", Action);
        command.Parameters.AddWithValue("targetId", userId.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}