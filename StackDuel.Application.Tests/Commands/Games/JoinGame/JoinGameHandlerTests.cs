using Ardalis.Result;
using Moq;
using StackDuel.Application.Games;
using JoinGameCommand = StackDuel.Application.Commands.Games.JoinGame.JoinGameCommand;
using JoinGameHandler = StackDuel.Application.Commands.Games.JoinGame.JoinGameHandler;
using JoinGameValidator = StackDuel.Application.Commands.Games.JoinGame.JoinGameValidator;

namespace StackDuel.Application.Tests.Commands.Games.JoinGame;

public class JoinGameHandlerTests
{
    private readonly Mock<IGameJoiner> _gameJoiner = new();
    private readonly JoinGameHandler _handler;

    public JoinGameHandlerTests()
    {
        _handler = new JoinGameHandler(_gameJoiner.Object, new JoinGameValidator());
    }

    [Fact]
    public async Task Handle_InvalidCommand_ReturnsInvalidAndNeverJoins()
    {
        var command = new JoinGameCommand(Guid.Empty, Guid.NewGuid());

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
        _gameJoiner.Verify(
            x => x.JoinAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ValidCommand_JoinsTheRequestedGameAndReturnsThatResult()
    {
        var command = new JoinGameCommand(Guid.NewGuid(), Guid.NewGuid());
        _gameJoiner
            .Setup(x => x.JoinAsync(command.GameId, command.RequestedByUserId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        _gameJoiner.Verify(
            x => x.JoinAsync(command.GameId, command.RequestedByUserId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_JoinRejected_ReturnsThatResult()
    {
        var command = new JoinGameCommand(Guid.NewGuid(), Guid.NewGuid());
        _gameJoiner
            .Setup(x => x.JoinAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Forbidden());

        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }
}