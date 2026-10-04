using StackDuel.Application.Submissions;
using StackDuel.Application.Submissions.Dtos;
using StackDuel.Domain.ExecutionPipelines;
using StackDuel.Domain.ExecutionPipelines.Entities;
using StackDuel.Domain.ExecutionPipelines.Enums;
using StackDuel.Domain.SubmissionJobs;
using StackDuel.Domain.Submissions;
using StackDuel.Domain.Submissions.Entities;
using StackDuel.Domain.Submissions.Enums;
using StackDuel.Domain.Submissions.ValueObjects;
using Ardalis.Result;
using Moq;
using GetAdminSubmissionDetailHandler = StackDuel.Application.Queries.Submissions.GetAdminSubmissionDetail.GetAdminSubmissionDetailHandler;
using GetAdminSubmissionDetailQuery = StackDuel.Application.Queries.Submissions.GetAdminSubmissionDetail.GetAdminSubmissionDetailQuery;

namespace StackDuel.Application.Tests.Queries.Submissions.GetAdminSubmissionDetail;

public class GetAdminSubmissionDetailHandlerTests
{
    private Mock<ISubmissionWriteRepository> _submissionRepository = null!;
    private Mock<ISubmissionReadRepository> _submissionReadRepository = null!;
    private Mock<ISubmissionJobRepository> _submissionJobRepository = null!;
    private Mock<IExecutionPipelineRepository> _pipelineRepository = null!;
    private GetAdminSubmissionDetailHandler _handler = null!;

    private static readonly Guid TestCaseId = Guid.NewGuid();

    [SetUp]
    public void SetUp()
    {
        _submissionRepository = new Mock<ISubmissionWriteRepository>();
        _submissionReadRepository = new Mock<ISubmissionReadRepository>();
        _submissionJobRepository = new Mock<ISubmissionJobRepository>();
        _pipelineRepository = new Mock<IExecutionPipelineRepository>();

        _handler = new GetAdminSubmissionDetailHandler(
            _submissionRepository.Object,
            _submissionReadRepository.Object,
            _submissionJobRepository.Object,
            _pipelineRepository.Object
        );
    }

    private static Submission CreateSubmission()
    {
        var submission = new Submission(
            Guid.NewGuid(),
            Guid.NewGuid(),
            SubmissionType.Submit,
            new SourceCode("print(1)"),
            [TestCaseId]
        );
        submission.UpdateResult(
            TestCaseId,
            SubmissionResultStatus.Accepted,
            runtime: 12,
            memoryUsed: 256,
            actualOutput: "1"
        );
        return submission;
    }

    private static AdminSubmissionListItemDto CreateContext(Submission submission) =>
        new(
            submission.Id,
            submission.Type,
            submission.Status,
            submission.ProblemSetupId,
            Guid.NewGuid(),
            "Two Sum",
            "two-sum",
            new SubmissionLanguageDto(Guid.NewGuid(), "Python", "3.12"),
            new SubmissionUserDto("alice", null),
            submission.CreatedAt,
            submission.MemoryUsage,
            submission.ExecutionTime
        );

    [Test]
    public async Task Handle_SubmissionNotFound_ReturnsNotFound()
    {
        var submissionId = Guid.NewGuid();
        _submissionRepository
            .Setup(x => x.FindByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Submission?)null);

        Result<AdminSubmissionDetailDto> result = await _handler.Handle(
            new GetAdminSubmissionDetailQuery(submissionId),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_AdminContextNotFound_ReturnsNotFound()
    {
        var submissionId = Guid.NewGuid();
        var submission = CreateSubmission();

        _submissionRepository
            .Setup(x => x.FindByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionReadRepository
            .Setup(x => x.FindAdminSubmissionByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminSubmissionListItemDto?)null);

        Result<AdminSubmissionDetailDto> result = await _handler.Handle(
            new GetAdminSubmissionDetailQuery(submissionId),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_NoJob_ReturnsDtoWithNullJobAndMappedResults()
    {
        var submissionId = Guid.NewGuid();
        var submission = CreateSubmission();
        var context = CreateContext(submission);

        _submissionRepository
            .Setup(x => x.FindByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionReadRepository
            .Setup(x => x.FindAdminSubmissionByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);
        _submissionJobRepository
            .Setup(x => x.FindBySubmissionIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((SubmissionJob?)null);

        Result<AdminSubmissionDetailDto> result = await _handler.Handle(
            new GetAdminSubmissionDetailQuery(submissionId),
            CancellationToken.None
        );

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value.Job, Is.Null);
            Assert.That(result.Value.Id, Is.EqualTo(submission.Id));
            Assert.That(result.Value.ProblemTitle, Is.EqualTo("Two Sum"));
            Assert.That(result.Value.MemoryUsage, Is.EqualTo(256));
            Assert.That(result.Value.ExecutionTime, Is.EqualTo(12));
            Assert.That(result.Value.Results, Has.Count.EqualTo(1));
            Assert.That(result.Value.Results[0].Status, Is.EqualTo(SubmissionResultStatus.Accepted));
        });
    }

    [Test]
    public async Task Handle_WithJob_DerivesStepStatusesForPendingRunningSucceededAndFailedSteps()
    {
        var submissionId = Guid.NewGuid();
        var submission = CreateSubmission();
        var context = CreateContext(submission);

        var pipeline = new ExecutionPipeline("standard");
        var step1 = pipeline.AddStep(ExecutionPipelineStepType.Judge0Execute, 1, 3, 30, false, "execute");
        var step2 = pipeline.AddStep(ExecutionPipelineStepType.Judge0Poll, 2, 3, 30, false, "poll");
        var step3 = pipeline.AddStep(ExecutionPipelineStepType.Evaluate, 3, 3, 30, false, "evaluate");

        var job = new SubmissionJob(submission.Id, Guid.NewGuid(), step1.Id);
        var step1Attempt = job.StartAttempt();
        step1Attempt.Succeed("req", "res");
        job.AdvanceTo(step2.Id);
        var step2Attempt = job.StartAttempt();
        step2Attempt.Fail("boom");
        job.Fail("boom");

        _submissionRepository
            .Setup(x => x.FindByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(submission);
        _submissionReadRepository
            .Setup(x => x.FindAdminSubmissionByIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);
        _submissionJobRepository
            .Setup(x => x.FindBySubmissionIdAsync(submissionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        _pipelineRepository
            .Setup(x => x.FindByIdWithStepsAsync(job.PipelineId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pipeline);

        Result<AdminSubmissionDetailDto> result = await _handler.Handle(
            new GetAdminSubmissionDetailQuery(submissionId),
            CancellationToken.None
        );

        Assert.That(result.Value.Job, Is.Not.Null);
        var steps = result.Value.Job!.Steps;

        Assert.Multiple(() =>
        {
            Assert.That(steps[0].Status, Is.EqualTo(AdminSubmissionJobStepStatus.Succeeded));
            Assert.That(steps[1].Status, Is.EqualTo(AdminSubmissionJobStepStatus.Failed));
            Assert.That(steps[2].Status, Is.EqualTo(AdminSubmissionJobStepStatus.Pending));
            Assert.That(steps[1].IsCurrent, Is.True);
            Assert.That(steps[0].AttemptCount, Is.EqualTo(1));
            Assert.That(steps[2].AttemptCount, Is.EqualTo(0));
        });
    }
}