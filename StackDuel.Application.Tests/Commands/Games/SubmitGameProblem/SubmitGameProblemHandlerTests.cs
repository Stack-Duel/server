using Ardalis.Result;
using MediatR;
using Moq;
using StackDuel.Application.Commands.Submissions.CreateSubmission;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using SubmitGameProblemCommand = StackDuel.Application.Commands.Games.SubmitGameProblem.SubmitGameProblemCommand;
using SubmitGameProblemHandler = StackDuel.Application.Commands.Games.SubmitGameProblem.SubmitGameProblemHandler;
using SubmitGameProblemValidator = StackDuel.Application.Commands.Games.SubmitGameProblem.SubmitGameProblemValidator;

namespace StackDuel.Application.Tests.Commands.Games.SubmitGameProblem;

public class SubmitGameProblemHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IGameWriteRepository> _gameWriteRepository = null!;
    private Mock<IDomainEventDispatcher> _domainEventDispatcher = null!;
    private Mock<IMediator> _mediator = null!;
    private SubmitGameProblemHandler _handler = null!;

    public SubmitGameProblemHandlerTests()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _gameWriteRepository = new Mock<IGameWriteRepository>();
        _domainEventDispatcher = new Mock<IDomainEventDispatcher>();
        _mediator = new Mock<IMediator>();

        _handler = new SubmitGameProblemHandler(
            _gameReadRepository.Object,
            _gameWriteRepository.Object,
            _domainEventDispatcher.Object,
            _mediator.Object,
            new SubmitGameProblemValidator()
        );
    }

    private static SubmitGameProblemCommand CreateCommand(Guid gameId, Guid problemId, Guid requestedByUserId) =>
        new(gameId, problemId, Guid.NewGuid(), "print('hi')", requestedByUserId);

    [Fact]
    public async Task Handle_InvalidCommand_ReturnsInvalidAndDoesNotTouchRepository()
    {
        var command = new SubmitGameProblemCommand(Guid.Empty, Guid.NewGuid(), Guid.NewGuid(), "code", Guid.NewGuid());

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _gameReadRepository.Verify(
            x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_GameNotFound_ReturnsNotFound()
    {
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        var command = CreateCommand(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_NotParticipant_ReturnsForbidden()
    {
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var command = CreateCommand(game.Id, Guid.NewGuid(), Guid.NewGuid());
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_GameNotRunning_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var command = CreateCommand(game.Id, Guid.NewGuid(), userId);
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _mediator.Verify(x => x.Send(It.IsAny<CreateSubmissionCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ParticipantForfeited_ReturnsInvalid()
    {
        var forfeitedUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [forfeitedUserId, otherUserId], 600);
        game.Start();
        game.Forfeit(forfeitedUserId);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var command = CreateCommand(game.Id, Guid.NewGuid(), forfeitedUserId);
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        Assert.Equal(GameStatus.Running, game.Status);
    }

    [Fact]
    public async Task Handle_ParticipantFinishedProblems_ReturnsInvalid()
    {
        var finishedUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [finishedUserId, otherUserId], 600);
        game.Start();
        game.FinishProblemsFor(finishedUserId);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var command = CreateCommand(game.Id, Guid.NewGuid(), finishedUserId);
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_ProblemSessionNotInitialized_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var command = CreateCommand(game.Id, Guid.NewGuid(), userId);
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_ProblemDoesNotMatchCurrentSession_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.Single();
        participant.InitializeProblemSession(Guid.NewGuid());

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var command = CreateCommand(game.Id, Guid.NewGuid(), userId);
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_SubmissionCreationFails_ReturnsError()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.Single();
        participant.InitializeProblemSession(problemId);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _mediator
            .Setup(x => x.Send(It.IsAny<CreateSubmissionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Error("boom"));

        var command = CreateCommand(game.Id, problemId, userId);
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Error, result.Status);
        Assert.Null(participant.ProblemSession!.ActiveSubmissionId);
        _gameWriteRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<Game>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_Success_SetsActiveSubmissionAndPersists()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        var submissionId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.Single();
        participant.InitializeProblemSession(problemId);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _mediator
            .Setup(x => x.Send(It.IsAny<CreateSubmissionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(submissionId));

        var command = CreateCommand(game.Id, problemId, userId);
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(submissionId, result.Value);
        Assert.Equal(submissionId, participant.ProblemSession!.ActiveSubmissionId);
        _gameWriteRepository.Verify(x => x.SaveChangesAsync(game, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Success_PassesGameIdToCreateSubmissionCommand()
    {
        var userId = Guid.NewGuid();
        var problemId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        var participant = game.Participants.Single();
        participant.InitializeProblemSession(problemId);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _mediator
            .Setup(x => x.Send(It.IsAny<CreateSubmissionCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Guid>.Success(Guid.NewGuid()));

        var command = CreateCommand(game.Id, problemId, userId);
        await _handler.Handle(command, CancellationToken.None);

        _mediator.Verify(
            x => x.Send(It.Is<CreateSubmissionCommand>(c => c.GameId == game.Id), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }
}