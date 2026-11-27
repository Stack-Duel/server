using Ardalis.Result;
using Moq;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Domain.Games.Entities;
using StackDuel.Domain.Games.Enums;
using GetGameAccessHandler = StackDuel.Application.Queries.Games.GetGameAccess.GetGameAccessHandler;
using GetGameAccessQuery = StackDuel.Application.Queries.Games.GetGameAccess.GetGameAccessQuery;

namespace StackDuel.Application.Tests.Queries.Games.GetGameAccess;

public class GetGameAccessHandlerTests
{
    private readonly Mock<IGameReadRepository> _gameReadRepository = new();
    private readonly GetGameAccessHandler _handler;

    public GetGameAccessHandlerTests()
    {
        _handler = new GetGameAccessHandler(_gameReadRepository.Object);
    }

    private static Game RunningGameWithParticipant(Guid userId)
    {
        Game game = new(Guid.NewGuid(), Guid.NewGuid(), [Guid.NewGuid()], [userId], 600);
        game.Start();
        return game;
    }

    private void SetupGame(Game? game) =>
        _gameReadRepository
            .Setup(x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(game);

    [Fact]
    public async Task Handle_ReturnsNotFoundForAnEmptyGameId()
    {
        Result<GameAccessDto> result = await _handler.Handle(
            new GetGameAccessQuery(Guid.Empty, Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
        _gameReadRepository.Verify(
            x => x.FindGameByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundForAnEmptyUserId()
    {
        Result<GameAccessDto> result = await _handler.Handle(
            new GetGameAccessQuery(Guid.NewGuid(), Guid.Empty),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ReturnsNotFoundWhenTheGameDoesNotExist()
    {
        SetupGame(null);

        Result<GameAccessDto> result = await _handler.Handle(
            new GetGameAccessQuery(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.NotFound, result.Status);
    }

    [Fact]
    public async Task Handle_ReturnsForbiddenWhenTheCallerIsNotAParticipant()
    {
        Game game = RunningGameWithParticipant(Guid.NewGuid());
        SetupGame(game);

        Result<GameAccessDto> result = await _handler.Handle(
            new GetGameAccessQuery(game.Id, Guid.NewGuid()),
            CancellationToken.None
        );

        Assert.Equal(ResultStatus.Forbidden, result.Status);
    }

    [Fact]
    public async Task Handle_ReturnsTheGameStatusForAParticipant()
    {
        Guid userId = Guid.NewGuid();
        Game game = RunningGameWithParticipant(userId);
        SetupGame(game);

        Result<GameAccessDto> result = await _handler.Handle(
            new GetGameAccessQuery(game.Id, userId),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Equal(GameStatus.Running, result.Value.Status);
    }
}