using Ardalis.Result;
using Moq;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;
using DeleteProblemCommand = StackDuel.Application.Commands.Problems.DeleteProblem.DeleteProblemCommand;
using DeleteProblemHandler = StackDuel.Application.Commands.Problems.DeleteProblem.DeleteProblemHandler;
using DeleteProblemValidator = StackDuel.Application.Commands.Problems.DeleteProblem.DeleteProblemValidator;

namespace StackDuel.Application.Tests.Commands.Problems.DeleteProblem;

public class DeleteProblemHandlerTests
{
    private const string Title = "Count Repeated Values";

    private readonly Mock<IProblemRepository> _problemRepository = new();
    private readonly DeleteProblemHandler _handler;

    public DeleteProblemHandlerTests()
    {
        _handler = new DeleteProblemHandler(new DeleteProblemValidator(), _problemRepository.Object);
    }

    private static Problem CreateProblem() =>
        new(
            new Slug("count-repeated-values"),
            new Title(Title),
            new Question(new string('q', 60)),
            new Difficulty(450),
            new TimeLimit(1000),
            new MemoryLimit(64)
        );

    private Problem SetUpProblem(Problem? problem = null)
    {
        Problem resolved = problem ?? CreateProblem();

        _problemRepository
            .Setup(repository => repository.FindByIdAsync(resolved.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolved);

        return resolved;
    }

    private Task<Result> DeleteAsync(Problem problem, string? confirmationTitle = null) =>
        _handler.Handle(new DeleteProblemCommand(problem.Id, confirmationTitle ?? Title), default);

    [Fact]
    public async Task MissingProblem_ReturnsNotFound()
    {
        _problemRepository
            .Setup(repository => repository.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        Result result = await _handler.Handle(new DeleteProblemCommand(Guid.NewGuid(), Title), default);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task AMismatchedTitle_IsRejected()
    {
        Problem problem = SetUpProblem();

        Result result = await DeleteAsync(problem, "count repeated values");

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(result.ValidationErrors, error => error.Identifier == "ConfirmationTitle");
        Assert.False(problem.IsDeleted);
    }

    [Fact]
    public async Task AnEmptyConfirmationTitle_FailsValidation()
    {
        Problem problem = SetUpProblem();

        Result result = await DeleteAsync(problem, "  ");

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.False(problem.IsDeleted);
    }

    [Fact]
    public async Task SurroundingWhitespaceInTheTypedTitleIsForgiven()
    {
        Problem problem = SetUpProblem();

        Result result = await DeleteAsync(problem, $"  {Title}  ");

        Assert.True(result.IsSuccess);
        Assert.True(problem.IsDeleted);
    }

    [Fact]
    public async Task AProblemMidValidation_CannotBeDeleted()
    {
        Problem problem = CreateProblem();
        problem.SubmitForValidation();
        SetUpProblem(problem);

        Result result = await DeleteAsync(problem);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Contains(result.ValidationErrors, error => error.ErrorMessage.Contains("validation to finish"));
        Assert.False(problem.IsDeleted);
    }

    [Fact]
    public async Task ADraft_IsSoftDeletedAndPersisted()
    {
        Problem problem = SetUpProblem();

        Result result = await DeleteAsync(problem);

        Assert.True(result.IsSuccess);
        Assert.True(problem.IsDeleted);
        Assert.NotNull(problem.DeletedAt);
        _problemRepository.Verify(
            repository => repository.UpdateAsync(problem, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task APublishedProblem_KeepsItsRowAndStatusSoAnUnfinishedGameSurvives()
    {
        Problem published = CreateProblem();
        published.SubmitForValidation();
        published.CompleteValidation();
        Problem problem = SetUpProblem(published);

        Result result = await DeleteAsync(problem);

        Assert.True(result.IsSuccess);
        Assert.True(problem.IsDeleted);
        Assert.Equal(ProblemStatus.Published, problem.Status);
        _problemRepository.Verify(
            repository => repository.DeleteAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task DeletingTwice_KeepsTheOriginalDeletionTimestamp()
    {
        Problem problem = SetUpProblem();

        await DeleteAsync(problem);
        DateTime? first = problem.DeletedAt;

        await DeleteAsync(problem);

        Assert.Equal(first, problem.DeletedAt);
    }
}