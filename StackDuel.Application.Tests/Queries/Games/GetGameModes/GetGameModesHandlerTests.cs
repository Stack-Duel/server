using Ardalis.Result;
using Moq;
using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Domain.Games.Entities;
using GetGameModesHandler = StackDuel.Application.Queries.Games.GetGameModes.GetGameModesHandler;
using GetGameModesQuery = StackDuel.Application.Queries.Games.GetGameModes.GetGameModesQuery;

namespace StackDuel.Application.Tests.Queries.Games.GetGameModes;

public class GetGameModesHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private GetGameModesHandler _handler = null!;

    public GetGameModesHandlerTests()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _handler = new GetGameModesHandler(_gameReadRepository.Object);
    }

    [Fact]
    public async Task Handle_NoActiveModes_ReturnsEmptyList()
    {
        _gameReadRepository.Setup(x => x.GetActiveGameModesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Result<IReadOnlyList<GameModeDto>> result = await _handler.Handle(
            new GetGameModesQuery(),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value);
    }

    [Fact]
    public async Task Handle_ActiveModes_MapsFieldsAndOrdersTimeOptionsByDuration()
    {
        var mode = new GameMode(
            "blitz",
            "Blitz",
            "Fast paced games.",
            isBuiltIn: true,
            minPlayers: 1,
            maxPlayers: 4,
            defaultPoolId: Guid.NewGuid()
        );
        mode.AddTimeOption(300, isDefault: false);
        mode.AddTimeOption(60, isDefault: true);
        mode.AddTimeOption(180, isDefault: false);

        _gameReadRepository.Setup(x => x.GetActiveGameModesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([mode]);

        Result<IReadOnlyList<GameModeDto>> result = await _handler.Handle(
            new GetGameModesQuery(),
            CancellationToken.None
        );

        Assert.True(result.IsSuccess);
        GameModeDto dto = result.Value.Single();
        Assert.Equal(mode.Id, dto.Id);
        Assert.Equal("blitz", dto.Key);
        Assert.Equal("Blitz", dto.Name);
        Assert.Equal("Fast paced games.", dto.Description);
        Assert.Equal(1, dto.MinPlayers);
        Assert.Equal(4, dto.MaxPlayers);
        Assert.Equal(new[] { 60, 180, 300 }, dto.TimeOptions.Select(o => o.DurationSeconds));
        Assert.True(dto.TimeOptions.Single(o => o.DurationSeconds == 60).IsDefault);
    }
}