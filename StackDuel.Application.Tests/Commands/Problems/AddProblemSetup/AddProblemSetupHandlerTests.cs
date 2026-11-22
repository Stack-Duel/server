using Ardalis.Result;
using Moq;
using StackDuel.Application.Commands.Problems.AddProblemSetup;
using StackDuel.Application.Problems;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;

namespace StackDuel.Application.Tests.Commands.Problems.AddProblemSetup;

public class AddProblemSetupHandlerTests
{
    private readonly Mock<IProblemRepository> _problemRepository = new();
    private readonly Mock<IExecutionPipelineRepository> _executionPipelineRepository = new();
    private readonly Mock<IProblemTrackLanguagePolicy> _trackLanguagePolicy = new();
    private readonly AddProblemSetupHandler _handler;

    public AddProblemSetupHandlerTests()
    {
        _handler = new AddProblemSetupHandler(
            new AddProblemSetupValidator(),
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

    private Task<Result<Guid>> AddAsync(Problem problem, Guid languageVersionId, Guid? requestedByUserId = null) =>
        _handler.Handle(new AddProblemSetupCommand(problem.Id, languageVersionId, requestedByUserId), default);

    [Fact]
    public async Task MissingProblem_ReturnsNotFound()
    {
        _problemRepository
            .Setup(repository => repository.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        Result<Guid> result = await _handler.Handle(
            new AddProblemSetupCommand(Guid.NewGuid(), Guid.NewGuid()),
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

        Result<Guid> result = await AddAsync(problem, Guid.NewGuid());

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task AProblemMidValidation_CannotGainASetup()
    {
        Problem problem = CreateProblem();
        problem.SubmitForValidation();
        SetUpProblem(problem);

        Result<Guid> result = await AddAsync(problem, Guid.NewGuid());

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(result.ValidationErrors, error => error.Identifier == "Status");
        Assert.Empty(problem.Setups);
    }

    [Fact]
    public async Task ASecondSetupForTheSameLanguage_IsRejected()
    {
        Guid languageVersionId = Guid.NewGuid();
        Problem problem = CreateProblem();
        problem.AddSetup(languageVersionId, "code", functionName: null, Guid.NewGuid());
        SetUpProblem(problem);

        Result<Guid> result = await AddAsync(problem, languageVersionId);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(result.ValidationErrors, error => error.Identifier == "LanguageVersionId");
        Assert.Single(problem.Setups);
    }

    [Fact]
    public async Task ALanguageOutsideTheProblemsTrack_IsRejectedForANonAdminRequest()
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

        Result<Guid> result = await AddAsync(problem, Guid.NewGuid(), Guid.NewGuid());

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(result.ValidationErrors, error => error.Identifier == "LanguageVersionId");
    }

    [Fact]
    public async Task AnAdminRequest_SkipsTheTrackLanguageCheck()
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

        Result<Guid> result = await AddAsync(problem, Guid.NewGuid(), requestedByUserId: null);

        Assert.True(result.IsSuccess);
        _trackLanguagePolicy.Verify(
            policy =>
                policy.AllowsLanguageVersionAsync(
                    It.IsAny<Problem>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task NoConfiguredPipeline_IsAnError()
    {
        Problem problem = SetUpProblem();
        _executionPipelineRepository
            .Setup(repository => repository.FindIdByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        Result<Guid> result = await AddAsync(problem, Guid.NewGuid());

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Empty(problem.Setups);
    }

    [Fact]
    public async Task ANewSetup_IsAddedWithPlaceholderCodeAndPersisted()
    {
        Guid languageVersionId = Guid.NewGuid();
        Guid pipelineId = Guid.NewGuid();
        Problem problem = SetUpProblem();
        _executionPipelineRepository
            .Setup(repository => repository.FindIdByNameAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipelineId);

        Result<Guid> result = await AddAsync(problem, languageVersionId);

        ProblemSetup setup = Assert.Single(problem.Setups);
        Assert.Equal(setup.Id, result.Value);
        Assert.Equal(languageVersionId, setup.LanguageVersionId);
        Assert.Equal(pipelineId, setup.PipelineId);
        Assert.False(string.IsNullOrWhiteSpace(setup.InitialCode));
        Assert.Null(setup.FunctionName);
        _problemRepository.Verify(
            repository => repository.UpdateAsync(problem, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task AnEmptyLanguageVersionId_FailsValidation()
    {
        Problem problem = SetUpProblem();

        Result<Guid> result = await AddAsync(problem, Guid.Empty);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Empty(problem.Setups);
    }
}