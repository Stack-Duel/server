using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.ExecutionPipelines.Enums;
using StackDuel.Domain.SubmissionJobs;
using StackDuel.Domain.Submissions;
using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.ValueObjects;
using StackDuel.Domain.TestSuites;
using Ardalis.Result;
using Moq;
using GetSubmissionStatusHandler = StackDuel.Application.Queries.Submissions.GetSubmissionStatus.GetSubmissionStatusHandler;
using GetSubmissionStatusQuery = StackDuel.Application.Queries.Submissions.GetSubmissionStatus.GetSubmissionStatusQuery;

namespace StackDuel.Application.Tests.Queries.Submissions.GetSubmissionStatus;

public class GetSubmissionStatusHandlerTests
{
    private Mock<ISubmissionWriteRepository> _submissionRepository = null!;
    private Mock<ITestSuiteWriteRepository> _testSuiteRepository = null!;
    private Mock<ISubmissionJobRepository> _submissionJobRepository = null!;
    private Mock<IExecutionPipelineRepository> _pipelineRepository = null!;
    private GetSubmissionStatusHandler _handler = null!;

    private static readonly Guid TestCaseId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _submissionRepository = new Mock<ISubmissionWriteRepository>();
        _testSuiteRepository = new Mock<ITestSuiteWriteRepository>();
        _submissionJobRepository = new Mock<ISubmissionJobRepository>();
        _pipelineRepository = new Mock<IExecutionPipelineRepository>();

        _handler = new GetSubmissionStatusHandler(
            _submissionRepository.Object,
            _testSuiteRepository.Object,
            _submissionJobRepository.Object,
            _pipelineRepository.Object
        );

        _testSuiteRepository
            .Setup(x =>
                x.FindExpectedOutputsByTestCaseIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync([]);
        _testSuiteRepository
            .Setup(x => x.FindInputsByTestCaseIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);
    }

    private static Submission CreateSubmission(Guid userId) =>
        new(userId, Guid.NewGuid(), SubmissionType.Submit, new SourceCode("print(1)"), [TestCaseId]);

    [Test]
    public async Task Handle_SubmissionNotFound_ReturnsNotFound()
    {
        var submissionId = Guid.NewGuid();
        _submissionRepository
            .Setup(x => x.FindByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Submission?)null);

        var result = await _handler.Handle(
            new GetSubmissionStatusQuery(submissionId, Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_SubmissionBelongsToDifferentUser_ReturnsNotFound()
    {
        var owner = Guid.NewGuid();
        var submission = CreateSubmission(owner);
        var submissionId = Guid.NewGuid();

        _submissionRepository
            .Setup(x => x.FindByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);

        var result = await _handler.Handle(
            new GetSubmissionStatusQuery(submissionId, Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_NoJob_ReturnsDtoWithNullCurrentStepName()
    {
        var userId = Guid.NewGuid();
        var submission = CreateSubmission(userId);
        var submissionId = Guid.NewGuid();

        _submissionRepository
            .Setup(x => x.FindByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionJobRepository
            .Setup(x => x.FindBySubmissionIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubmissionJob?)null);

        var result = await _handler.Handle(new GetSubmissionStatusQuery(submissionId, userId), CancellationToken.None);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.CurrentStepName, Is.Null);
            Assert.That(result.Value.SubmissionId, Is.EqualTo(submission.Id));
            Assert.That(result.Value.Results, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public async Task Handle_WithActiveJob_ResolvesCurrentStepNameFromPipeline()
    {
        var userId = Guid.NewGuid();
        var submission = CreateSubmission(userId);
        var submissionId = Guid.NewGuid();

        var pipeline = new ExecutionPipeline("standard");
        var step = pipeline.AddStep(ExecutionPipelineStepType.Judge0Poll, 1, 3, 30, true, "polling judge0");
        var job = new SubmissionJob(submission.Id, Guid.NewGuid(), step.Id);

        _submissionRepository
            .Setup(x => x.FindByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionJobRepository
            .Setup(x => x.FindBySubmissionIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _pipelineRepository
            .Setup(x => x.FindByIdWithStepsAsync(job.PipelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipeline);

        var result = await _handler.Handle(new GetSubmissionStatusQuery(submissionId, userId), CancellationToken.None);

        Assert.That(result.Value.CurrentStepName, Is.EqualTo("polling judge0"));
    }

    [Test]
    public async Task Handle_MapsResultInputsAndExpectedOutputsFromTestSuiteRepository()
    {
        var userId = Guid.NewGuid();
        var submission = CreateSubmission(userId);
        submission.UpdateResult(TestCaseId, SubmissionResultStatus.WrongAnswer, actualOutput: "3");
        var submissionId = Guid.NewGuid();

        _submissionRepository
            .Setup(x => x.FindByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionJobRepository
            .Setup(x => x.FindBySubmissionIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubmissionJob?)null);
        _testSuiteRepository
            .Setup(x => x.FindInputsByTestCaseIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, string> { [TestCaseId] = "2 3" });
        _testSuiteRepository
            .Setup(x =>
                x.FindExpectedOutputsByTestCaseIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(new Dictionary<Guid, string> { [TestCaseId] = "5" });

        var result = await _handler.Handle(new GetSubmissionStatusQuery(submissionId, userId), CancellationToken.None);

        var resultDto = result.Value.Results.Single();
        Assert.Multiple(() =>
        {
            Assert.That(resultDto.Input, Is.EqualTo("2 3"));
            Assert.That(resultDto.ExpectedOutput, Is.EqualTo("5"));
            Assert.That(resultDto.ActualOutput, Is.EqualTo("3"));
            Assert.That(resultDto.Status, Is.EqualTo(SubmissionResultStatus.WrongAnswer));
        });
    }
}