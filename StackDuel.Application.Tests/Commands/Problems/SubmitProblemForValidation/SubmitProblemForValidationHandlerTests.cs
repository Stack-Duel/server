using Ardalis.Result;
using Moq;
using StackDuel.Application.Messaging;
using StackDuel.Application.Messaging.Messages;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.ProblemValidation;
using StackDuel.Domain.ProblemValidation.Entities;
using StackDuel.Domain.TestCaseGeneration.ValueObjects;
using StackDuel.Domain.TestSuites.Entities;
using StackDuel.Domain.TestSuites.Enums;
using System.Reflection;
using SubmitProblemForValidationCommand = StackDuel.Application.Commands.Problems.SubmitProblemForValidation.SubmitProblemForValidationCommand;
using SubmitProblemForValidationHandler = StackDuel.Application.Commands.Problems.SubmitProblemForValidation.SubmitProblemForValidationHandler;
using SubmitProblemForValidationValidator = StackDuel.Application.Commands.Problems.SubmitProblemForValidation.SubmitProblemForValidationValidator;

namespace StackDuel.Application.Tests.Commands.Problems.SubmitProblemForValidation;

public class SubmitProblemForValidationHandlerTests
{
    private readonly Mock<IProblemRepository> _problemRepository = new();
    private readonly Mock<IProblemValidationJobRepository> _problemValidationJobRepository = new();
    private readonly Mock<IMessagePublisher> _messagePublisher = new();
    private readonly SubmitProblemForValidationHandler _handler;

    public SubmitProblemForValidationHandlerTests()
    {
        _handler = new SubmitProblemForValidationHandler(
            new SubmitProblemForValidationValidator(),
            _problemRepository.Object,
            _problemValidationJobRepository.Object,
            _messagePublisher.Object
        );
    }

    private static Problem CreateDraftProblem() =>
        new(
            new Slug("two-sum"),
            new Title("Two Sum"),
            new Question(new string('a', Question.MinLength)),
            new Difficulty(500),
            new TimeLimit(1000),
            new MemoryLimit(64)
        );

    private static ProblemSetup AddSetupWithReferenceSolution(Problem problem)
    {
        ProblemSetup setup = problem.AddSetup(Guid.NewGuid(), "def twoSum():", "twoSum", Guid.NewGuid());
        problem.SetReferenceSolution(setup.Id, "return 1");
        return setup;
    }

    private static void AttachSampleTestCase(ProblemSetup setup, int inputCount = 1)
    {
        TestSuite sampleSuite = new("samples", TestSuiteType.Sample);
        TestCase sampleCase = sampleSuite.AddTestCase("first");

        for (int position = 0; position < inputCount; position++)
            sampleCase.AddInput("1", "integer");

        FieldInfo testSuitesField = typeof(ProblemSetup).GetField(
            "_testSuites",
            BindingFlags.Instance | BindingFlags.NonPublic
        )!;
        ((List<TestSuite>)testSuitesField.GetValue(setup)!).Add(sampleSuite);
    }

    private static void SetGenerationParameters(Problem problem) =>
        problem.SetGenerationParameters(
            [new GenerationParameterSpec("n", "int", 1, 10, null, null, null)],
            "int",
            25,
            99
        );

    private static Problem CreateReadyProblem()
    {
        Problem problem = CreateDraftProblem();
        AttachSampleTestCase(AddSetupWithReferenceSolution(problem));
        SetGenerationParameters(problem);
        return problem;
    }

    private void GivenProblem(Guid problemId, Problem? problem) =>
        _problemRepository.Setup(x => x.FindByIdAsync(problemId, It.IsAny<CancellationToken>())).ReturnsAsync(problem);

    private Task<Result> Handle(Guid problemId) =>
        _handler.Handle(new SubmitProblemForValidationCommand(problemId), CancellationToken.None);

    private static IReadOnlyList<string> ErrorMessages(Result result) =>
        [.. result.ValidationErrors.Select(error => error.ErrorMessage)];

    [Fact]
    public async Task Handle_EmptyProblemId_ReturnsInvalidWithoutLoadingProblem()
    {
        Result result = await Handle(Guid.Empty);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _problemRepository.Verify(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ProblemMissing_ReturnsNotFound()
    {
        Guid problemId = Guid.NewGuid();
        GivenProblem(problemId, null);

        Result result = await Handle(problemId);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Theory]
    [InlineData(ProblemStatus.Published)]
    [InlineData(ProblemStatus.Archived)]
    [InlineData(ProblemStatus.Pending)]
    public async Task Handle_StatusNotSubmittable_ReturnsInvalidNamingTheStatus(ProblemStatus status)
    {
        Guid problemId = Guid.NewGuid();
        Problem problem = CreateReadyProblem();
        SetStatus(problem, status);
        GivenProblem(problemId, problem);

        Result result = await Handle(problemId);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains($"Cannot submit a problem for validation from status {status}.", ErrorMessages(result));
        _problemValidationJobRepository.Verify(
            x => x.AddAsync(It.IsAny<ProblemValidationJob>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_FailedProblem_IsSubmittableAgain()
    {
        Guid problemId = Guid.NewGuid();
        Problem problem = CreateReadyProblem();
        problem.FailValidation("reference solution timed out");
        GivenProblem(problemId, problem);

        Result result = await Handle(problemId);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(ProblemStatus.Pending, problem.Status);
    }

    [Fact]
    public async Task Handle_NothingConfigured_ReturnsEveryUnmetRequirement()
    {
        Guid problemId = Guid.NewGuid();
        GivenProblem(problemId, CreateDraftProblem());

        Result result = await Handle(problemId);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal(
            [
                "At least one language setup is required.",
                "At least one sample test case is required.",
                "Generation parameters are required before submitting for validation.",
            ],
            ErrorMessages(result)
        );
    }

    [Fact]
    public async Task Handle_SetupMissingReferenceSolution_ReturnsErrorNamingTheSetup()
    {
        Guid problemId = Guid.NewGuid();
        Problem problem = CreateDraftProblem();
        ProblemSetup setup = problem.AddSetup(Guid.NewGuid(), "def twoSum():", "twoSum", Guid.NewGuid());
        AttachSampleTestCase(setup);
        SetGenerationParameters(problem);
        GivenProblem(problemId, problem);

        Result result = await Handle(problemId);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal([$"A reference solution is required for setup {setup.Id}."], ErrorMessages(result));
    }

    [Fact]
    public async Task Handle_MissingSampleTestCases_ReturnsError()
    {
        Guid problemId = Guid.NewGuid();
        Problem problem = CreateDraftProblem();
        AddSetupWithReferenceSolution(problem);
        SetGenerationParameters(problem);
        GivenProblem(problemId, problem);

        Result result = await Handle(problemId);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal(["At least one sample test case is required."], ErrorMessages(result));
    }

    [Fact]
    public async Task Handle_MissingGenerationParameters_ReturnsError()
    {
        Guid problemId = Guid.NewGuid();
        Problem problem = CreateDraftProblem();
        AttachSampleTestCase(AddSetupWithReferenceSolution(problem));
        GivenProblem(problemId, problem);

        Result result = await Handle(problemId);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal(["Generation parameters are required before submitting for validation."], ErrorMessages(result));
    }

    [Fact]
    public async Task Handle_UnmetRequirements_DoesNotChangeStatusOrQueueJob()
    {
        Guid problemId = Guid.NewGuid();
        Problem problem = CreateDraftProblem();
        GivenProblem(problemId, problem);

        await Handle(problemId);

        Assert.Equal(ProblemStatus.Draft, problem.Status);
        _problemRepository.Verify(x => x.UpdateAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()), Times.Never);
        _problemValidationJobRepository.Verify(
            x => x.AddAsync(It.IsAny<ProblemValidationJob>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _messagePublisher.Verify(
            x => x.PublishAsync(It.IsAny<ProblemValidationJobContinuationMessage>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_EveryRequirementMet_ReturnsSuccessAndMovesProblemToPending()
    {
        Guid problemId = Guid.NewGuid();
        Problem problem = CreateReadyProblem();
        GivenProblem(problemId, problem);

        Result result = await Handle(problemId);

        Assert.Equal(ResultStatus.Ok, result.Status);
        Assert.Equal(ProblemStatus.Pending, problem.Status);
        _problemRepository.Verify(x => x.UpdateAsync(problem, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_EveryRequirementMet_QueuesValidationJobForTheProblem()
    {
        Guid problemId = Guid.NewGuid();
        Problem problem = CreateReadyProblem();
        GivenProblem(problemId, problem);

        await Handle(problemId);

        _problemValidationJobRepository.Verify(
            x =>
                x.AddAsync(
                    It.Is<ProblemValidationJob>(job => job.ProblemId == problem.Id),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_EveryRequirementMet_PublishesContinuationMessageForTheQueuedJob()
    {
        Guid problemId = Guid.NewGuid();
        GivenProblem(problemId, CreateReadyProblem());

        ProblemValidationJob? queuedJob = null;
        _problemValidationJobRepository
            .Setup(x => x.AddAsync(It.IsAny<ProblemValidationJob>(), It.IsAny<CancellationToken>()))
            .Callback<ProblemValidationJob, CancellationToken>((job, _) => queuedJob = job);

        await Handle(problemId);

        Assert.NotNull(queuedJob);
        _messagePublisher.Verify(
            x =>
                x.PublishAsync(
                    It.Is<ProblemValidationJobContinuationMessage>(message => message.JobId == queuedJob.Id),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    private static void SetStatus(Problem problem, ProblemStatus status)
    {
        switch (status)
        {
            case ProblemStatus.Published:
                problem.Publish();
                break;
            case ProblemStatus.Archived:
                problem.Archive();
                break;
            case ProblemStatus.Pending:
                problem.SubmitForValidation();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status), status, "Unsupported status for this test.");
        }
    }

    [Fact]
    public async Task Handle_GenerationParametersDoNotMatchTheSampleInputs_ReturnsInvalid()
    {
        Problem problem = CreateDraftProblem();
        AttachSampleTestCase(AddSetupWithReferenceSolution(problem), inputCount: 2);

        problem.SetGenerationParameters(
            [new GenerationParameterSpec("asdfa", "integer", 1, 4123, null, null, null)],
            "integer",
            20,
            12345
        );

        _problemRepository
            .Setup(repository => repository.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);

        Result result = await _handler.Handle(new SubmitProblemForValidationCommand(problem.Id), default);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(
            result.ValidationErrors,
            error => error.ErrorMessage.Contains("generation parameter(s) but each sample test case has")
        );
    }

    [Fact]
    public async Task Handle_GenerationParametersMatchTheSampleInputs_IsAccepted()
    {
        Problem problem = CreateReadyProblem();

        _problemRepository
            .Setup(repository => repository.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);

        Result result = await _handler.Handle(new SubmitProblemForValidationCommand(problem.Id), default);

        Assert.True(result.IsSuccess, string.Join("; ", result.ValidationErrors.Select(e => e.ErrorMessage)));
    }
}