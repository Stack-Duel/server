using Ardalis.Result;
using MediatR;
using Moq;
using StackDuel.Application.Games;
using StackDuel.Domain.Games.Entities;
using JoinGameByCodeCommand = StackDuel.Application.Commands.Games.JoinGameByCode.JoinGameByCodeCommand;
using JoinGameByCodeHandler = StackDuel.Application.Commands.Games.JoinGameByCode.JoinGameByCodeHandler;
using JoinGameByCodeValidator = StackDuel.Application.Commands.Games.JoinGameByCode.JoinGameByCodeValidator;
using JoinGameCommand = StackDuel.Application.Commands.Games.JoinGame.JoinGameCommand;

namespace StackDuel.Application.Tests.Commands.Games.JoinGameByCode;

public class JoinGameByCodeHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IMediator> _mediator = null!;
    private JoinGameByCodeHandler _handler = null!;

    public JoinGameByCodeHandlerTests()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _mediator = new Mock<IMediator>();

        _handler = new JoinGameByCodeHandler(
            _gameReadRepository.Object,
            _mediator.Object,
            new JoinGameByCodeValidator()
        );
    }

    [Fact]
    public async Task Handle_InvalidCommand_ReturnsInvalidAndDoesNotTouchRepository()
    {
        var command = new JoinGameByCodeCommand(string.Empty, Guid.NewGuid());

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _gameReadRepository.Verify(
            x => x.FindGameByJoinCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_CodeNotFound_ReturnsNotFound()
    {
        _gameReadRepository
            .Setup(x => x.FindGameByJoinCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        var command = new JoinGameByCodeCommand("ABCDEFG", Guid.NewGuid());
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
        _mediator.Verify(x => x.Send(It.IsAny<JoinGameCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidCodeLength_ReturnsInvalidAndDoesNotTouchRepository()
    {
        var command = new JoinGameByCodeCommand("ABC", Guid.NewGuid());

        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _gameReadRepository.Verify(
            x => x.FindGameByJoinCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_GameNotPendingAndNotAParticipant_ReturnsInvalid()
    {
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);
        game.Start();

        _gameReadRepository
            .Setup(x => x.FindGameByJoinCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var command = new JoinGameByCodeCommand(game.JoinCode, Guid.NewGuid());
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _mediator.Verify(x => x.Send(It.IsAny<JoinGameCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyAParticipantInAPendingLobby_RedirectsWithoutDelegatingToJoin()
    {
        var requesterId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [requesterId], 600);

        _gameReadRepository
            .Setup(x => x.FindGameByJoinCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var command = new JoinGameByCodeCommand(game.JoinCode, requesterId);
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(game.Id, result.Value);
        _mediator.Verify(x => x.Send(It.IsAny<JoinGameCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyAParticipantInARunningGame_StillRedirects()
    {
        var requesterId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [requesterId, Guid.NewGuid()], 600);
        game.Start();

        _gameReadRepository
            .Setup(x => x.FindGameByJoinCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var command = new JoinGameByCodeCommand(game.JoinCode, requesterId);
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(game.Id, result.Value);
    }

    [Fact]
    public async Task Handle_UnderlyingJoinFails_PropagatesFailure()
    {
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);

        _gameReadRepository
            .Setup(x => x.FindGameByJoinCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _mediator
            .Setup(x => x.Send(It.IsAny<JoinGameCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Forbidden());

        var command = new JoinGameByCodeCommand(game.JoinCode, Guid.NewGuid());
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_Success_ReturnsGameId()
    {
        var requesterId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);

        _gameReadRepository
            .Setup(x => x.FindGameByJoinCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);
        _mediator
            .Setup(x =>
                x.Send(
                    It.Is<JoinGameCommand>(c => c.GameId == game.Id && c.RequestedByUserId == requesterId),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(Result.Success());

        var command = new JoinGameByCodeCommand(game.JoinCode, requesterId);
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(game.Id, result.Value);
    }
}