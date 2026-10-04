using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Games.ValueObjects;
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
        ProblemSession = null;
    }

    private GameParticipant() { }

    public Guid UserId { get; private set; }

    public int SeatNo { get; private set; }

    public DateTime JoinedAt { get; private set; }

    public int Score { get; private set; }

    public int SkipsRemaining { get; private set; }

    public GameProblemSession? ProblemSession { get; private set; }

    public DateTime? ForfeitedAt { get; private set; }

    public bool HasForfeited => ForfeitedAt is not null;

    public DateTime? FinishedAt { get; private set; }

    public bool HasFinishedProblems => FinishedAt is not null;

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

    /// <summary>
    /// Whether this participant may attempt <paramref name="action"/> against <paramref name="problemId"/>
    /// right now: still playing, has an initialized session, and that session's current problem is
    /// the one being acted on. Backs <see cref="Game.EvaluateProblemAttempt"/> — see that for the
    /// game-level checks (participant exists, game running) layered around this.
    /// </summary>
    public ProblemAttemptEligibility CanAttemptProblem(ParticipantAction action, Guid problemId)
    {
        if (!CanPerform(action))
            return ProblemAttemptEligibility.ParticipantStopped;

        if (ProblemSession is null)
            return ProblemAttemptEligibility.ProblemSessionNotInitialized;

        if (ProblemSession.CurrentProblemId != problemId)
            return ProblemAttemptEligibility.ProblemMismatch;

        return ProblemAttemptEligibility.Eligible;
    }

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

    public void InitializeProblemSession(Guid initialProblemId)
    {
        if (ProblemSession is not null)
            throw new InvalidOperationException("Problem session is already initialized.");

        ProblemSession = new GameProblemSession(initialProblemId);
    }

    public void IncrementScore()
    {
        Score++;
    }

    public void SetActiveSubmission(Guid submissionId)
    {
        if (ProblemSession is null)
            throw new InvalidOperationException("Problem session is not initialized.");

        // Replace reference so EF Core detects the change (jsonb value conversion snapshot issue).
        var next = new GameProblemSession(ProblemSession.CurrentProblemId);
        foreach (var solvedId in ProblemSession.SolvedProblemIds)
            next.AddSolvedProblem(solvedId);
        foreach (var skippedId in ProblemSession.SkippedProblemIds)
            next.AddSkippedProblem(skippedId);
        foreach (var (problemId, recordedSubmissionId) in ProblemSession.SolvedProblemSubmissionIds)
            next.RecordSolvedSubmission(problemId, recordedSubmissionId);
        next.SetActiveSubmission(submissionId);
        ProblemSession = next;
    }

    public void AdvanceProblem(Guid nextProblemId)
    {
        if (ProblemSession is null)
            throw new InvalidOperationException("Problem session is not initialized.");

        // Build a new GameProblemSession instance rather than mutating in place.
        // ProblemSession is stored as a jsonb column with a value conversion;
        // EF Core's snapshot points to the same object reference, so in-place
        // mutations are invisible to change detection. Replacing the reference
        // guarantees the property is detected as changed and saved.
        var next = new GameProblemSession(nextProblemId);
        foreach (var solvedId in ProblemSession.SolvedProblemIds)
            next.AddSolvedProblem(solvedId);
        next.AddSolvedProblem(ProblemSession.CurrentProblemId);
        foreach (var skippedId in ProblemSession.SkippedProblemIds)
            next.AddSkippedProblem(skippedId);

        foreach (var (problemId, recordedSubmissionId) in ProblemSession.SolvedProblemSubmissionIds)
            next.RecordSolvedSubmission(problemId, recordedSubmissionId);
        if (ProblemSession.ActiveSubmissionId is { } activeSubmissionId)
            next.RecordSolvedSubmission(ProblemSession.CurrentProblemId, activeSubmissionId);

        ProblemSession = next;
    }

    public void SkipToProblem(Guid nextProblemId)
    {
        if (ProblemSession is null)
            throw new InvalidOperationException("Problem session is not initialized.");

        UseSkip();

        // Same rebuild-rather-than-mutate approach as AdvanceProblem (jsonb value
        // conversion snapshot issue). The skipped problem goes to SkippedProblemIds
        // instead of SolvedProblemIds, so it's excluded from future selection without
        // counting toward the participant's score.
        var next = new GameProblemSession(nextProblemId);
        foreach (var solvedId in ProblemSession.SolvedProblemIds)
            next.AddSolvedProblem(solvedId);
        foreach (var skippedId in ProblemSession.SkippedProblemIds)
            next.AddSkippedProblem(skippedId);
        next.AddSkippedProblem(ProblemSession.CurrentProblemId);

        foreach (var (problemId, recordedSubmissionId) in ProblemSession.SolvedProblemSubmissionIds)
            next.RecordSolvedSubmission(problemId, recordedSubmissionId);

        ProblemSession = next;
    }

    public void UseSkip()
    {
        if (SkipsRemaining <= 0)
            throw new InvalidOperationException("No skips remaining.");

        SkipsRemaining--;
    }
}