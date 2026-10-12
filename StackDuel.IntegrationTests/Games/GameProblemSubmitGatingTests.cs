using Ardalis.Result;
using Microsoft.Extensions.DependencyInjection;
using StackDuel.Application.Services.Games;
using StackDuel.Application.Submissions;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.IntegrationTests.Infrastructure;

namespace StackDuel.IntegrationTests.Games;

public sealed class GameProblemSubmitGatingTests(IntegrationTestEnvironment environment)
    : ApiIntegrationTestBase(environment)
{
    [Fact]
    public async Task Submit_is_rejected_when_the_player_already_solved_their_current_problem()
    {
        SeatedPlayer player = await SeatPlayerOnTheirCurrentProblemAsync("sub-solved", "solved");
        await Seeder.CreateSubmissionAsync(
            player.UserId,
            player.ProblemSetupId,
            status: SubmissionStatus.Accepted,
            createdAt: DateTime.UtcNow.AddMinutes(-1),
            gameId: player.GameId
        );

        Result<Guid> result = await SubmitAsync(player);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(result.ValidationErrors, error => error.ErrorMessage.Contains("already solved"));
    }

    [Fact]
    public async Task Submit_is_rejected_while_a_recent_submission_is_still_being_graded()
    {
        SeatedPlayer player = await SeatPlayerOnTheirCurrentProblemAsync("sub-grading", "grading");
        await Seeder.CreateSubmissionAsync(
            player.UserId,
            player.ProblemSetupId,
            status: SubmissionStatus.Running,
            createdAt: DateTime.UtcNow.AddMinutes(-1),
            gameId: player.GameId
        );

        Result<Guid> result = await SubmitAsync(player);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(result.ValidationErrors, error => error.ErrorMessage.Contains("still being graded"));
    }

    [Fact]
    public async Task Submit_is_allowed_past_the_gate_when_the_ungraded_submission_is_older_than_the_grading_window()
    {
        SeatedPlayer player = await SeatPlayerOnTheirCurrentProblemAsync("sub-abandoned", "abandoned");
        await Seeder.CreateSubmissionAsync(
            player.UserId,
            player.ProblemSetupId,
            status: SubmissionStatus.Running,
            createdAt: DateTime.UtcNow - SubmissionGradingWindow.StaleAfter.Add(TimeSpan.FromMinutes(5)),
            gameId: player.GameId
        );

        Result<Guid> result = await SubmitAsync(player);

        Assert.NotEqual(ResultStatus.Invalid, result.Status);
        Assert.Empty(result.ValidationErrors);
    }

    private async Task<SeatedPlayer> SeatPlayerOnTheirCurrentProblemAsync(string sub, string username)
    {
        GradingFixture fixture = await Seeder.CreateGradingFixtureAsync();
        Guid userId = await Seeder.CreateUserAsync(sub, username);
        Guid gameId = await Seeder.CreateGameAsync();
        await Seeder.AddGameParticipantAsync(gameId, userId, fixture.Problem.ProblemId);

        return new SeatedPlayer(gameId, userId, fixture.Problem.ProblemId, fixture.Problem.ProblemSetupId);
    }

    private async Task<Result<Guid>> SubmitAsync(SeatedPlayer player)
    {
        using IServiceScope scope = CreateServiceScope();
        IGameService gameService = scope.ServiceProvider.GetRequiredService<IGameService>();
        return await gameService.SubmitGameProblemAsync(
            player.GameId,
            player.ProblemId,
            player.ProblemSetupId,
            "print(1)",
            player.UserId,
            CancellationToken.None
        );
    }

    private sealed record SeatedPlayer(Guid GameId, Guid UserId, Guid ProblemId, Guid ProblemSetupId);
}
