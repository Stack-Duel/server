using StackDuel.Application.Games;
using StackDuel.Application.Games.Dtos;
using StackDuel.Domain.Games.Entities;
using Ardalis.Result;
using Moq;
using GetGameModesHandler = StackDuel.Application.Queries.Games.GetGameModes.GetGameModesHandler;
using GetGameModesQuery = StackDuel.Application.Queries.Games.GetGameModes.GetGameModesQuery;

namespace StackDuel.Application.Tests.Queries.Games.GetGameModes;

public class GetGameModesHandlerTests
{
    private Mock<IGameReadRepository> _gameReadRepository = null!;
    private GetGameModesHandler _handler = null!;

    [SetUp]
    public void SetUp()
    {
        _gameReadRepository = new Mock<IGameReadRepository>();
        _handler = new GetGameModesHandler(_gameReadRepository.Object);
    }

    [Test]
    public async Task Handle_NoActiveModes_ReturnsEmptyList()
    {
        _gameReadRepository.Setup(x => x.GetActiveGameModesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        Result<IReadOnlyList<GameModeDto>> result = await _handler.Handle(
            new GetGameModesQuery(),
            CancellationToken.None
        );

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value, Is.Empty);
    }

    [Test]
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

        Assert.That(result.IsSuccess, Is.True);
        GameModeDto dto = result.Value.Single();
        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(mode.Id));
            Assert.That(dto.Key, Is.EqualTo("blitz"));
            Assert.That(dto.Name, Is.EqualTo("Blitz"));
            Assert.That(dto.Description, Is.EqualTo("Fast paced games."));
            Assert.That(dto.MinPlayers, Is.EqualTo(1));
            Assert.That(dto.MaxPlayers, Is.EqualTo(4));
            Assert.That(dto.TimeOptions.Select(o => o.DurationSeconds), Is.EqualTo(new[] { 60, 180, 300 }));
            Assert.That(dto.TimeOptions.Single(o => o.DurationSeconds == 60).IsDefault, Is.True);
        });
    }
}