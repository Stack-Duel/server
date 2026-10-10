using Ardalis.Result;
using Moq;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;
using UpdateProblemCommand = StackDuel.Application.Commands.Problems.UpdateProblem.UpdateProblemCommand;
using UpdateProblemHandler = StackDuel.Application.Commands.Problems.UpdateProblem.UpdateProblemHandler;
using UpdateProblemValidator = StackDuel.Application.Commands.Problems.UpdateProblem.UpdateProblemValidator;

namespace StackDuel.Application.Tests.Commands.Problems.UpdateProblem;

public class UpdateProblemHandlerTests
{
    private static readonly string ValidQuestion = new('q', 60);

    private Mock<IProblemRepository> _problemRepository = null!;
    private UpdateProblemHandler _handler = null!;

    public UpdateProblemHandlerTests()
    {
        _problemRepository = new Mock<IProblemRepository>();
        _handler = new UpdateProblemHandler(new UpdateProblemValidator(), _problemRepository.Object);
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

    private static UpdateProblemCommand ValidCommand(
        Guid problemId,
        ProblemStatus? status = null,
        IReadOnlyCollection<string>? tags = null
    ) => new(problemId, "Updated Title", ValidQuestion, 200, 2000, 256, tags ?? [], status);

    [Fact]
    public async Task Handle_ProblemNotFound_ReturnsNotFound()
    {
        _problemRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        Result result = await _handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ValidUpdate_UpdatesContentAndPersists()
    {
        var problem = CreateProblem();
        _problemRepository.Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(problem);

        Result result = await _handler.Handle(ValidCommand(problem.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Updated Title", problem.Title.Value);
        Assert.Equal(200, problem.Difficulty.Value);
        Assert.Single(problem.History);
        _problemRepository.Verify(x => x.UpdateAsync(problem, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_TagWithInvalidFormat_ReturnsInvalidResultAndDoesNotPersist()
    {
        var problem = CreateProblem();
        _problemRepository.Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(problem);

        Result result = await _handler.Handle(ValidCommand(problem.Id, tags: ["invalid tag"]), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _problemRepository.Verify(x => x.UpdateAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_StatusTransition_DraftToPublished_Succeeds()
    {
        var problem = CreateProblem();
        _problemRepository.Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(problem);

        await _handler.Handle(ValidCommand(problem.Id, status: ProblemStatus.Published), CancellationToken.None);

        Assert.Equal(ProblemStatus.Published, problem.Status);
    }

    [Fact]
    public async Task Handle_InvalidStatusTransition_ReturnsInvalidAndLeavesProblemUnchanged()
    {
        var problem = CreateProblem();
        problem.Archive();
        _problemRepository.Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(problem);

        Result result = await _handler.Handle(
            ValidCommand(problem.Id, status: ProblemStatus.Published),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal(ProblemStatus.Archived, problem.Status);
        Assert.Equal("Two Sum", problem.Title.Value);
        _problemRepository.Verify(x => x.UpdateAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AddsNewTags_ViaFindOrCreateTagsAsync()
    {
        var problem = CreateProblem();
        _problemRepository.Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(problem);

        var createdTag = new ProblemTag(new Tag("arrays"));
        _problemRepository
            .Setup(x =>
                x.FindOrCreateTagsAsync(
                    It.Is<IReadOnlyCollection<string>>(names => names.Count == 1 && names.Contains("arrays")),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync([createdTag]);

        await _handler.Handle(ValidCommand(problem.Id, tags: ["arrays"]), CancellationToken.None);

        Assert.Contains(createdTag, problem.Tags);
    }

    [Fact]
    public async Task Handle_RemovesTagsNoLongerDesired()
    {
        var problem = CreateProblem();
        var existingTag = new ProblemTag(new Tag("old-tag"));
        problem.AddTag(existingTag);
        _problemRepository.Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(problem);

        await _handler.Handle(ValidCommand(problem.Id, tags: []), CancellationToken.None);

        Assert.DoesNotContain(existingTag, problem.Tags);
    }

    [Fact]
    public async Task Handle_DuplicateTagNames_DeduplicatedBeforeLookup()
    {
        var problem = CreateProblem();
        _problemRepository.Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(problem);

        _problemRepository
            .Setup(x => x.FindOrCreateTagsAsync(It.IsAny<IReadOnlyCollection<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        await _handler.Handle(ValidCommand(problem.Id, tags: ["arrays", "arrays", "Arrays"]), CancellationToken.None);

        _problemRepository.Verify(
            x =>
                x.FindOrCreateTagsAsync(
                    It.Is<IReadOnlyCollection<string>>(names => names.Count == 1 && names.Contains("arrays")),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_InvalidCommand_ReturnsInvalidAndDoesNotTouchRepository()
    {
        var command = ValidCommand(Guid.NewGuid()) with { Title = "" };

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _problemRepository.Verify(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}