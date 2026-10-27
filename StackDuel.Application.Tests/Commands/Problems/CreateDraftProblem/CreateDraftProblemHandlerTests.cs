using Ardalis.Result;
using Moq;
using StackDuel.Application.Problems.Dtos;
using StackDuel.Application.Services.Problems;
using StackDuel.Application.Tracks;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Problems;
using StackDuel.Domain.Problems.Entities;
using StackDuel.Domain.Problems.ValueObjects;
using StackDuel.Domain.SeedWork;
using StackDuel.Domain.Tracks.Entities;
using CreateDraftProblemCommand = StackDuel.Application.Commands.Problems.CreateDraftProblem.CreateDraftProblemCommand;
using CreateDraftProblemHandler = StackDuel.Application.Commands.Problems.CreateDraftProblem.CreateDraftProblemHandler;
using CreateDraftProblemValidator = StackDuel.Application.Commands.Problems.CreateDraftProblem.CreateDraftProblemValidator;

namespace StackDuel.Application.Tests.Commands.Problems.CreateDraftProblem;

public class CreateDraftProblemHandlerTests
{
    private static readonly string ValidQuestion = new('q', 60);

    private readonly Mock<ITrackReadRepository> _trackReadRepository = new();
    private readonly Mock<IProblemRepository> _problemRepository = new();
    private readonly Mock<ISlugService> _slugService = new();
    private readonly Mock<IProblemLimitService> _problemLimitService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAdvisoryLockService> _advisoryLockService = new();
    private readonly Mock<ITransactionScope> _transactionScope = new();
    private readonly UserContext _userContext = new() { User = CreateUserDto() };
    private readonly CreateDraftProblemHandler _handler;

    public CreateDraftProblemHandlerTests()
    {
        _unitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionScope.Object);

        _slugService.Setup(x => x.GenerateDraftSlug()).Returns(new Slug(Guid.NewGuid().ToString()));
        _problemLimitService
            .Setup(x => x.HasReachedOpenProblemLimitAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _handler = new CreateDraftProblemHandler(
            new CreateDraftProblemValidator(),
            _trackReadRepository.Object,
            _problemRepository.Object,
            _slugService.Object,
            _problemLimitService.Object,
            _unitOfWork.Object,
            _advisoryLockService.Object,
            _userContext
        );
    }

    private static UserDto CreateUserDto() =>
        new(Guid.NewGuid(), "auth0|abc", "alice", null, null, false, null, DateTime.UtcNow, null, []);

    private static Track CreateActiveTrack() => new("general-purpose", "General Purpose");

    private static CreateDraftProblemCommand ValidCommand(
        Guid trackId,
        IEnumerable<string>? tags = null
    ) =>
        new(
            new CreateDraftProblemInput(
                "Two Sum",
                ValidQuestion,
                tags ?? ["arrays"],
                100,
                1000,
                256,
                trackId
            )
        );

    [Fact]
    public async Task Handle_OpenProblemLimitReached_ReturnsInvalidAndDoesNotPersist()
    {
        _problemLimitService
            .Setup(x => x.HasReachedOpenProblemLimitAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Result<Guid> result = await _handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _problemRepository.Verify(x => x.AddAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _transactionScope.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TrackNotFound_ReturnsInvalidAndDoesNotPersist()
    {
        _trackReadRepository
            .Setup(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Track?)null);

        Result<Guid> result = await _handler.Handle(ValidCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _problemRepository.Verify(x => x.AddAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_TrackInactive_ReturnsInvalidAndDoesNotPersist()
    {
        Track track = CreateActiveTrack();
        track.Deactivate();
        _trackReadRepository.Setup(x => x.FindByIdAsync(track.Id, It.IsAny<CancellationToken>())).ReturnsAsync(track);

        Result<Guid> result = await _handler.Handle(ValidCommand(track.Id), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _problemRepository.Verify(x => x.AddAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesProblemAndCommitsTransaction()
    {
        Track track = CreateActiveTrack();
        _trackReadRepository.Setup(x => x.FindByIdAsync(track.Id, It.IsAny<CancellationToken>())).ReturnsAsync(track);

        var createdTag = new ProblemTag(new Tag("arrays"));
        _problemRepository
            .Setup(x => x.FindOrCreateTagsAsync(It.IsAny<IEnumerable<Tag>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([createdTag]);

        Problem? savedProblem = null;
        _problemRepository
            .Setup(x => x.AddAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()))
            .Callback<Problem, CancellationToken>((problem, _) => savedProblem = problem)
            .Returns(Task.CompletedTask);

        Result<Guid> result = await _handler.Handle(ValidCommand(track.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(savedProblem);
        Assert.Equal("Two Sum", savedProblem!.Title.Value);
        Assert.Equal(track.Id, savedProblem.TrackId);
        Assert.Equal(_userContext.User!.Id, savedProblem.CreatedById);
        Assert.Contains(createdTag, savedProblem.Tags);

        _advisoryLockService.Verify(
            x => x.AcquireUserScopedLockAsync(_userContext.User!.Id, It.IsAny<CancellationToken>()),
            Times.Once
        );
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionScope.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidCommand_ReturnsInvalidAndDoesNotTouchRepository()
    {
        Result<Guid> result = await _handler.Handle(ValidCommand(Guid.NewGuid(), tags: []), CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _trackReadRepository.Verify(x => x.FindByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _problemRepository.Verify(x => x.AddAsync(It.IsAny<Problem>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}