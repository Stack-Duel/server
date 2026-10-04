namespace StackDuel.Application.Messaging.Messages;

/// <summary>
/// Published (with a scheduled/delayed enqueue time) when a game starts, timed to arrive at
/// the moment the game's time limit elapses. The consumer must re-verify against the current
/// game state rather than trusting the message's arrival time as truth — see
/// <see cref="RescheduleCount"/> and <see cref="ExpectedStartedAt"/>.
/// </summary>
public sealed record GameTimeExpiredMessage(Guid GameId, DateTime ExpectedStartedAt, int RescheduleCount) : IMessage
{
    public static string QueueName => "game-time-expired";

    /// <summary>
    /// Hard cap on self-reschedules. If exceeded, the consumer finalizes the game regardless
    /// of the early-arrival check rather than rescheduling again.
    /// </summary>
    public const int MaxReschedules = 3;
}