using Ardalis.Result;
using Moq;
using StackDuel.Application.Commands.Problems.UpsertProblemSetupReferenceSolution;
using StackDuel.Application.Problems;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Application.Tests.Commands.Problems.UpsertProblemSetupReferenceSolution;

public class UpsertProblemSetupReferenceSolutionHandlerTests
{
    private const string StarterCode = "function solve() {}";
    private const string ReferenceSolution = "function solve() { return 1; }";

    private readonly Mock<IProblemRepository> _problemRepository = new();
    private readonly Mock<IExecutionPipelineRepository> _executionPipelineRepository = new();
    private readonly Mock<IProblemTrackLanguagePolicy> _trackLanguagePolicy = new();
    private readonly UpsertProblemSetupReferenceSolutionHandler _handler;

    public UpsertProblemSetupReferenceSolutionHandlerTests()
    {
        _handler = new UpsertProblemSetupReferenceSolutionHandler(
            new UpsertProblemSetupReferenceSolutionValidator(),
            _problemRepository.Object,
            _executionPipelineRepository.Object,
            _trackLanguagePolicy.Object
        );

        _executionPipelineRepository
            .Setup(repository => repository.FindIdByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Guid.NewGuid());

        _trackLanguagePolicy
            .Setup(policy =>
                policy.AllowsLanguageVersionAsync(
                    It.IsAny<Problem>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);
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

    private Task<Result<Guid>> UpsertAsync(
        Problem problem,
        Guid languageVersionId,
        string? starterCode = null,
        string? functionName = "solve",
        string? referenceSolution = null,
        Guid? requestedByUserId = null
    ) =>
        _handler.Handle(
            new UpsertProblemSetupReferenceSolutionCommand(
                problem.Id,
                languageVersionId,
                starterCode ?? StarterCode,
                functionName,
                referenceSolution ?? ReferenceSolution,
                requestedByUserId
            ),
            default
        );

    [Fact]
    public async Task MissingProblem_ReturnsNotFound()
    {
        _problemRepository
            .Setup(repository => repository.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        Result<Guid> result = await _handler.Handle(
            new UpsertProblemSetupReferenceSolutionCommand(
                Guid.NewGuid(),
                Guid.NewGuid(),
                StarterCode,
                "solve",
                ReferenceSolution
            ),
            default
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task ADeletedProblem_ReturnsNotFound()
    {
        Problem problem = CreateProblem();
        problem.Delete();
        SetUpProblem(problem);

        Result<Guid> result = await UpsertAsync(problem, Guid.NewGuid());

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task AProblemMidValidation_CannotHaveItsSolutionChanged()
    {
        Problem problem = CreateProblem();
        problem.SubmitForValidation();
        SetUpProblem(problem);

        Result<Guid> result = await UpsertAsync(problem, Guid.NewGuid());

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(result.ValidationErrors, error => error.Identifier == "Status");
        Assert.Empty(problem.Setups);
    }

    [Fact]
    public async Task AMissingSetupForALanguageOutsideTheTrack_IsRejectedForANonAdminRequest()
    {
        Problem problem = SetUpProblem();
        _trackLanguagePolicy
            .Setup(policy =>
                policy.AllowsLanguageVersionAsync(
                    It.IsAny<Problem>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(false);

        Result<Guid> result = await UpsertAsync(problem, Guid.NewGuid(), requestedByUserId: Guid.NewGuid());

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(result.ValidationErrors, error => error.Identifier == "LanguageVersionId");
        Assert.Empty(problem.Setups);
    }

    [Fact]
    public async Task AnExistingSetup_SkipsTheTrackLanguageCheck()
    {
        Guid languageVersionId = Guid.NewGuid();
        Problem problem = CreateProblem();
        problem.AddSetup(languageVersionId, "old code", "oldName", Guid.NewGuid());
        SetUpProblem(problem);

        _trackLanguagePolicy
            .Setup(policy =>
                policy.AllowsLanguageVersionAsync(
                    It.IsAny<Problem>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(false);

        Result<Guid> result = await UpsertAsync(problem, languageVersionId, requestedByUserId: Guid.NewGuid());

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task NoConfiguredPipeline_IsAnErrorWhenTheSetupMustBeCreated()
    {
        Problem problem = SetUpProblem();
        _executionPipelineRepository
            .Setup(repository => repository.FindIdByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        Result<Guid> result = await UpsertAsync(problem, Guid.NewGuid());

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Empty(problem.Setups);
    }

    [Fact]
    public async Task AFirstSolutionForALanguage_CreatesTheSetup()
    {
        Guid languageVersionId = Guid.NewGuid();
        Guid pipelineId = Guid.NewGuid();
        Problem problem = SetUpProblem();
        _executionPipelineRepository
            .Setup(repository => repository.FindIdByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipelineId);

        Result<Guid> result = await UpsertAsync(problem, languageVersionId);

        ProblemSetup setup = Assert.Single(problem.Setups);
        Assert.Equal(setup.Id, result.Value);
        Assert.Equal(pipelineId, setup.PipelineId);
        Assert.Equal(StarterCode, setup.InitialCode);
        Assert.Equal("solve", setup.FunctionName);
        Assert.Equal(ReferenceSolution, setup.ReferenceSolutionCode);
        _problemRepository.Verify(
            repository => repository.UpdateAsync(problem, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task AnExistingSetup_IsUpdatedInPlaceRatherThanDuplicated()
    {
        Guid languageVersionId = Guid.NewGuid();
        Problem problem = CreateProblem();
        ProblemSetup existing = problem.AddSetup(languageVersionId, "old code", "oldName", Guid.NewGuid());
        SetUpProblem(problem);

        Result<Guid> result = await UpsertAsync(
            problem,
            languageVersionId,
            starterCode: "new starter",
            functionName: "newName",
            referenceSolution: "new solution"
        );

        ProblemSetup setup = Assert.Single(problem.Setups);
        Assert.Equal(existing.Id, result.Value);
        Assert.Equal("new starter", setup.InitialCode);
        Assert.Equal("newName", setup.FunctionName);
        Assert.Equal("new solution", setup.ReferenceSolutionCode);
    }

    [Fact]
    public async Task ANullFunctionName_IsAccepted()
    {
        Guid languageVersionId = Guid.NewGuid();
        Problem problem = CreateProblem();
        problem.AddSetup(languageVersionId, "old code", "oldName", Guid.NewGuid());
        SetUpProblem(problem);

        Result<Guid> result = await UpsertAsync(problem, languageVersionId, functionName: null);

        Assert.True(result.IsSuccess);
        Assert.Null(problem.Setups.Single().FunctionName);
    }

    [Fact]
    public async Task AnEmptyReferenceSolution_FailsValidation()
    {
        Problem problem = SetUpProblem();

        Result<Guid> result = await UpsertAsync(problem, Guid.NewGuid(), referenceSolution: " ");

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Empty(problem.Setups);
    }

    [Fact]
    public async Task AnEmptyStarterCode_FailsValidation()
    {
        Problem problem = SetUpProblem();

        Result<Guid> result = await UpsertAsync(problem, Guid.NewGuid(), starterCode: " ");

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Empty(problem.Setups);
    }
}