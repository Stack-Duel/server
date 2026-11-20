using Ardalis.Result;
using Moq;
using StackDuel.Application.ProblemValidation;
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
    private readonly Mock<IProblemRepository> _problemRepository = new();
    private readonly Mock<IProblemValidationJobRepository> _jobRepository = new();
    private readonly Mock<IProblemValidationConcurrencyGuard> _concurrencyGuard = new();
    private readonly CancelProblemValidationHandler _handler;

    public CancelProblemValidationHandlerTests()
    {
        _concurrencyGuard
            .Setup(guard =>
                guard.RunExclusiveAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Func<CancellationToken, Task<Result>>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns((Guid _, Func<CancellationToken, Task<Result>> action, CancellationToken ct) => action(ct));

        _handler = new CancelProblemValidationHandler(
            new CancelProblemValidationValidator(),
            _problemRepository.Object,
            _jobRepository.Object,
            _concurrencyGuard.Object
        );
    }

    private static Problem CreateProblem() =>
        new(
            new Slug("two-sum"),
            new Title("Two Sum"),
            new Question(new string('a', Question.MinLength)),
            new Difficulty(500),
            new TimeLimit(1000),
            new MemoryLimit(64)
        );

    private static Problem CreateValidatingProblem()
    {
        Problem problem = CreateProblem();
        problem.SubmitForValidation();
        return problem;
    }

    [Fact]
    public async Task Handle_MissingProblem_ReturnsNotFound()
    {
        _problemRepository
            .Setup(repository => repository.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        Result result = await _handler.Handle(new CancelProblemValidationCommand(Guid.NewGuid()), default);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ProblemIsNotValidating_ReturnsInvalid()
    {
        _problemRepository
            .Setup(repository => repository.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateProblem());

        Result result = await _handler.Handle(new CancelProblemValidationCommand(Guid.NewGuid()), default);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_ValidatingProblem_CancelsTheJobAndReturnsTheProblemToDraft()
    {
        Problem problem = CreateValidatingProblem();
        ProblemValidationJob job = new(problem.Id);
        job.BeginStep(ProblemValidationStepKind.ReferenceSolutions);

        _problemRepository
            .Setup(repository => repository.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);
        _jobRepository
            .Setup(repository => repository.FindActiveForProblemAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);

        Result result = await _handler.Handle(new CancelProblemValidationCommand(problem.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProblemValidationJobStatus.Cancelled, job.Status);
        Assert.Equal(ProblemStatus.Draft, problem.Status);
        _jobRepository.Verify(repository => repository.UpdateAsync(job, It.IsAny<CancellationToken>()), Times.Once);
        _problemRepository.Verify(
            repository => repository.UpdateAsync(problem, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_NoActiveJob_StillReturnsTheProblemToDraft()
    {
        Problem problem = CreateValidatingProblem();

        _problemRepository
            .Setup(repository => repository.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(problem);
        _jobRepository
            .Setup(repository => repository.FindActiveForProblemAsync(problem.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProblemValidationJob?)null);

        Result result = await _handler.Handle(new CancelProblemValidationCommand(problem.Id), default);

        Assert.True(result.IsSuccess);
        Assert.Equal(ProblemStatus.Draft, problem.Status);
    }
}
