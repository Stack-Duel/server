using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Submissions.Enums;

namespace StackDuel.Application.Notifications;

public interface IGameNotificationService
{
    Task NotifyGameCompletedAsync(Guid gameId, GameStatus status, DateTime endedAt);

    Task NotifyGameLobbyUpdatedAsync(Guid gameId);

    Task NotifyGameProgressUpdatedAsync(Guid gameId);

    Task NotifyGameParticipantAttemptedAsync(Guid gameId, Guid userId, SubmissionStatus status, DateTime attemptedAt);

    Task NotifyGameParticipantReactedAsync(Guid gameId, Guid userId, string emoji, DateTime sentAt);
}