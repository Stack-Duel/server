using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackDuel.Application.Events;
using StackDuel.Application.Events.Games;
using StackDuel.Application.Games;
using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Games.Events;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Application.Tests.Events.Games;

public class RecordLeaderboardScoresOnGameCompletedHandlerTests
{
    private readonly Mock<IGameReadRepository> _gameReadRepository = new();
    private readonly Mock<ILeaderboardWriteRepository> _leaderboardWriteRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly RecordLeaderboardScoresOnGameCompletedHandler _handler;

    private readonly Guid _gameModeId = Guid.NewGuid();
    private readonly Guid _winnerId = Guid.NewGuid();
    private readonly Guid _loserId = Guid.NewGuid();

    public RecordLeaderboardScoresOnGameCompletedHandlerTests()
    {
        _handler = new RecordLeaderboardScoresOnGameCompletedHandler(
            _gameReadRepository.Object,
            _leaderboardWriteRepository.Object,
            _unitOfWork.Object,
            NullLogger<RecordLeaderboardScoresOnGameCompletedHandler>.Instance
        );
    }

    private Game CreateCompletedGame()
    {
        Game game = new(_gameModeId, Guid.NewGuid(), [Guid.NewGuid()], [_winnerId, _loserId], timeLimitInSeconds: 600);
        game.Start();
        game.RecordProblemSolved(_winnerId);
        game.Complete();
        return game;
    }

    private DomainEventNotification<GameCompletedDomainEvent> Completed(Guid gameId) =>
        new(new GameCompletedDomainEvent(gameId, GameStatus.Completed, DateTime.UtcNow));

    private void StubGameFound(Game game) =>
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

    private void StubNoExistingLeaderboard() =>
        _leaderboardWriteRepository
            .Setup(x =>
                x.FindByGameModeAndTimeLimitAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((Leaderboard?)null);

    [Fact]
    public async Task Handle_NoLeaderboardYet_CreatesOneWithEveryParticipantsScore()
    {
        Game game = CreateCompletedGame();
        StubGameFound(game);
        StubNoExistingLeaderboard();

        Leaderboard? added = null;
        _leaderboardWriteRepository
            .Setup(x => x.AddAsync(It.IsAny<Leaderboard>(), It.IsAny<CancellationToken>()))
            .Callback<Leaderboard, CancellationToken>((l, _) => added = l)
            .Returns(Task.CompletedTask);

        await _handler.Handle(Completed(game.Id), CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal(_gameModeId, added.GameModeId);
        Assert.Equal(600, added.TimeLimitInSeconds);
        Assert.Equal(2, added.Participants.Count);
    }

    [Fact]
    public async Task Handle_LeaderboardExists_RecordsScoresAndSavesInsteadOfAdding()
    {
        Game game = CreateCompletedGame();
        StubGameFound(game);

        Leaderboard existing = new(_gameModeId, 600);
        _leaderboardWriteRepository
            .Setup(x => x.FindByGameModeAndTimeLimitAsync(_gameModeId, 600, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);

        await _handler.Handle(Completed(game.Id), CancellationToken.None);

        Assert.Equal(2, existing.Participants.Count);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _leaderboardWriteRepository.Verify(
            x => x.AddAsync(It.IsAny<Leaderboard>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_GameNoLongerExists_DoesNothing()
    {
        Guid gameId = Guid.NewGuid();
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(gameId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Game?)null);

        await _handler.Handle(Completed(gameId), CancellationToken.None);

        _leaderboardWriteRepository.Verify(
            x => x.FindByGameModeAndTimeLimitAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_LeaderboardWriteFails_DoesNotFaultTheCommandThatCompletedTheGame()
    {
        Game game = CreateCompletedGame();
        StubGameFound(game);
        StubNoExistingLeaderboard();
        _leaderboardWriteRepository
            .Setup(x => x.AddAsync(It.IsAny<Leaderboard>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("database is down"));

        await _handler.Handle(Completed(game.Id), CancellationToken.None);
    }
}