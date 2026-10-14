using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

public sealed class GameParticipant : Entity
{
    public const int TotalSkips = 3;

    public GameParticipant(Guid userId, int seatNo)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id must not be empty.", nameof(userId));

        if (seatNo <= 0)
            throw new ArgumentException("Seat number must be greater than zero.", nameof(seatNo));

        UserId = userId;
        SeatNo = seatNo;
        JoinedAt = DateTime.UtcNow;
        Score = 0;
        SkipsRemaining = TotalSkips;
    }

    private GameParticipant() { }

    public Guid UserId { get; private set; }

    public int SeatNo { get; private set; }

    public DateTime JoinedAt { get; private set; }

    public int Score { get; private set; }

    public int SkipsRemaining { get; private set; }

    public DateTime? ForfeitedAt { get; private set; }

    public bool HasForfeited => ForfeitedAt is not null;

    public DateTime? FinishedAt { get; private set; }

    public bool HasFinishedProblems => FinishedAt is not null;

    /// <summary>
    /// The rounds this participant has finished with, one row each. The round they are currently on
    /// is <see cref="CurrentRoundId"/> and deliberately has no row here — that is what keeps every
    /// column on <see cref="GameRoundResult"/> required. A round only identifies its problem via
    /// <see cref="Game.Rounds"/>, so anything needing the actual problem id goes through
    /// <see cref="Game"/> rather than this entity.
    /// </summary>
    public IReadOnlyCollection<GameRoundResult> RoundResults => _roundResults.AsReadOnly();

    /// <summary>
    /// The round this participant is on, or null before their first round and after they have
    /// finished. A single column, so "at most one round in progress" is structural rather than an
    /// invariant some index or handler has to uphold.
    /// </summary>
    public Guid? CurrentRoundId { get; private set; }

    /// <summary>
    /// When the current round was started, carried here so it can be recorded on the result once
    /// the round completes. Set exactly when <see cref="CurrentRoundId"/> is.
    /// </summary>
    public DateTime? CurrentRoundStartedAt { get; private set; }

    /// <summary>
    /// The number of rounds this participant has reached, counting the one they're on. Doubles as
    /// the position of their next round, which is what the problem selection strategy is seeded
    /// with.
    /// </summary>
    public int RoundsReached => _roundResults.Count + (CurrentRoundId.HasValue ? 1 : 0);

    /// <summary>
    /// True once this participant has stopped playing for any reason — quit (<see cref="Forfeit"/>)
    /// or ran out of problems to solve (<see cref="FinishProblems"/>). Game completion (see
    /// Game.Forfeit/Game.FinishProblemsFor) is driven by every participant reaching this, not by
    /// either reason alone.
    /// </summary>
    public bool HasStoppedPlaying => HasForfeited || HasFinishedProblems;

    /// <summary>
    /// Single source of truth for participant lifecycle, derived from <see cref="ForfeitedAt"/> and
    /// <see cref="FinishedAt"/> rather than stored directly, so the two can never disagree. Backs
    /// <see cref="CanPerform"/> — see that for why callers should gate actions through this instead
    /// of checking <see cref="HasStoppedPlaying"/>/<see cref="HasForfeited"/> individually.
    /// </summary>
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

    /// <summary>
    /// Whether this participant may currently attempt <paramref name="action"/>. The single table
    /// CompleteProblemHandler, SkipProblemHandler, and ForfeitGameHandler all check against, instead
    /// of each re-deriving "has this participant stopped playing" independently.
    /// </summary>
    public bool CanPerform(ParticipantAction action) => AllowedActions[PlayState].Contains(action);

    public void Forfeit()
    {
        if (HasStoppedPlaying)
            throw new InvalidOperationException("Participant has already stopped playing.");

        ForfeitedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marks this participant as having exhausted the problem pool — there was nothing left to
    /// serve them (see DifficultyRampProblemSelectionStrategy returning null). Distinct from
    /// forfeiting: they didn't quit, they ran out of content.
    /// </summary>
    public void FinishProblems()
    {
        if (HasStoppedPlaying)
            throw new InvalidOperationException("Participant has already stopped playing.");

        FinishedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Puts this participant on <paramref name="roundId"/>. Rejects a second concurrent round rather
    /// than silently leaving two in progress, and rejects re-entering a round already finished — a
    /// participant is offered each round exactly once.
    /// </summary>
    internal void StartRound(Guid roundId)
    {
        if (CurrentRoundId.HasValue)
            throw new InvalidOperationException("Participant is already on a round.");

        if (_roundResults.Any(result => result.RoundId == roundId))
            throw new InvalidOperationException("Participant has already played this round.");

        CurrentRoundId = roundId;
        CurrentRoundStartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records how the current round ended and clears the cursor, leaving the participant on no
    /// round until <see cref="StartRound"/> moves them to the next one.
    /// </summary>
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