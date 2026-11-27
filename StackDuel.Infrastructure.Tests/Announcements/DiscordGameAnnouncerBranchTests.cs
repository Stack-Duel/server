using Discord;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using StackDuel.Application.Games;
using StackDuel.Application.Users;
using StackDuel.Application.Users.Dtos;
using StackDuel.Domain.Games.Entities;
using StackDuel.Infrastructure.Announcements;
using GameEntity = StackDuel.Domain.Games.Entities.Game;

namespace StackDuel.Infrastructure.Tests.Announcements;

public class DiscordGameAnnouncerBranchTests
{
    private sealed class RecordingWebhookSender : IDiscordWebhookSender
    {
        public List<Embed> Sent { get; } = [];

        public Task SendAsync(Embed embed, CancellationToken cancellationToken = default)
        {
            Sent.Add(embed);
            return Task.CompletedTask;
        }
    }

    private readonly Mock<IGameReadRepository> _gameReadRepository = new();
    private readonly Mock<IUserReadRepository> _userReadRepository = new();
    private readonly RecordingWebhookSender _sender = new();
    private readonly DiscordGameAnnouncer _announcer;

    public DiscordGameAnnouncerBranchTests()
    {
        _announcer = new DiscordGameAnnouncer(
            _sender,
            _gameReadRepository.Object,
            _userReadRepository.Object,
            NullLogger<DiscordGameAnnouncer>.Instance
        );

        _userReadRepository
            .Setup(x => x.FindByIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, UserDto>());
    }

    private static GameEntity CreateGame(Guid gameModeId)
    {
        GameEntity game = new(gameModeId, Guid.NewGuid(), [Guid.NewGuid()], [Guid.NewGuid()], timeLimitInSeconds: 600);
        return game;
    }

    private void StubGameFound(GameEntity game) =>
        _gameReadRepository.Setup(x => x.FindGameByIdAsync(game.Id, It.IsAny<CancellationToken>())).ReturnsAsync(game);

    private void StubGameModeFound(GameMode? gameMode) =>
        _gameReadRepository
            .Setup(x => x.FindGameModeByIdIncludingInactiveAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(gameMode);

    [Fact]
    public async Task AnnounceGameCreatedAsync_GameModeNoLongerExists_SendsNothing()
    {
        GameEntity game = CreateGame(Guid.NewGuid());
        StubGameFound(game);
        StubGameModeFound(null);

        await _announcer.AnnounceGameCreatedAsync(game.Id);

        Assert.Empty(_sender.Sent);
    }

    [Fact]
    public async Task AnnounceGameCreatedAsync_HostIsNotAKnownUser_FallsBackToAPlaceholderName()
    {
        Guid gameModeId = Guid.NewGuid();
        GameEntity game = CreateGame(gameModeId);
        StubGameFound(game);
        StubGameModeFound(new GameMode("duel", "Duel", "Head to head", true, 2, 2, Guid.NewGuid()));

        await _announcer.AnnounceGameCreatedAsync(game.Id);

        Embed embed = Assert.Single(_sender.Sent);
        Assert.Equal("someone", embed.Fields.Single(f => f.Name == "Host").Value);
    }
}