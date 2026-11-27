using Microsoft.AspNetCore.SignalR;
using Moq;
using StackDuel.Api.Hubs;
using StackDuel.Application.Notifications;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Api.Tests.Hubs;

public class SignalRGameNotificationServiceTests
{
    private readonly Mock<IHubClients> _clients = new();
    private readonly Mock<IClientProxy> _group = new();
    private readonly SignalRGameNotificationService _service;

    private readonly Guid _gameId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public SignalRGameNotificationServiceTests()
    {
        Mock<IHubContext<GameHub>> hubContext = new();
        _clients.Setup(x => x.Group(It.IsAny<string>())).Returns(_group.Object);
        hubContext.SetupGet(x => x.Clients).Returns(_clients.Object);
        _service = new SignalRGameNotificationService(hubContext.Object);
    }

    private void VerifySentToGameGroup<TPayload>(string method, Func<TPayload, bool> payloadMatches)
    {
        _clients.Verify(x => x.Group($"game:{_gameId}"), Times.Once);
        _group.Verify(
            x =>
                x.SendCoreAsync(
                    method,
                    It.Is<object?[]>(args => args.Length == 1 && payloadMatches((TPayload)args[0]!)),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task NotifyGameCompletedAsync_SendsTheCompletedPayloadToTheGameGroup()
    {
        DateTime endedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await _service.NotifyGameCompletedAsync(_gameId, GameStatus.Completed, endedAt);

        VerifySentToGameGroup<GameCompletedNotification>(
            SignalRGameNotificationService.GameCompletedMethod,
            payload =>
                payload.GameId == _gameId && payload.Status == GameStatus.Completed && payload.EndedAt == endedAt
        );
    }

    [Fact]
    public async Task NotifyGameLobbyUpdatedAsync_SendsTheLobbyPayloadToTheGameGroup()
    {
        await _service.NotifyGameLobbyUpdatedAsync(_gameId);

        VerifySentToGameGroup<GameLobbyUpdatedNotification>(
            SignalRGameNotificationService.GameLobbyUpdatedMethod,
            payload => payload.GameId == _gameId
        );
    }

    [Fact]
    public async Task NotifyGameProgressUpdatedAsync_SendsTheProgressPayloadToTheGameGroup()
    {
        await _service.NotifyGameProgressUpdatedAsync(_gameId);

        VerifySentToGameGroup<GameProgressUpdatedNotification>(
            SignalRGameNotificationService.GameProgressUpdatedMethod,
            payload => payload.GameId == _gameId
        );
    }

    [Fact]
    public async Task NotifyGameParticipantAttemptedAsync_SendsTheAttemptPayloadToTheGameGroup()
    {
        DateTime attemptedAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await _service.NotifyGameParticipantAttemptedAsync(
            _gameId,
            _userId,
            SubmissionStatus.WrongAnswer,
            attemptedAt
        );

        VerifySentToGameGroup<GameParticipantAttemptedNotification>(
            SignalRGameNotificationService.GameParticipantAttemptedMethod,
            payload =>
                payload.GameId == _gameId
                && payload.UserId == _userId
                && payload.Status == SubmissionStatus.WrongAnswer
                && payload.AttemptedAt == attemptedAt
        );
    }

    [Fact]
    public async Task NotifyGameParticipantReactedAsync_SendsTheReactionPayloadToTheGameGroup()
    {
        DateTime sentAt = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        await _service.NotifyGameParticipantReactedAsync(_gameId, _userId, "🔥", sentAt);

        VerifySentToGameGroup<GameParticipantReactedNotification>(
            SignalRGameNotificationService.GameParticipantReactedMethod,
            payload =>
                payload.GameId == _gameId
                && payload.UserId == _userId
                && payload.Emoji == "🔥"
                && payload.SentAt == sentAt
        );
    }

    [Fact]
    public async Task NotifyGameCompletedAsync_DoesNotPassACallersCancellationTokenToTheBroadcast()
    {
        await _service.NotifyGameCompletedAsync(_gameId, GameStatus.Completed, DateTime.UtcNow);

        _group.Verify(
            x => x.SendCoreAsync(It.IsAny<string>(), It.IsAny<object?[]>(), CancellationToken.None),
            Times.Once
        );
    }
}