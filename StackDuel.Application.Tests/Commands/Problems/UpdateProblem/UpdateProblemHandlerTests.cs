using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.Enums;
using StackDuel.Domain.Problems.ValueObjects;
using Ardalis.Result;
using Moq;
using UpdateProblemCommand = StackDuel.Application.Commands.Problems.UpdateProblem.UpdateProblemCommand;
using UpdateProblemHandler = StackDuel.Application.Commands.Problems.UpdateProblem.UpdateProblemHandler;
using UpdateProblemValidator = StackDuel.Application.Commands.Problems.UpdateProblem.UpdateProblemValidator;

namespace StackDuel.Application.Tests.Commands.Problems.UpdateProblem;

public class UpdateProblemHandlerTests
{
    private static readonly string ValidQuestion = new('q', 60);

    private Mock<IProblemRepository> _problemRepository = null!;
    private UpdateProblemHandler _handler = null!;

    [SetUp]
    public void SetUp()
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

    [Test]
    public async Task Handle_ProblemNotFound_ReturnsNotFound()
    {
        _problemRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Problem?)null);

        Result result = await _handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.NotFound));
    }

    [Test]
    public async Task Handle_ValidUpdate_UpdatesContentAndPersists()
    {
        var problem = CreateProblem();
        _problemRepository.Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(problem);

        Result result = await _handler.Handle(ValidCommand(problem.Id), CancellationToken.None);

        Assert.That(result.IsSuccess, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(problem.Title.Value, Is.EqualTo("Updated Title"));
            Assert.That(problem.Difficulty.Value, Is.EqualTo(200));
            Assert.That(problem.History, Has.Count.EqualTo(1));
        });
        _problemRepository.Verify(x => x.UpdateAsync(problem, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Handle_TagWithInvalidFormat_ReturnsInvalidResultAndDoesNotPersist()
    {
        var problem = CreateProblem();
        _problemRepository.Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(problem);

        Result result = await _handler.Handle(ValidCommand(problem.Id, tags: ["invalid tag"]), CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        _problemRepository.Verify(x => x.UpdateAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Handle_StatusTransition_DraftToPublished_Succeeds()
    {
        var problem = CreateProblem();
        _problemRepository.Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(problem);

        await _handler.Handle(ValidCommand(problem.Id, status: ProblemStatus.Published), CancellationToken.None);

        Assert.That(problem.Status, Is.EqualTo(ProblemStatus.Published));
    }

    [Test]
    public async Task Handle_InvalidStatusTransition_ReturnsInvalidAndLeavesProblemUnchanged()
    {
        var problem = CreateProblem();
        problem.Archive();
        _problemRepository.Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(problem);

        Result result = await _handler.Handle(
            ValidCommand(problem.Id, status: ProblemStatus.Published),
            CancellationToken.None
        );

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        Assert.Multiple(() =>
        {
            Assert.That(problem.Status, Is.EqualTo(ProblemStatus.Archived));
            Assert.That(problem.Title.Value, Is.EqualTo("Two Sum"));
        });
        _problemRepository.Verify(x => x.UpdateAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
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

        Assert.That(problem.Tags, Contains.Item(createdTag));
    }

    [Test]
    public async Task Handle_RemovesTagsNoLongerDesired()
    {
        var problem = CreateProblem();
        var existingTag = new ProblemTag(new Tag("old-tag"));
        problem.AddTag(existingTag);
        _problemRepository.Setup(x => x.FindByIdAsync(problem.Id, It.IsAny<CancellationToken>())).ReturnsAsync(problem);

        await _handler.Handle(ValidCommand(problem.Id, tags: []), CancellationToken.None);

        Assert.That(problem.Tags, Does.Not.Contain(existingTag));
    }

    [Test]
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

    [Test]
    public async Task Handle_InvalidCommand_ReturnsInvalidAndDoesNotTouchRepository()
    {
        var command = ValidCommand(Guid.NewGuid()) with { Title = "" };

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.That(result.Status, Is.EqualTo(ResultStatus.Invalid));
        _problemRepository.Verify(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}