using Ardalis.Result;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.ExecutionPipelines.Enums;
using StackDuel.Domain.SeedWork;
using StackDuel.Domain.SubmissionJobs;
using StackDuel.Domain.Submissions;
using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.Factories;
using StackDuel.Domain.TestSuites;
using CreateSubmissionCommand = StackDuel.Application.Commands.Submissions.CreateSubmission.CreateSubmissionCommand;
using CreateSubmissionHandler = StackDuel.Application.Commands.Submissions.CreateSubmission.CreateSubmissionHandler;
using CreateSubmissionValidator = StackDuel.Application.Commands.Submissions.CreateSubmission.CreateSubmissionValidator;

namespace StackDuel.Application.Tests.Commands.Submissions.CreateSubmission;

public class CreateSubmissionHandlerTests
{
    private const int OfficialGradingCaseCount = 20;

    private Mock<ISubmissionWriteRepository> _submissionRepository = null!;
    private Mock<ISubmissionJobRepository> _submissionJobRepository = null!;
    private Mock<IExecutionPipelineRepository> _pipelineRepository = null!;
    private Mock<ITestSuiteWriteRepository> _testSuiteRepository = null!;
    private Mock<IDomainEventDispatcher> _domainEventDispatcher = null!;
    private CreateSubmissionHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _submissionRepository = new Mock<ISubmissionWriteRepository>();
        _submissionJobRepository = new Mock<ISubmissionJobRepository>();
        _pipelineRepository = new Mock<IExecutionPipelineRepository>();
        _testSuiteRepository = new Mock<ITestSuiteWriteRepository>();
        _domainEventDispatcher = new Mock<IDomainEventDispatcher>();

        _handler = new CreateSubmissionHandler(
            new CreateSubmissionValidator(),
            new SubmissionFactory(),
            _submissionRepository.Object,
            _submissionJobRepository.Object,
            _pipelineRepository.Object,
            _testSuiteRepository.Object,
            _domainEventDispatcher.Object
        );
    }

    private static CreateSubmissionCommand ValidCommand(
        SubmissionType type = SubmissionType.Run,
        IReadOnlyCollection<CreateSubmissionCustomTestCaseDto>? customTestCases = null,
        Guid? gameId = null
    ) => new(Guid.NewGuid(), type, "return 42;", Guid.NewGuid(), customTestCases, GameId: gameId);

    private static ExecutionPipeline CreatePipelineWithStep()
    {
        var pipeline = new ExecutionPipeline("default");
        pipeline.AddStep(ExecutionPipelineStepType.Judge0Execute, 1, 3, 10, false, "execute");
        return pipeline;
    }

    private void SetUpRunTypeTestCases(params Guid[] publicCaseIds)
    {
        _testSuiteRepository
            .Setup(x => x.FindPublicTestCaseIdsByProblemSetupIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(publicCaseIds);
        _testSuiteRepository
            .Setup(x =>
                x.CreateAdHocTestCasesAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IReadOnlyCollection<IReadOnlyCollection<string>>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([]);
    }

    private void SetUpPipeline(Guid pipelineId, ExecutionPipeline pipeline)
    {
        _testSuiteRepository
            .Setup(x => x.FindPipelineIdByProblemSetupIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipelineId);
        _pipelineRepository
            .Setup(x => x.FindByIdWithStepsAsync(pipelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipeline);
    }

    [Test]
    public async Task Handle_RunType_Success_ReturnsSubmissionIdAndPersists()
    {
        SetUpRunTypeTestCases(Guid.NewGuid());
        var pipeline = CreatePipelineWithStep();
        SetUpPipeline(Guid.NewGuid(), pipeline);

        Result<Guid> result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Not.EqualTo(Guid.Empty));
        _submissionRepository.Verify(
            x => x.AddAsync(It.IsAny<Submission>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        _submissionJobRepository.Verify(
            x =>
                x.AddAsync(
                    It.Is<SubmissionJob>(j =>
                        j.PipelineId == pipeline.Id && j.CurrentStepId == pipeline.FirstStep()!.Id
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        _domainEventDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_WithGameId_PersistsSubmissionWithGameId()
    {
        SetUpRunTypeTestCases(Guid.NewGuid());
        SetUpPipeline(Guid.NewGuid(), CreatePipelineWithStep());
        var gameId = Guid.NewGuid();

        await _handler.Handle(ValidCommand(gameId: gameId), CancellationToken.None);

        _submissionRepository.Verify(
            x => x.AddAsync(It.Is<Submission>(s => s.GameId == gameId), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_RunType_PassesCustomTestCaseInputsToRepository()
    {
        SetUpRunTypeTestCases(Guid.NewGuid());
        SetUpPipeline(Guid.NewGuid(), CreatePipelineWithStep());

        var customTestCases = new List<CreateSubmissionCustomTestCaseDto> { new(["1", "2"]) };

        await _handler.Handle(ValidCommand(customTestCases: customTestCases), CancellationToken.None);

        _testSuiteRepository.Verify(
            x =>
                x.CreateAdHocTestCasesAsync(
                    It.IsAny<Guid>(),
                    It.Is<IReadOnlyCollection<IReadOnlyCollection<string>>>(cases =>
                        cases.Count == 1 && cases.First().SequenceEqual(new[] { "1", "2" })
                    ),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_SubmitType_UsesGradingTestCaseIdsAndSkipsRunTypeLookups()
    {
        _testSuiteRepository
            .Setup(x =>
                x.FindGradingTestCaseIdsByProblemSetupIdAsync(
                    It.IsAny<Guid>(),
                    OfficialGradingCaseCount,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([Guid.NewGuid()]);
        SetUpPipeline(Guid.NewGuid(), CreatePipelineWithStep());

        Result<Guid> result = await _handler.Handle(ValidCommand(type: SubmissionType.Submit), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        _testSuiteRepository.Verify(
            x => x.FindPublicTestCaseIdsByProblemSetupIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _testSuiteRepository.Verify(
            x =>
                x.CreateAdHocTestCasesAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<IReadOnlyCollection<IReadOnlyCollection<string>>>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_NoTestCasesAvailable_ReturnsErrorAndDoesNotLookUpPipeline()
    {
        SetUpRunTypeTestCases();

        Result<Guid> result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Error));
        _testSuiteRepository.Verify(
            x => x.FindPipelineIdByProblemSetupIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _submissionRepository.Verify(
            x => x.AddAsync(It.IsAny<Submission>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_PipelineIdNotFound_ReturnsNotFound()
    {
        SetUpRunTypeTestCases(Guid.NewGuid());
        _testSuiteRepository
            .Setup(x => x.FindPipelineIdByProblemSetupIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid?)null);

        Result<Guid> result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
        _submissionRepository.Verify(
            x => x.AddAsync(It.IsAny<Submission>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_PipelineNotFound_ReturnsNotFound()
    {
        SetUpRunTypeTestCases(Guid.NewGuid());
        var pipelineId = Guid.NewGuid();
        _testSuiteRepository
            .Setup(x => x.FindPipelineIdByProblemSetupIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipelineId);
        _pipelineRepository
            .Setup(x => x.FindByIdWithStepsAsync(pipelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExecutionPipeline?)null);

        Result<Guid> result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_PipelineHasNoSteps_ReturnsError()
    {
        SetUpRunTypeTestCases(Guid.NewGuid());
        SetUpPipeline(Guid.NewGuid(), new ExecutionPipeline("empty"));

        Result<Guid> result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Error));
        _submissionRepository.Verify(
            x => x.AddAsync(It.IsAny<Submission>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Test]
    public async Task Handle_InvalidCommand_ReturnsInvalidAndDoesNotTouchRepositories()
    {
        var command = ValidCommand() with { Code = "" };

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        _testSuiteRepository.Verify(
            x => x.FindPublicTestCaseIdsByProblemSetupIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}