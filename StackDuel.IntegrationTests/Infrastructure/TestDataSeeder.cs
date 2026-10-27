using Npgsql;
using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.IntegrationTests.Infrastructure;

public sealed class TestDataSeeder(string connectionString)
{
    public async Task<Guid> GetUserIdBySubAsync(string sub, CancellationToken cancellationToken = default)
    {
        await using NpgsqlConnection connection = await OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT id FROM users WHERE sub = @sub";
        command.Parameters.AddWithValue("sub", sub);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is Guid id ? id : throw new InvalidOperationException($"No user found with sub '{sub}'.");
    }

    public async Task<Guid> CreatePermissionAsync(
        string code,
        string description = "test permission",
        CancellationToken cancellationToken = default
    )
    {
        Guid id = Guid.NewGuid();
        await ExecuteAsync(
            "INSERT INTO permissions (id, code, description) VALUES (@id, @code, @description)",
            cancellationToken,
            ("id", id),
            ("code", code),
            ("description", description)
        );
        return id;
    }

    public async Task<Guid> CreateRoleAsync(string name, CancellationToken cancellationToken = default)
    {
        Guid id = Guid.NewGuid();
        await ExecuteAsync(
            "INSERT INTO roles (id, name) VALUES (@id, @name)",
            cancellationToken,
            ("id", id),
            ("name", name)
        );
        return id;
    }

    public Task GrantRolePermissionAsync(
        Guid roleId,
        Guid permissionId,
        DecisionEffect effect,
        CancellationToken cancellationToken = default
    ) =>
        ExecuteAsync(
            "INSERT INTO role_permissions (role_id, permission_id, effect) VALUES (@roleId, @permissionId, @effect)",
            cancellationToken,
            ("roleId", roleId),
            ("permissionId", permissionId),
            ("effect", effect.ToString())
        );

    public async Task<Guid> CreateGroupAsync(string name, CancellationToken cancellationToken = default)
    {
        Guid id = Guid.NewGuid();
        await ExecuteAsync(
            "INSERT INTO groups (id, name) VALUES (@id, @name)",
            cancellationToken,
            ("id", id),
            ("name", name)
        );
        return id;
    }

    public Task GrantGroupRoleAsync(Guid groupId, Guid roleId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO group_roles (group_id, role_id) VALUES (@groupId, @roleId)",
            cancellationToken,
            ("groupId", groupId),
            ("roleId", roleId)
        );

    public Task AddUserToGroupAsync(Guid userId, Guid groupId, CancellationToken cancellationToken = default) =>
        ExecuteAsync(
            "INSERT INTO user_groups (user_id, group_id) VALUES (@userId, @groupId)",
            cancellationToken,
            ("userId", userId),
            ("groupId", groupId)
        );

    public async Task<Guid> CreateTrackAsync(
        string key,
        string name,
        bool isActive = true,
        CancellationToken cancellationToken = default
    )
    {
        Guid id = Guid.NewGuid();
        await ExecuteAsync(
            """
            INSERT INTO tracks (id, key, name, is_active, allows_language_selection)
            VALUES (@id, @key, @name, @isActive, true)
            """,
            cancellationToken,
            ("id", id),
            ("key", key),
            ("name", name),
            ("isActive", isActive)
        );
        return id;
    }

    public async Task<Guid> GetLanguageVersionIdBySlugAsync(
        string languageSlug,
        CancellationToken cancellationToken = default
    )
    {
        await using NpgsqlConnection connection = await OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = """
            SELECT lv.id
            FROM language_versions lv
            JOIN languages l ON l.id = lv.language_id
            WHERE l.slug = @slug
            LIMIT 1
            """;
        command.Parameters.AddWithValue("slug", languageSlug);

        object? result = await command.ExecuteScalarAsync(cancellationToken);
        return result is Guid id
            ? id
            : throw new InvalidOperationException($"No language version found for slug '{languageSlug}'.");
    }

    public async Task<Guid> CreateSubmissionAsync(
        Guid userId,
        Guid problemSetupId,
        SubmissionStatus status,
        SubmissionType type = SubmissionType.Submit,
        CancellationToken cancellationToken = default
    )
    {
        Guid id = Guid.NewGuid();
        await ExecuteAsync(
            """
            INSERT INTO submissions (id, user_id, problem_setup_id, type, status, created_at, source_code, additional_files)
            VALUES (@id, @userId, @problemSetupId, @type, @status, now(), @sourceCode, @additionalFiles)
            """,
            cancellationToken,
            ("id", id),
            ("userId", userId),
            ("problemSetupId", problemSetupId),
            ("type", (int)type),
            ("status", (int)status),
            ("sourceCode", "print('hi')"),
            ("additionalFiles", "[]")
        );
        return id;
    }

    private async Task ExecuteAsync(
        string commandText,
        CancellationToken cancellationToken,
        params (string Name, object Value)[] parameters
    )
    {
        await using NpgsqlConnection connection = await OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = commandText;

        foreach ((string name, object value) in parameters)
            command.Parameters.AddWithValue(name, value);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task<NpgsqlConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        NpgsqlConnection connection = new(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}