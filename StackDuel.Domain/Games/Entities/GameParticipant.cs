using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

public sealed class GameParticipant : Entity
{
    public const int TotalSkips = 3;

    public GameParticipant(Guid gameId, Guid userId, int seatNo)
    {
        if (gameId == Guid.Empty)
            throw new ArgumentException("Game id must not be empty.", nameof(gameId));

        if (userId == Guid.Empty)
            throw new ArgumentException("User id must not be empty.", nameof(userId));

        if (seatNo <= 0)
            throw new ArgumentException("Seat number must be greater than zero.", nameof(seatNo));

        GameId = gameId;
        UserId = userId;
        SeatNo = seatNo;
        JoinedAt = DateTime.UtcNow;
        Score = 0;
        SkipsRemaining = TotalSkips;
    }

    private GameParticipant() { }

    public Guid GameId { get; private set; }

    public Guid UserId { get; private set; }

    public int SeatNo { get; private set; }

    public DateTime JoinedAt { get; private set; }

    public int Score { get; private set; }

    public int SkipsRemaining { get; private set; }

    public DateTime? ForfeitedAt { get; private set; }

    public bool HasForfeited => ForfeitedAt is not null;

    public DateTime? FinishedAt { get; private set; }

    public bool HasFinishedProblems => FinishedAt is not null;

    public IReadOnlyCollection<GameRoundResult> RoundResults => _roundResults.AsReadOnly();

    public Guid? CurrentRoundId { get; private set; }

    public DateTime? CurrentRoundStartedAt { get; private set; }

    public int RoundsReached => _roundResults.Count + (CurrentRoundId.HasValue ? 1 : 0);

    public bool HasStoppedPlaying => HasForfeited || HasFinishedProblems;

    public ParticipantPlayState PlayState
    {
        get
        {
            if (HasForfeited)
                return ParticipantPlayState.Forfeited;

            return HasFinishedProblems ? ParticipantPlayState.Finished : ParticipantPlayState.InProgress;
        }
    }

    private static readonly Dictionary<ParticipantPlayState, ParticipantAction[]> AllowedActions = new()
    {
        [ParticipantPlayState.InProgress] =
        [
            ParticipantAction.CompleteProblem,
            ParticipantAction.SkipProblem,
            ParticipantAction.Forfeit,
        ],
        [ParticipantPlayState.Finished] = [],
        [ParticipantPlayState.Forfeited] = [],
    };

    public bool CanPerform(ParticipantAction action) => AllowedActions[PlayState].Contains(action);

    public void Forfeit()
    {
        if (HasStoppedPlaying)
            throw new InvalidOperationException("Participant has already stopped playing.");

        ForfeitedAt = DateTime.UtcNow;
    }

    public void FinishProblems()
    {
        if (HasStoppedPlaying)
            throw new InvalidOperationException("Participant has already stopped playing.");

        FinishedAt = DateTime.UtcNow;
    }

    internal void StartRound(Guid roundId)
    {
        if (CurrentRoundId.HasValue)
            throw new InvalidOperationException("Participant is already on a round.");

        if (_roundResults.Any(result => result.RoundId == roundId))
            throw new InvalidOperationException("Participant has already played this round.");

        CurrentRoundId = roundId;
        CurrentRoundStartedAt = DateTime.UtcNow;
    }

    internal GameRoundResult CompleteCurrentRound(GameRoundOutcome outcome, Guid? solvingSubmissionId)
    {
        if (!CurrentRoundId.HasValue)
            throw new InvalidOperationException("Participant is not on a round.");

        GameRoundResult result = outcome switch
        {
            GameRoundOutcome.Solved => GameRoundResult.Solved(
                CurrentRoundId.Value,
                CurrentRoundStartedAt!.Value,
                solvingSubmissionId
                    ?? throw new InvalidOperationException("A round cannot be solved without a submission.")
            ),
            GameRoundOutcome.Skipped => GameRoundResult.Skipped(CurrentRoundId.Value, CurrentRoundStartedAt!.Value),
            _ => throw new ArgumentOutOfRangeException(nameof(outcome), outcome, "Unhandled round outcome."),
        };

        _roundResults.Add(result);
        CurrentRoundId = null;
        CurrentRoundStartedAt = null;

        return result;
    }

    public void IncrementScore()
    {
        Score++;
    }

    public void UseSkip()
    {
        if (SkipsRemaining <= 0)
            throw new InvalidOperationException("No skips remaining.");

        SkipsRemaining--;
    }

    private readonly List<GameRoundResult> _roundResults = [];
}