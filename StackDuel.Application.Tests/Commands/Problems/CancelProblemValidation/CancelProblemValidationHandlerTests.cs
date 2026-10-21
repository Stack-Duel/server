using Ardalis.Result;
using Moq;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.ProblemValidation;
using StackDuel.Domain.ProblemValidation.Entities;
using StackDuel.Domain.ProblemValidation.Enums;
using CancelProblemValidationCommand = StackDuel.Application.Commands.Problems.CancelProblemValidation.CancelProblemValidationCommand;
using CancelProblemValidationHandler = StackDuel.Application.Commands.Problems.CancelProblemValidation.CancelProblemValidationHandler;
using CancelProblemValidationValidator = StackDuel.Application.Commands.Problems.CancelProblemValidation.CancelProblemValidationValidator;

namespace StackDuel.Application.Tests.Commands.Problems.CancelProblemValidation;

public class CancelProblemValidationHandlerTests
{
    private static readonly string ValidQuestion = new('q', 60);

    private Mock<IProblemRepository> _problemRepository = null!;
    private Mock<IProblemValidationJobRepository> _jobRepository = null!;
    private CancelProblemValidationHandler _handler = null!;

    public CancelProblemValidationHandlerTests()
    {
        _problemRepository = new Mock<IProblemRepository>();
        _jobRepository = new Mock<IProblemValidationJobRepository>();

        _handler = new CancelProblemValidationHandler(
            new CancelProblemValidationValidator(),
            _problemRepository.Object,
            _jobRepository.Object
        );
    }

    private static Problem CreateProblem() =>
        new(
            new Slug("two-sum"),
            new Title("Two Sum"),
            new Question(ValidQuestion),
            new Difficulty(100),
            new TimeLimit(1000),
            new MemoryLimit(256)
        );

    private Problem GivenPendingProblem()
    {
        Problem problem = CreateProblem();
        problem.SubmitForValidation();
        _problemRepository.Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(problem);

        return problem;
    }

    private ProblemValidationJob GivenUnfinishedJob(Guid problemId)
    {
        ProblemValidationJob job = new(problemId);
        _jobRepository
            .Setup(x => x.FindUnfinishedByProblemIdAsync(problemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        return job;
    }

    [Fact]
    public async Task Handle_PendingProblem_ReturnsItToDraft()
    {
        Problem problem = GivenPendingProblem();
        GivenUnfinishedJob(problem.Id);

        Result result = await _handler.Handle(
            new CancelProblemValidationCommand(problem.Id),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(ProblemStatus.Draft, problem.Status);
        _problemRepository.Verify(x => x.UpdateAsync(problem, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PendingProblem_CancelsItsUnfinishedJob()
    {
        Problem problem = GivenPendingProblem();
        ProblemValidationJob job = GivenUnfinishedJob(problem.Id);

        await _handler.Handle(new CancelProblemValidationCommand(problem.Id), CancellationToken.None);

        Assert.Equal(ProblemValidationJobStatus.Cancelled, job.Status);
        Assert.NotNull(job.CompletedAt);
        _jobRepository.Verify(x => x.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ClearsAnyPreviousFailureReason()
    {
        Problem problem = CreateProblem();
        problem.FailValidation("an older failure");
        problem.SubmitForValidation();
        _problemRepository.Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(problem);
        GivenUnfinishedJob(problem.Id);

        await _handler.Handle(new CancelProblemValidationCommand(problem.Id), CancellationToken.None);

        Assert.Null(problem.ValidationFailureReason);
    }

    [Fact]
    public async Task Handle_NoUnfinishedJob_StillReturnsTheProblemToDraft()
    {
        Problem problem = GivenPendingProblem();
        _jobRepository
            .Setup(x => x.FindUnfinishedByProblemIdAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProblemValidationJob?)null);

        Result result = await _handler.Handle(
            new CancelProblemValidationCommand(problem.Id),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(ProblemStatus.Draft, problem.Status);
        _jobRepository.Verify(
            x => x.UpdateAsync(It.IsAny<ProblemValidationJob>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ProblemNotFound_ReturnsNotFound()
    {
        _problemRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        Result result = await _handler.Handle(
            new CancelProblemValidationCommand(Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Theory]
    [InlineData(ProblemStatus.Draft)]
    [InlineData(ProblemStatus.Published)]
    [InlineData(ProblemStatus.Archived)]
    public async Task Handle_ProblemNotAwaitingValidation_IsRejected(ProblemStatus status)
    {
        Problem problem = CreateProblem();

        if (status == ProblemStatus.Published)
            problem.Publish();
        else if (status == ProblemStatus.Archived)
            problem.Archive();

        _problemRepository.Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(problem);

        Result result = await _handler.Handle(
            new CancelProblemValidationCommand(problem.Id),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal(status, problem.Status);
        _problemRepository.Verify(x => x.UpdateAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyProblemId_IsRejected()
    {
        Result result = await _handler.Handle(
            new CancelProblemValidationCommand(Guid.Empty),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }
}