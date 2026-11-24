using Ardalis.Result;
using Moq;
using StackDuel.Application.Commands.Problems.RemoveProblemSetup;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Application.Tests.Commands.Problems.RemoveProblemSetup;

public class RemoveProblemSetupHandlerTests
{
    private readonly Mock<IProblemRepository> _problemRepository = new();
    private readonly RemoveProblemSetupHandler _handler;

    public RemoveProblemSetupHandlerTests()
    {
        _handler = new RemoveProblemSetupHandler(new RemoveProblemSetupValidator(), _problemRepository.Object);
    }

    private static Problem CreateProblem() =>
        new(
            new Slug("two-sum"),
            new Title("Two Sum"),
            new Question(new string('q', 60)),
            new Difficulty(150),
            new TimeLimit(1000),
            new MemoryLimit(256)
        );

    private Problem SetUpProblem(Problem? problem = null)
    {
        Problem resolved = problem ?? CreateProblem();
        _problemRepository
            .Setup(repository => repository.FindByIdAsync(resolved.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolved);
        return resolved;
    }

    private Task<Result> RemoveAsync(Problem problem, Guid languageVersionId) =>
        _handler.Handle(new RemoveProblemSetupCommand(problem.Id, languageVersionId), default);

    [Fact]
    public async Task MissingProblem_ReturnsNotFound()
    {
        _problemRepository
            .Setup(repository => repository.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        Result result = await _handler.Handle(
            new RemoveProblemSetupCommand(Guid.NewGuid(), Guid.NewGuid()),
            default
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task MissingSetupForThatLanguage_ReturnsNotFound()
    {
        Problem problem = SetUpProblem();

        Result result = await RemoveAsync(problem, Guid.NewGuid());

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task RetiresOnlyTheRequestedLanguageSetup()
    {
        Problem problem = CreateProblem();
        Guid javaVersionId = Guid.NewGuid();
        Guid pythonVersionId = Guid.NewGuid();
        problem.AddSetup(javaVersionId, "// java", "minCoins", Guid.NewGuid());
        problem.AddSetup(pythonVersionId, "# python", "min_coins", Guid.NewGuid());
        SetUpProblem(problem);

        Result result = await RemoveAsync(problem, javaVersionId);

        Assert.True(result.IsSuccess);
        Assert.Null(problem.FindSetupByLanguageVersionId(javaVersionId));
        Assert.NotNull(problem.FindSetupByLanguageVersionId(pythonVersionId));
        _problemRepository.Verify(
            repository => repository.UpdateAsync(problem, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task KeepsTheRetiredSetupRowSoHistoryCanStillReferenceIt()
    {
        Problem problem = CreateProblem();
        Guid javaVersionId = Guid.NewGuid();
        ProblemSetup setup = problem.AddSetup(javaVersionId, "// java", "minCoins", Guid.NewGuid());
        SetUpProblem(problem);

        await RemoveAsync(problem, javaVersionId);

        ProblemSetup retained = Assert.Single(problem.Setups);
        Assert.Equal(setup.Id, retained.Id);
        Assert.True(retained.IsRetired);
        Assert.Empty(problem.ActiveSetups());
    }

    [Fact]
    public async Task RetiredLanguageCanBeAddedAgain()
    {
        Problem problem = CreateProblem();
        Guid javaVersionId = Guid.NewGuid();
        problem.AddSetup(javaVersionId, "// java", "minCoins", Guid.NewGuid());
        SetUpProblem(problem);

        await RemoveAsync(problem, javaVersionId);
        ProblemSetup replacement = problem.AddSetup(javaVersionId, "// java v2", "minCoins", Guid.NewGuid());

        Assert.Equal(2, problem.Setups.Count);
        Assert.Equal(replacement.Id, problem.FindSetupByLanguageVersionId(javaVersionId)?.Id);
        Assert.Single(problem.ActiveSetups());
    }

    [Fact]
    public async Task EditLockedProblem_IsRejected()
    {
        Problem problem = CreateProblem();
        Guid javaVersionId = Guid.NewGuid();
        problem.AddSetup(javaVersionId, "// java", "minCoins", Guid.NewGuid());
        problem.SubmitForValidation();
        SetUpProblem(problem);

        Result result = await RemoveAsync(problem, javaVersionId);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.NotNull(problem.FindSetupByLanguageVersionId(javaVersionId));
    }
}