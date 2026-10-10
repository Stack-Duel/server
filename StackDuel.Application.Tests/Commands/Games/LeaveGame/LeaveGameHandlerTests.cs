using Ardalis.Result;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.SeedWork;
using LeaveGameCommand = StackDuel.Application.Commands.Games.LeaveGame.LeaveGameCommand;
using LeaveGameHandler = StackDuel.Application.Commands.Games.LeaveGame.LeaveGameHandler;
using LeaveGameValidator = StackDuel.Application.Commands.Games.LeaveGame.LeaveGameValidator;

namespace StackDuel.Application.Tests.Commands.Games.LeaveGame;

public class LeaveGameHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private Mock<IGameWriteRepository> _gameWriteRepository = null!;
    private Mock<IDomainEventDispatcher> _domainEventDispatcher = null!;
    private LeaveGameHandler _handler = null!;

    public LeaveGameHandlerTests()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _gameWriteRepository = new Mock<IGameWriteRepository>();
        _domainEventDispatcher = new Mock<IDomainEventDispatcher>();

        _handler = new LeaveGameHandler(
            _gameReadRepository.Object,
            _gameWriteRepository.Object,
            _domainEventDispatcher.Object,
            new LeaveGameValidator()
        );
    }

    [Fact]
    public async Task Handle_InvalidCommand_ReturnsInvalidAndDoesNotTouchRepository()
    {
        var command = new LeaveGameCommand(Guid.Empty, Guid.NewGuid());

        Result result = await _handler.Handle(command, CancellationToken.None);

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

        var command = new LeaveGameCommand(Guid.NewGuid(), Guid.NewGuid());
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_NotParticipant_ReturnsForbidden()
    {
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], 600);
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var command = new LeaveGameCommand(game.Id, Guid.NewGuid());
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_GameNotPending_ReturnsInvalid()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId, Guid.NewGuid()], 600);
        game.Start();

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var command = new LeaveGameCommand(game.Id, userId);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(ResultStatus.Invalid, result.Status);
    }

    [Fact]
    public async Task Handle_Success_RemovesParticipantAndKeepsLobbyOpen()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId, otherUserId], 600);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var command = new LeaveGameCommand(game.Id, userId);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(game.Participants, p => p.UserId == userId);
        Assert.Equal(GameStatus.Pending, game.Status);
        _gameWriteRepository.Verify(x => x.SaveChangesAsync(game, It.IsAny<CancellationToken>()), Times.Once);
        _domainEventDispatcher.Verify(
            x => x.DispatchAsync(It.IsAny<IEnumerable<IDomainEvent>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_LastParticipantLeaves_CancelsLobby()
    {
        var userId = Guid.NewGuid();
        var game = new Game(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);

        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

        var command = new LeaveGameCommand(game.Id, userId);
        Result result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(GameStatus.Cancelled, game.Status);
    }
}