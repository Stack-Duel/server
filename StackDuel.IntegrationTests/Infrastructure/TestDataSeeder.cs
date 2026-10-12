using Npgsql;
using StackDuel.Domain.Authorization.Rbac.Enums;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Languages.Enums;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.SubmissionJobs.Enums;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.IntegrationTests.Infrastructure;

public sealed record SeededProblem(Guid ProblemId, Guid ProblemSetupId);

public sealed record GradingFixture(Guid PipelineId, IReadOnlyList<SeededProblem> Problems)
{
    public SeededProblem Problem => Problems[0];
}

public sealed record SeededSubmissionJob(SubmissionJobStatus Status, string? FailureReason, DateTime? CompletedAt);

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

    public async Task<Guid> CreateUserAsync(string sub, string username, CancellationToken cancellationToken = default)
    {
        Guid id = Guid.NewGuid();
        await ExecuteAsync(
            "INSERT INTO users (id, sub, username, created_at, is_private) VALUES (@id, @sub, @username, @createdAt, false)",
            cancellationToken,
            ("id", id),
            ("sub", sub),
            ("username", username),
            ("createdAt", DateTime.UtcNow)
        );
        return id;
    }

    public async Task<Guid> CreateExecutionPipelineAsync(CancellationToken cancellationToken = default)
    {
        Guid id = Guid.NewGuid();
        await ExecuteAsync(
            "INSERT INTO execution_pipelines (id, name) VALUES (@id, @name)",
            cancellationToken,
            ("id", id),
            ("name", $"pipeline-{Unique()}")
        );
        return id;
    }

    public async Task<Guid> CreateLanguageVersionAsync(CancellationToken cancellationToken = default)
    {
        Guid trackId = Guid.NewGuid();
        await ExecuteAsync(
            "INSERT INTO tracks (id, key, name, is_active, allows_language_selection) VALUES (@id, @key, @name, true, true)",
            cancellationToken,
            ("id", trackId),
            ("key", $"track-{Unique()}"),
            ("name", "Test track")
        );

        Guid languageId = Guid.NewGuid();
        await ExecuteAsync(
            "INSERT INTO languages (id, name, slug, status, track_id) VALUES (@id, @name, @slug, @status, @trackId)",
            cancellationToken,
            ("id", languageId),
            ("name", "Test language"),
            ("slug", $"language-{Unique()}"),
            ("status", (int)LanguageStatus.Active),
            ("trackId", trackId)
        );

        Guid versionId = Guid.NewGuid();
        await ExecuteAsync(
            """
            INSERT INTO language_versions (id, language_id, version, judge0_id, status, execution_mode)
            VALUES (@id, @languageId, @version, @judge0Id, @status, @executionMode)
            """,
            cancellationToken,
            ("id", versionId),
            ("languageId", languageId),
            ("version", "1.0"),
            ("judge0Id", 71),
            ("status", (int)LanguageVersionStatus.Active),
            ("executionMode", (int)TestExecutionMode.PerCase)
        );

        return versionId;
    }

    public async Task<Guid> CreateProblemAsync(CancellationToken cancellationToken = default)
    {
        Guid id = Guid.NewGuid();
        string unique = Unique();
        await ExecuteAsync(
            """
            INSERT INTO problems (id, slug, title, question, difficulty, time_limit, memory_limit, status, created_at)
            VALUES (@id, @slug, @title, @question, @difficulty, @timeLimit, @memoryLimit, @status, @createdAt)
            """,
            cancellationToken,
            ("id", id),
            ("slug", $"problem-{unique}"),
            ("title", $"Problem {unique}"),
            ("question", "What is the answer?"),
            ("difficulty", 1),
            ("timeLimit", 1000),
            ("memoryLimit", 128),
            ("status", (int)ProblemStatus.Published),
            ("createdAt", DateTime.UtcNow)
        );
        return id;
    }

    public async Task<Guid> CreateProblemSetupAsync(
        Guid problemId,
        Guid languageVersionId,
        Guid pipelineId,
        CancellationToken cancellationToken = default
    )
    {
        Guid id = Guid.NewGuid();
        await ExecuteAsync(
            """
            INSERT INTO problem_setups (id, problem_id, language_version_id, pipeline_id, initial_code, additional_files)
            VALUES (@id, @problemId, @languageVersionId, @pipelineId, @initialCode, @additionalFiles)
            """,
            cancellationToken,
            ("id", id),
            ("problemId", problemId),
            ("languageVersionId", languageVersionId),
            ("pipelineId", pipelineId),
            ("initialCode", "// start here"),
            ("additionalFiles", "[]")
        );
        return id;
    }

    public async Task<GradingFixture> CreateGradingFixtureAsync(
        int problemCount = 1,
        CancellationToken cancellationToken = default
    )
    {
        Guid pipelineId = await CreateExecutionPipelineAsync(cancellationToken);
        Guid languageVersionId = await CreateLanguageVersionAsync(cancellationToken);

        List<SeededProblem> problems = [];
        for (int index = 0; index < problemCount; index++)
        {
            Guid problemId = await CreateProblemAsync(cancellationToken);
            Guid problemSetupId = await CreateProblemSetupAsync(
                problemId,
                languageVersionId,
                pipelineId,
                cancellationToken
            );
            problems.Add(new SeededProblem(problemId, problemSetupId));
        }

        return new GradingFixture(pipelineId, problems);
    }

    public async Task<Guid> CreateGameAsync(
        GameStatus status = GameStatus.Running,
        int timeLimitInSeconds = 3600,
        CancellationToken cancellationToken = default
    )
    {
        Guid poolId = Guid.NewGuid();
        await ExecuteAsync(
            "INSERT INTO problem_pools (id, key, name, created_at) VALUES (@id, @key, @name, @createdAt)",
            cancellationToken,
            ("id", poolId),
            ("key", $"pool-{Unique()}"),
            ("name", "Test pool"),
            ("createdAt", DateTime.UtcNow)
        );

        Guid gameModeId = Guid.NewGuid();
        await ExecuteAsync(
            """
            INSERT INTO game_modes
                (id, key, name, description, default_pool_id, is_active, is_built_in, min_players, max_players, created_at)
            VALUES (@id, @key, @name, @description, @defaultPoolId, true, true, 1, 2, @createdAt)
            """,
            cancellationToken,
            ("id", gameModeId),
            ("key", $"mode-{Unique()}"),
            ("name", "Test mode"),
            ("description", "Test mode"),
            ("defaultPoolId", poolId),
            ("createdAt", DateTime.UtcNow)
        );

        Guid gameId = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;
        await ExecuteAsync(
            """
            INSERT INTO games
                (id, game_mode_id, pool_id, join_code, status, time_limit_in_seconds, created_at, started_at, skips_enabled)
            VALUES (@id, @gameModeId, @poolId, @joinCode, @status, @timeLimitInSeconds, @createdAt, @startedAt, true)
            """,
            cancellationToken,
            ("id", gameId),
            ("gameModeId", gameModeId),
            ("poolId", poolId),
            ("joinCode", Unique()[..7].ToUpperInvariant()),
            ("status", (int)status),
            ("timeLimitInSeconds", timeLimitInSeconds),
            ("createdAt", now),
            ("startedAt", now)
        );

        return gameId;
    }

    public Task AddGameParticipantAsync(
        Guid gameId,
        Guid userId,
        Guid currentProblemId,
        int seatNo = 1,
        CancellationToken cancellationToken = default
    )
    {
        string problemSession =
            $"{{\"currentProblemId\":\"{currentProblemId}\",\"solvedProblemIds\":[],\"skippedProblemIds\":[]}}";

        return ExecuteAsync(
            """
            INSERT INTO game_participants
                (id, game_id, user_id, seat_no, joined_at, score, skips_remaining, problem_session)
            VALUES (@id, @gameId, @userId, @seatNo, @joinedAt, 0, 3, @problemSession::jsonb)
            """,
            cancellationToken,
            ("id", Guid.NewGuid()),
            ("gameId", gameId),
            ("userId", userId),
            ("seatNo", seatNo),
            ("joinedAt", DateTime.UtcNow),
            ("problemSession", problemSession)
        );
    }

    public async Task<Guid> CreateSubmissionAsync(
        Guid userId,
        Guid problemSetupId,
        SubmissionType type = SubmissionType.Submit,
        SubmissionStatus status = SubmissionStatus.Queued,
        DateTime? createdAt = null,
        Guid? gameId = null,
        CancellationToken cancellationToken = default
    )
    {
        Guid id = Guid.NewGuid();
        await ExecuteAsync(
            """
            INSERT INTO submissions
                (id, user_id, problem_setup_id, game_id, type, status, created_at, source_code, additional_files)
            VALUES (@id, @userId, @problemSetupId, @gameId, @type, @status, @createdAt, @sourceCode, @additionalFiles)
            """,
            cancellationToken,
            ("id", id),
            ("userId", userId),
            ("problemSetupId", problemSetupId),
            ("gameId", gameId.HasValue ? gameId.Value : DBNull.Value),
            ("type", (int)type),
            ("status", (int)status),
            ("createdAt", createdAt ?? DateTime.UtcNow),
            ("sourceCode", "print(1)"),
            ("additionalFiles", "[]")
        );
        return id;
    }

    public async Task<Guid> CreateSubmissionJobAsync(
        Guid submissionId,
        Guid pipelineId,
        SubmissionJobStatus status,
        DateTime createdAt,
        CancellationToken cancellationToken = default
    )
    {
        Guid id = Guid.NewGuid();
        await ExecuteAsync(
            """
            INSERT INTO submission_jobs (id, submission_id, pipeline_id, status, created_at)
            VALUES (@id, @submissionId, @pipelineId, @status, @createdAt)
            """,
            cancellationToken,
            ("id", id),
            ("submissionId", submissionId),
            ("pipelineId", pipelineId),
            ("status", (int)status),
            ("createdAt", createdAt)
        );
        return id;
    }

    public async Task<SeededSubmissionJob> GetSubmissionJobAsync(
        Guid jobId,
        CancellationToken cancellationToken = default
    )
    {
        await using NpgsqlConnection connection = await OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT status, failure_reason, completed_at FROM submission_jobs WHERE id = @id";
        command.Parameters.AddWithValue("id", jobId);

        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException($"No submission job found with id '{jobId}'.");

        return new SeededSubmissionJob(
            (SubmissionJobStatus)reader.GetInt32(0),
            reader.IsDBNull(1) ? null : reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetDateTime(2)
        );
    }

    public async Task<SubmissionStatus> GetSubmissionStatusAsync(
        Guid submissionId,
        CancellationToken cancellationToken = default
    )
    {
        await using NpgsqlConnection connection = await OpenConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = "SELECT status FROM submissions WHERE id = @id";
        command.Parameters.AddWithValue("id", submissionId);

        object? status = await command.ExecuteScalarAsync(cancellationToken);
        return status is int value
            ? (SubmissionStatus)value
            : throw new InvalidOperationException($"No submission found with id '{submissionId}'.");
    }

    private static string Unique() => Guid.NewGuid().ToString("N")[..8];

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