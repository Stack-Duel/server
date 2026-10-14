using StackDuel.Domain.Games;
using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Games.Events;
using StackDuel.Domain.SeedWork;

namespace StackDuel.Domain.Games.Entities;

public sealed class Game : AggregateRoot
{
    public Game(
        Guid gameModeId,
        Guid poolId,
        IEnumerable<Guid> trackIds,
        IEnumerable<Guid> playerUserIds,
        int timeLimitInSeconds,
        Guid? lobbyId = null,
        IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>? trackLanguageIds = null,
        bool skipsEnabled = true
    )
    {
        if (gameModeId == Guid.Empty)
            throw new ArgumentException("Game mode id must not be empty.", nameof(gameModeId));

        if (poolId == Guid.Empty)
            throw new ArgumentException("Pool id must not be empty.", nameof(poolId));

        Guid[] trackIdArray =
            trackIds?.Where(id => id != Guid.Empty).Distinct().ToArray()
            ?? throw new ArgumentNullException(nameof(trackIds));

        if (trackIdArray.Length == 0)
            throw new ArgumentException("At least one track is required.", nameof(trackIds));

        if (timeLimitInSeconds <= 0)
            throw new ArgumentException("Time limit must be greater than zero.", nameof(timeLimitInSeconds));

        Guid[] participantIds =
            playerUserIds?.Where(id => id != Guid.Empty).Distinct().ToArray()
            ?? throw new ArgumentNullException(nameof(playerUserIds));

        if (participantIds.Length == 0)
            throw new ArgumentException("At least one player is required.", nameof(playerUserIds));

        GameModeId = gameModeId;
        PoolId = poolId;
        LobbyId = lobbyId;
        TimeLimitInSeconds = timeLimitInSeconds;
        SkipsEnabled = skipsEnabled;
        CreatedAt = DateTime.UtcNow;
        Status = GameStatus.Pending;
        JoinCode = GameJoinCode.Generate();

        foreach (Guid trackId in trackIdArray)
        {
            IReadOnlyList<Guid> languageIds =
                trackLanguageIds is not null && trackLanguageIds.TryGetValue(trackId, out IReadOnlyList<Guid>? ids)
                    ? ids
                    : [];

            _tracks.Add(new GameTrack(trackId, languageIds));
        }

        int seatNo = 1;
        foreach (Guid userId in participantIds)
        {
            _participants.Add(new GameParticipant(userId, seatNo));
            seatNo++;
        }
    }

    public void Join(Guid userId, int maxPlayers)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("User id must not be empty.", nameof(userId));

        if (Status != GameStatus.Pending)
            throw new InvalidOperationException("Only pending games can be joined.");

        if (_participants.Any(p => p.UserId == userId))
            throw new InvalidOperationException("User is already a participant in this game.");

        if (_participants.Count >= maxPlayers)
            throw new InvalidOperationException("Game is already full.");

        // One past the highest seat currently in use, not _participants.Count + 1 — a seat
        // freed by Leave must never be reissued while a lower-numbered seat is still taken,
        // or the new participant would collide with it on the (game_id, seat_no) unique index.
        int nextSeatNo = _participants.Count == 0 ? 1 : _participants.Max(p => p.SeatNo) + 1;
        _participants.Add(new GameParticipant(userId, nextSeatNo));

        AddDomainEvent(new GameLobbyUpdatedDomainEvent(Id));
    }

    /// <summary>
    /// Removes a participant from a still-Pending lobby. If they were the last one left, the
    /// lobby is cancelled outright — there's no one left to host it. Deliberately never
    /// renumbers the remaining seats: <see cref="HostUserId"/> is always whoever currently holds
    /// the lowest seat number, so a non-host leaving just shrinks the roster, and a host leaving
    /// hands the role to the next-lowest seat automatically without touching anyone else's row.
    /// </summary>
    public void Leave(Guid userId)
    {
        if (Status != GameStatus.Pending)
            throw new InvalidOperationException("Only pending games can be left.");

        GameParticipant participant =
            _participants.FirstOrDefault(p => p.UserId == userId)
            ?? throw new InvalidOperationException("User is not a participant in this game.");

        _participants.Remove(participant);

        if (_participants.Count == 0)
        {
            Cancel();
        }
        else
        {
            AddDomainEvent(new GameLobbyUpdatedDomainEvent(Id));
        }
    }

    /// <summary>
    /// Closes the lobby outright — cancels it regardless of how many participants remain, unlike
    /// <see cref="Leave"/> (which only removes the caller and keeps the lobby going for whoever's
    /// left, if anyone). Only valid while still Pending — once the game has started there's no
    /// "close", only Forfeit or letting the clock run out. Authorization (host-only) is the
    /// caller's responsibility, matching Start/Forfeit/etc.
    /// </summary>
    public void CloseLobby()
    {
        if (Status != GameStatus.Pending)
            throw new InvalidOperationException("Only a pending lobby can be closed.");

        Cancel();

        AddDomainEvent(new GameLobbyUpdatedDomainEvent(Id));
    }

    /// <summary>
    /// The current host: whoever holds the lowest seat number among current participants. Not
    /// necessarily seat 1 — Leave never renumbers, so if the original seat-1 host leaves, the
    /// role passes to whoever has the next-lowest seat instead. Null only for an empty
    /// (Cancelled) game, since every Game always has at least one participant otherwise.
    /// </summary>
    public Guid? HostUserId => _participants.OrderBy(p => p.SeatNo).FirstOrDefault()?.UserId;

    public void Start(int countdownSeconds = 0)
    {
        if (Status != GameStatus.Pending)
            throw new InvalidOperationException("Only pending games can be started.");

        Status = GameStatus.Running;
        StartedAt = DateTime.UtcNow.AddSeconds(countdownSeconds);

        AddDomainEvent(new GameStartedDomainEvent(Id, StartedAt.Value, TimeLimitInSeconds));
        AddDomainEvent(new GameLobbyUpdatedDomainEvent(Id));
    }

    public void Complete()
    {
        if (Status != GameStatus.Running)
            throw new InvalidOperationException("Only running games can be completed.");

        Status = GameStatus.Completed;
        EndedAt = DateTime.UtcNow;

        AddDomainEvent(new GameCompletedDomainEvent(Id, Status, EndedAt.Value));
    }

    /// <summary>
    /// True when a Running game's time limit has elapsed. Used as a defense-in-depth check —
    /// independent of whether the scheduled GameTimeExpiredMessage/sweep job has finalized the
    /// game yet — anywhere the game is read or acted on, so a slow/lost message can never let
    /// the game outlive its clock.
    /// </summary>
    public bool HasExpired(DateTime nowUtc) =>
        Status == GameStatus.Running
        && StartedAt is not null
        && nowUtc >= StartedAt.Value.AddSeconds(TimeLimitInSeconds);

    /// <summary>
    /// Completes the game if it has expired, returning whether it did. Callers that mutate
    /// state on a Running game (submit, complete-problem) or read it (get game state) should
    /// call this first so an expired game is always finalized before anything else happens,
    /// regardless of whether the message-bus expiry path has caught up yet.
    /// </summary>
    public bool CompleteIfExpired(DateTime nowUtc)
    {
        if (!HasExpired(nowUtc))
            return false;

        Complete();
        return true;
    }

    public void Cancel()
    {
        if (Status == GameStatus.Completed || Status == GameStatus.Cancelled)
            throw new InvalidOperationException("Completed or cancelled games cannot be cancelled again.");

        Status = GameStatus.Cancelled;
        EndedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Forfeits one participant. The game itself only ends once every participant has stopped
    /// playing (see <see cref="CompleteIfEveryoneStopped"/>) — in a multiplayer mode, one player
    /// quitting shouldn't cut the others' run short; they keep playing (and can still improve
    /// their score) until the clock runs out or everyone else has also stopped, at which point
    /// the highest score among all participants wins.
    /// </summary>
    public void Forfeit(Guid userId)
    {
        if (Status != GameStatus.Running)
            throw new InvalidOperationException("Only running games can be forfeited.");

        GameParticipant participant =
            _participants.FirstOrDefault(p => p.UserId == userId)
            ?? throw new InvalidOperationException("User is not a participant in this game.");

        participant.Forfeit();

        CompleteIfEveryoneStopped();
    }

    /// <summary>
    /// Everything CompleteProblemHandler and SkipProblemHandler need to know before touching a
    /// participant's current problem, in one call: is there such a participant, did the game just
    /// auto-complete because its clock ran out (checked here, before the running-check, exactly
    /// like the two handlers used to do it independently), and — if it's still running — is this
    /// participant actually eligible to attempt <paramref name="action"/> against
    /// <paramref name="problemId"/>. Forfeit isn't covered here: it doesn't target a problem, so it
    /// only ever needed <see cref="GameParticipant.CanPerform"/>.
    /// </summary>
    public GameProblemAttempt EvaluateProblemAttempt(
        Guid userId,
        Guid problemId,
        ParticipantAction action,
        DateTime nowUtc
    )
    {
        GameParticipant? participant = _participants.FirstOrDefault(p => p.UserId == userId);
        if (participant is null)
            return new GameProblemAttempt(null, GameJustExpired: false, ProblemAttemptEligibility.ParticipantNotFound);

        bool justExpired = CompleteIfExpired(nowUtc);

        if (Status != GameStatus.Running)
            return new GameProblemAttempt(participant, justExpired, ProblemAttemptEligibility.GameNotRunning);

        return new GameProblemAttempt(participant, justExpired, EvaluateEligibility(participant, action, problemId));
    }

    /// <summary>
    /// Lives here rather than on <see cref="GameParticipant"/> because matching the requested
    /// problem needs both sides: the participant knows which round they're on, but only the game
    /// knows which problem that round holds.
    /// </summary>
    private ProblemAttemptEligibility EvaluateEligibility(
        GameParticipant participant,
        ParticipantAction action,
        Guid problemId
    )
    {
        if (!participant.CanPerform(action))
            return ProblemAttemptEligibility.ParticipantStopped;

        Guid? currentProblemId = CurrentProblemIdFor(participant);
        if (!currentProblemId.HasValue)
            return ProblemAttemptEligibility.RoundNotStarted;

        return currentProblemId.Value == problemId
            ? ProblemAttemptEligibility.Eligible
            : ProblemAttemptEligibility.ProblemMismatch;
    }

    private Guid? CurrentProblemIdFor(GameParticipant participant)
    {
        Guid? roundId = participant.CurrentRoundId;
        if (!roundId.HasValue)
            return null;

        return _rounds.FirstOrDefault(round => round.Id == roundId.Value)?.ProblemId;
    }

    /// <summary>
    /// Records a solved problem for a participant — bumps their score and raises
    /// GameProgressUpdatedDomainEvent so opponents watching the Score tab find out promptly
    /// instead of seeing stale numbers until something else happens to trigger a refetch.
    /// </summary>
    public void RecordProblemSolved(Guid userId)
    {
        if (Status != GameStatus.Running)
            throw new InvalidOperationException("Only running games can record progress.");

        GameParticipant participant =
            _participants.FirstOrDefault(p => p.UserId == userId)
            ?? throw new InvalidOperationException("User is not a participant in this game.");

        participant.IncrementScore();

        AddDomainEvent(new GameProgressUpdatedDomainEvent(Id));
    }

    /// <summary>
    /// Marks a participant as having exhausted the problem pool. Same "doesn't end the game alone"
    /// rule as <see cref="Forfeit"/> — they're just done for a different reason (nothing left to
    /// solve, rather than quitting).
    /// </summary>
    public void FinishProblemsFor(Guid userId)
    {
        if (Status != GameStatus.Running)
            throw new InvalidOperationException("Only running games can be updated.");

        GameParticipant participant =
            _participants.FirstOrDefault(p => p.UserId == userId)
            ?? throw new InvalidOperationException("User is not a participant in this game.");

        participant.FinishProblems();

        CompleteIfEveryoneStopped();
    }

    /// <summary>
    /// Completes the game if every participant has stopped playing (forfeited or run out of
    /// problems — any mix), returning whether it did. Called after Forfeit/FinishProblemsFor, and
    /// also safe to call from a read path (e.g. GetGameState) as a self-healing check: two
    /// participants stopping via concurrent requests can each read the game before the other's
    /// write lands, so neither sees "everyone's done" and the game is left Running with nothing
    /// left to happen in it until the expiry sweep eventually catches it. Calling this on the next
    /// read closes that window immediately instead of waiting for the clock.
    /// </summary>
    public bool CompleteIfEveryoneStopped()
    {
        if (Status != GameStatus.Running)
            return false;

        if (!_participants.All(p => p.HasStoppedPlaying))
            return false;

        Complete();
        return true;
    }

    /// <summary>
    /// The problem id every participant who has reached <paramref name="position"/> was (or will
    /// be) given, if anyone has reached it yet. Null means no participant has gotten there —
    /// the caller is the first, and must generate one and record it via
    /// <see cref="AppendRound"/> so every later arrival reuses the exact same problem instead
    /// of each participant rolling independently.
    /// </summary>
    public Guid? ProblemIdAtPosition(int position) =>
        _rounds.FirstOrDefault(round => round.Position == position)?.ProblemId;

    /// <summary>
    /// Extends the shared sequence by one round, at whatever the next position is. Only ever called
    /// after <see cref="ProblemIdAtPosition"/> came back null for that position — this is what makes
    /// the sequence a single shared list instead of independent per-participant selection: the first
    /// participant to reach a new position generates it, and it's persisted here so everyone after
    /// them lands on the identical problem.
    /// </summary>
    public void AppendRound(Guid problemId)
    {
        if (problemId == Guid.Empty)
            throw new ArgumentException("Problem id must not be empty.", nameof(problemId));

        if (_rounds.Any(round => round.ProblemId == problemId))
            throw new DuplicateGameProblemException(Id, problemId);

        _rounds.Add(new GameRound(_rounds.Count, problemId));
    }

    /// <summary>
    /// The problem a participant is currently on, or null before their first round has been started.
    /// </summary>
    public Guid? CurrentProblemIdFor(Guid userId)
    {
        GameParticipant? participant = _participants.FirstOrDefault(p => p.UserId == userId);
        return participant is null ? null : CurrentProblemIdFor(participant);
    }

    /// <summary>
    /// Every problem this participant should never be offered again: solved, skipped, or the one
    /// they're currently on — i.e. the problem behind every round they have reached. The single
    /// source of truth for the exclude list handed to IProblemSelectionStrategy, so a skipped
    /// problem can't quietly drop out of one caller's exclusions while staying in another's.
    /// </summary>
    public IReadOnlyList<Guid> ExcludedProblemIdsFor(Guid userId)
    {
        GameParticipant? participant = _participants.FirstOrDefault(p => p.UserId == userId);
        if (participant is null)
            return [];

        HashSet<Guid> reachedRoundIds = [.. participant.RoundResults.Select(result => result.RoundId)];
        if (participant.CurrentRoundId.HasValue)
            reachedRoundIds.Add(participant.CurrentRoundId.Value);

        return [.. _rounds.Where(round => reachedRoundIds.Contains(round.Id)).Select(round => round.ProblemId)];
    }

    /// <summary>
    /// How many rounds a participant has reached, counting the one they're on. Doubles as the
    /// position their next round would occupy.
    /// </summary>
    public int RoundsReachedFor(Guid userId) =>
        _participants.FirstOrDefault(p => p.UserId == userId)?.RoundsReached ?? 0;

    /// <summary>
    /// Puts a participant on the round holding <paramref name="problemId"/> — their first round, or
    /// the next one after <see cref="SolveCurrentRoundAndAdvance"/>/<see cref="SkipCurrentRoundAndAdvance"/>.
    /// The round must already exist; <see cref="AppendRound"/> is what creates it.
    /// </summary>
    public void StartRoundFor(Guid userId, Guid problemId)
    {
        RequireParticipant(userId).StartRound(RequireRoundFor(problemId).Id);
    }

    /// <summary>
    /// Records the current round as solved by <paramref name="solvingSubmissionId"/> and moves the
    /// participant on. The caller is responsible for having verified that submission really is this
    /// participant's latest attempt at this round — that lives in <c>game_round_submissions</c>, not
    /// in this aggregate, so it cannot be checked here.
    /// </summary>
    public void SolveCurrentRoundAndAdvance(Guid userId, Guid nextProblemId, Guid solvingSubmissionId)
    {
        Advance(userId, nextProblemId, GameRoundOutcome.Solved, solvingSubmissionId);
    }

    /// <summary>
    /// Consumes a skip and moves the participant on. The skip is spent even though the round wasn't
    /// solved, matching the old SkipToProblem behaviour.
    /// </summary>
    public void SkipCurrentRoundAndAdvance(Guid userId, Guid nextProblemId)
    {
        Advance(userId, nextProblemId, GameRoundOutcome.Skipped, solvingSubmissionId: null);
    }

    /// <summary>
    /// Every precondition is checked before anything mutates, so a rejected advance leaves the
    /// participant exactly as it found them. Ordering matters here: charging the skip before
    /// confirming there is a round to leave and a round to arrive at would cost the participant a
    /// skip on a call that then throws.
    /// </summary>
    private void Advance(Guid userId, Guid nextProblemId, GameRoundOutcome outcome, Guid? solvingSubmissionId)
    {
        GameParticipant participant = RequireParticipant(userId);
        GameRound nextRound = RequireRoundFor(nextProblemId);

        if (!participant.CurrentRoundId.HasValue)
            throw new InvalidOperationException("Participant is not on a round.");

        if (participant.RoundResults.Any(result => result.RoundId == nextRound.Id))
            throw new InvalidOperationException("Participant has already played the round being advanced to.");

        if (outcome == GameRoundOutcome.Skipped)
            participant.UseSkip();

        participant.CompleteCurrentRound(outcome, solvingSubmissionId);
        participant.StartRound(nextRound.Id);
    }

    private GameParticipant RequireParticipant(Guid userId) =>
        _participants.FirstOrDefault(p => p.UserId == userId)
        ?? throw new InvalidOperationException("User is not a participant in this game.");

    private GameRound RequireRoundFor(Guid problemId) =>
        _rounds.FirstOrDefault(round => round.ProblemId == problemId)
        ?? throw new InvalidOperationException($"Game has no round for problem '{problemId}'.");

    /// <summary>
    /// Records the transport handle (e.g. Azure Service Bus sequence number) for the scheduled
    /// GameTimeExpiredMessage tied to this run of the game, so it can be cancelled later if the
    /// game ends early (forfeit) or gets rescheduled.
    /// </summary>
    public void AttachScheduledExpiry(long sequenceNumber)
    {
        ScheduledExpirySequenceNumber = sequenceNumber;
    }

    /// <summary>
    /// Clears the recorded scheduled-expiry handle once it's no longer relevant — e.g. after a
    /// cancellation attempt (successful or not) on early forfeit. Safe to call regardless of
    /// whether the underlying transport could actually honor the cancellation; the
    /// GameTimeExpiredMessage consumer re-verifies game state independently either way.
    /// </summary>
    public void ClearScheduledExpiry()
    {
        ScheduledExpirySequenceNumber = null;
    }

    private Game() { }

    public Guid? LobbyId { get; private set; }
    public Guid GameModeId { get; private set; }
    public string JoinCode { get; private set; }
    public int TimeLimitInSeconds { get; private set; }
    public bool SkipsEnabled { get; private set; }
    public GameStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? StartedAt { get; private set; }
    public DateTime? EndedAt { get; private set; }

    /// <summary>
    /// Azure Service Bus sequence number for the scheduled expiry message, when the active
    /// transport supports cancellation by handle. Null on RabbitMQ (no such handle exists) or
    /// before the domain event handler has attached it.
    /// </summary>
    public long? ScheduledExpirySequenceNumber { get; private set; }

    public Guid PoolId { get; private set; }

    public IReadOnlyCollection<GameParticipant> Participants => _participants.AsReadOnly();

    public IReadOnlyCollection<GameTrack> Tracks => _tracks.AsReadOnly();

    /// <summary>
    /// The shared sequence of rounds for this game, in position order. Every participant advances
    /// through this same list rather than each getting an independently-selected run of problems —
    /// see <see cref="ProblemIdAtPosition"/> and <see cref="AppendRound"/>.
    /// </summary>
    public IReadOnlyCollection<GameRound> Rounds => _rounds.AsReadOnly();

    private readonly List<GameParticipant> _participants = [];
    private readonly List<GameTrack> _tracks = [];
    private readonly List<GameRound> _rounds = [];
}