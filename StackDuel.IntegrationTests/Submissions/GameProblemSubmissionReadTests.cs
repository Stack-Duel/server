using Microsoft.Extensions.DependencyInjection;
using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.IntegrationTests.Infrastructure;

namespace StackDuel.IntegrationTests.Submissions;

public sealed class GameProblemSubmissionReadTests(IntegrationTestEnvironment environment)
    : ApiIntegrationTestBase(environment)
{
    private static readonly DateTime Noon = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task FindGameProblemSubmission_resolves_the_problem_through_the_problem_setup_join()
    {
        GradingFixture fixture = await Seeder.CreateGradingFixtureAsync();
        Guid userId = await Seeder.CreateUserAsync("sub-finder", "finder");
        Guid gameId = await Seeder.CreateGameAsync();
        Guid submissionId = await Seeder.CreateSubmissionAsync(
            userId,
            fixture.Problem.ProblemSetupId,
            status: SubmissionStatus.Accepted,
            createdAt: Noon,
            gameId: gameId
        );

        GameProblemSubmissionDto? submission = await FindGameProblemSubmissionAsync(submissionId);

        Assert.NotNull(submission);
        Assert.Equal(submissionId, submission!.SubmissionId);
        Assert.Equal(gameId, submission.GameId);
        Assert.Equal(userId, submission.UserId);
        Assert.Equal(fixture.Problem.ProblemId, submission.ProblemId);
        Assert.Equal(SubmissionStatus.Accepted, submission.Status);
        Assert.Equal(Noon, submission.CreatedAt);
    }

    [Fact]
    public async Task FindGameProblemSubmission_returns_null_for_a_submission_that_does_not_exist()
    {
        GameProblemSubmissionDto? submission = await FindGameProblemSubmissionAsync(Guid.NewGuid());

        Assert.Null(submission);
    }

    [Fact]
    public async Task FindGameProblemSubmission_returns_null_for_a_submission_made_outside_a_game()
    {
        GradingFixture fixture = await Seeder.CreateGradingFixtureAsync();
        Guid userId = await Seeder.CreateUserAsync("sub-practice", "practice");
        Guid submissionId = await Seeder.CreateSubmissionAsync(userId, fixture.Problem.ProblemSetupId);

        GameProblemSubmissionDto? submission = await FindGameProblemSubmissionAsync(submissionId);

        Assert.Null(submission);
    }

    [Fact]
    public async Task FindGameProblemSubmission_returns_null_for_a_run_submission()
    {
        GradingFixture fixture = await Seeder.CreateGradingFixtureAsync();
        Guid userId = await Seeder.CreateUserAsync("sub-runner", "runner");
        Guid gameId = await Seeder.CreateGameAsync();
        Guid submissionId = await Seeder.CreateSubmissionAsync(
            userId,
            fixture.Problem.ProblemSetupId,
            type: SubmissionType.Run,
            gameId: gameId
        );

        GameProblemSubmissionDto? submission = await FindGameProblemSubmissionAsync(submissionId);

        Assert.Null(submission);
    }

    [Fact]
    public async Task GetLatestGameProblemSubmissions_keeps_only_the_newest_submission_per_user_and_problem()
    {
        GradingFixture fixture = await Seeder.CreateGradingFixtureAsync();
        Guid userId = await Seeder.CreateUserAsync("sub-repeater", "repeater");
        Guid gameId = await Seeder.CreateGameAsync();

        await Seeder.CreateSubmissionAsync(
            userId,
            fixture.Problem.ProblemSetupId,
            status: SubmissionStatus.WrongAnswer,
            createdAt: Noon,
            gameId: gameId
        );
        await Seeder.CreateSubmissionAsync(
            userId,
            fixture.Problem.ProblemSetupId,
            status: SubmissionStatus.WrongAnswer,
            createdAt: Noon.AddMinutes(1),
            gameId: gameId
        );
        Guid newestSubmissionId = await Seeder.CreateSubmissionAsync(
            userId,
            fixture.Problem.ProblemSetupId,
            status: SubmissionStatus.Accepted,
            createdAt: Noon.AddMinutes(2),
            gameId: gameId
        );

        IReadOnlyList<GameProblemSubmissionDto> latest = await GetLatestGameProblemSubmissionsAsync(gameId);

        GameProblemSubmissionDto only = Assert.Single(latest);
        Assert.Equal(newestSubmissionId, only.SubmissionId);
        Assert.Equal(SubmissionStatus.Accepted, only.Status);
        Assert.Equal(fixture.Problem.ProblemId, only.ProblemId);
    }

    [Fact]
    public async Task GetLatestGameProblemSubmissions_returns_an_entry_for_every_user_and_problem_pair()
    {
        GradingFixture fixture = await Seeder.CreateGradingFixtureAsync(problemCount: 2);
        SeededProblem firstProblem = fixture.Problems[0];
        SeededProblem secondProblem = fixture.Problems[1];
        Guid firstUserId = await Seeder.CreateUserAsync("sub-pair-one", "pairone");
        Guid secondUserId = await Seeder.CreateUserAsync("sub-pair-two", "pairtwo");
        Guid gameId = await Seeder.CreateGameAsync();

        foreach (Guid userId in new[] { firstUserId, secondUserId })
        {
            foreach (SeededProblem problem in new[] { firstProblem, secondProblem })
            {
                await Seeder.CreateSubmissionAsync(userId, problem.ProblemSetupId, createdAt: Noon, gameId: gameId);
            }
        }

        IReadOnlyList<GameProblemSubmissionDto> latest = await GetLatestGameProblemSubmissionsAsync(gameId);

        Assert.Equal(4, latest.Count);
        Assert.Contains(latest, dto => dto.UserId == firstUserId && dto.ProblemId == firstProblem.ProblemId);
        Assert.Contains(latest, dto => dto.UserId == firstUserId && dto.ProblemId == secondProblem.ProblemId);
        Assert.Contains(latest, dto => dto.UserId == secondUserId && dto.ProblemId == firstProblem.ProblemId);
        Assert.Contains(latest, dto => dto.UserId == secondUserId && dto.ProblemId == secondProblem.ProblemId);
    }

    [Fact]
    public async Task GetLatestGameProblemSubmissions_excludes_run_submissions_and_other_games()
    {
        GradingFixture fixture = await Seeder.CreateGradingFixtureAsync();
        Guid userId = await Seeder.CreateUserAsync("sub-mixed", "mixed");
        Guid gameId = await Seeder.CreateGameAsync();
        Guid otherGameId = await Seeder.CreateGameAsync();

        Guid countedSubmissionId = await Seeder.CreateSubmissionAsync(
            userId,
            fixture.Problem.ProblemSetupId,
            createdAt: Noon,
            gameId: gameId
        );
        await Seeder.CreateSubmissionAsync(
            userId,
            fixture.Problem.ProblemSetupId,
            type: SubmissionType.Run,
            createdAt: Noon.AddMinutes(5),
            gameId: gameId
        );
        await Seeder.CreateSubmissionAsync(
            userId,
            fixture.Problem.ProblemSetupId,
            createdAt: Noon.AddMinutes(10),
            gameId: otherGameId
        );

        IReadOnlyList<GameProblemSubmissionDto> latest = await GetLatestGameProblemSubmissionsAsync(gameId);

        GameProblemSubmissionDto only = Assert.Single(latest);
        Assert.Equal(countedSubmissionId, only.SubmissionId);
    }

    [Fact]
    public async Task FindLatestGameProblemSubmission_returns_the_newest_submission_for_the_game_user_and_problem()
    {
        GradingFixture fixture = await Seeder.CreateGradingFixtureAsync(problemCount: 2);
        SeededProblem targetProblem = fixture.Problems[0];
        SeededProblem otherProblem = fixture.Problems[1];
        Guid userId = await Seeder.CreateUserAsync("sub-latest", "latest");
        Guid otherUserId = await Seeder.CreateUserAsync("sub-latest-other", "latestother");
        Guid gameId = await Seeder.CreateGameAsync();

        await Seeder.CreateSubmissionAsync(
            userId,
            targetProblem.ProblemSetupId,
            status: SubmissionStatus.WrongAnswer,
            createdAt: Noon,
            gameId: gameId
        );
        Guid newestSubmissionId = await Seeder.CreateSubmissionAsync(
            userId,
            targetProblem.ProblemSetupId,
            status: SubmissionStatus.Running,
            createdAt: Noon.AddMinutes(3),
            gameId: gameId
        );
        await Seeder.CreateSubmissionAsync(
            userId,
            otherProblem.ProblemSetupId,
            createdAt: Noon.AddMinutes(9),
            gameId: gameId
        );
        await Seeder.CreateSubmissionAsync(
            otherUserId,
            targetProblem.ProblemSetupId,
            createdAt: Noon.AddMinutes(9),
            gameId: gameId
        );

        GameProblemSubmissionDto? submission = await FindLatestGameProblemSubmissionAsync(
            gameId,
            userId,
            targetProblem.ProblemId
        );

        Assert.NotNull(submission);
        Assert.Equal(newestSubmissionId, submission!.SubmissionId);
        Assert.Equal(SubmissionStatus.Running, submission.Status);
    }

    [Fact]
    public async Task FindLatestGameProblemSubmission_returns_null_when_the_user_has_not_submitted_that_problem()
    {
        GradingFixture fixture = await Seeder.CreateGradingFixtureAsync(problemCount: 2);
        SeededProblem attemptedProblem = fixture.Problems[0];
        SeededProblem untouchedProblem = fixture.Problems[1];
        Guid userId = await Seeder.CreateUserAsync("sub-untouched", "untouched");
        Guid gameId = await Seeder.CreateGameAsync();

        await Seeder.CreateSubmissionAsync(userId, attemptedProblem.ProblemSetupId, createdAt: Noon, gameId: gameId);

        GameProblemSubmissionDto? submission = await FindLatestGameProblemSubmissionAsync(
            gameId,
            userId,
            untouchedProblem.ProblemId
        );

        Assert.Null(submission);
    }

    private async Task<GameProblemSubmissionDto?> FindGameProblemSubmissionAsync(Guid submissionId)
    {
        using IServiceScope scope = CreateServiceScope();
        ISubmissionReadRepository repository = scope.ServiceProvider.GetRequiredService<ISubmissionReadRepository>();
        return await repository.FindGameProblemSubmissionAsync(submissionId);
    }

    private async Task<IReadOnlyList<GameProblemSubmissionDto>> GetLatestGameProblemSubmissionsAsync(Guid gameId)
    {
        using IServiceScope scope = CreateServiceScope();
        ISubmissionReadRepository repository = scope.ServiceProvider.GetRequiredService<ISubmissionReadRepository>();
        return await repository.GetLatestGameProblemSubmissionsAsync(gameId);
    }

    private async Task<GameProblemSubmissionDto?> FindLatestGameProblemSubmissionAsync(
        Guid gameId,
        Guid userId,
        Guid problemId
    )
    {
        using IServiceScope scope = CreateServiceScope();
        ISubmissionReadRepository repository = scope.ServiceProvider.GetRequiredService<ISubmissionReadRepository>();
        return await repository.FindLatestGameProblemSubmissionAsync(gameId, userId, problemId);
    }
}