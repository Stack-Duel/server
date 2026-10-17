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
            _participants.Add(new GameParticipant(Id, userId, seatNo));
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

        int nextSeatNo = _participants.Count == 0 ? 1 : _participants.Max(p => p.SeatNo) + 1;
        _participants.Add(new GameParticipant(Id, userId, nextSeatNo));

        AddDomainEvent(new GameLobbyUpdatedDomainEvent(Id));
    }

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

    public void CloseLobby()
    {
        if (Status != GameStatus.Pending)
            throw new InvalidOperationException("Only a pending lobby can be closed.");

        Cancel();

        AddDomainEvent(new GameLobbyUpdatedDomainEvent(Id));
    }

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

    public bool HasExpired(DateTime nowUtc) =>
        Status == GameStatus.Running
        && StartedAt is not null
        && nowUtc >= StartedAt.Value.AddSeconds(TimeLimitInSeconds);

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

    public bool CompleteIfEveryoneStopped()
    {
        if (Status != GameStatus.Running)
            return false;

        if (!_participants.All(p => p.HasStoppedPlaying))
            return false;

        Complete();
        return true;
    }

    public Guid? ProblemIdAtPosition(int position) =>
        _rounds.FirstOrDefault(round => round.Position == position)?.ProblemId;

    public void AppendRound(Guid problemId)
    {
        if (problemId == Guid.Empty)
            throw new ArgumentException("Problem id must not be empty.", nameof(problemId));

        if (_rounds.Any(round => round.ProblemId == problemId))
            throw new DuplicateGameProblemException(Id, problemId);

        _rounds.Add(new GameRound(Id, _rounds.Count, problemId));
    }

    public Guid? CurrentProblemIdFor(Guid userId)
    {
        GameParticipant? participant = _participants.FirstOrDefault(p => p.UserId == userId);
        return participant is null ? null : CurrentProblemIdFor(participant);
    }

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

    public int RoundsReachedFor(Guid userId) =>
        _participants.FirstOrDefault(p => p.UserId == userId)?.RoundsReached ?? 0;

    public void StartRoundFor(Guid userId, Guid problemId)
    {
        RequireParticipant(userId).StartRound(RequireRoundFor(problemId).Id);
    }

    public void SolveCurrentRound(Guid userId, Guid solvingSubmissionId)
    {
        CompleteCurrentRound(userId, GameRoundOutcome.Solved, solvingSubmissionId);
    }

    public void SkipCurrentRound(Guid userId)
    {
        CompleteCurrentRound(userId, GameRoundOutcome.Skipped, solvingSubmissionId: null);
    }

    public void SolveCurrentRoundAndAdvance(Guid userId, Guid nextProblemId, Guid solvingSubmissionId)
    {
        Advance(userId, nextProblemId, GameRoundOutcome.Solved, solvingSubmissionId);
    }

    public void SkipCurrentRoundAndAdvance(Guid userId, Guid nextProblemId)
    {
        Advance(userId, nextProblemId, GameRoundOutcome.Skipped, solvingSubmissionId: null);
    }

    private void Advance(Guid userId, Guid nextProblemId, GameRoundOutcome outcome, Guid? solvingSubmissionId)
    {
        GameParticipant participant = RequireParticipant(userId);
        GameRound nextRound = RequireRoundFor(nextProblemId);

        if (participant.RoundResults.Any(result => result.RoundId == nextRound.Id))
            throw new InvalidOperationException("Participant has already played the round being advanced to.");

        CompleteCurrentRound(userId, outcome, solvingSubmissionId);
        participant.StartRound(nextRound.Id);
    }

    private void CompleteCurrentRound(Guid userId, GameRoundOutcome outcome, Guid? solvingSubmissionId)
    {
        GameParticipant participant = RequireParticipant(userId);

        if (!participant.CurrentRoundId.HasValue)
            throw new InvalidOperationException("Participant is not on a round.");

        if (outcome == GameRoundOutcome.Skipped)
            participant.UseSkip();

        participant.CompleteCurrentRound(outcome, solvingSubmissionId);
    }

    private GameParticipant RequireParticipant(Guid userId) =>
        _participants.FirstOrDefault(p => p.UserId == userId)
        ?? throw new InvalidOperationException("User is not a participant in this game.");

    private GameRound RequireRoundFor(Guid problemId) =>
        _rounds.FirstOrDefault(round => round.ProblemId == problemId)
        ?? throw new InvalidOperationException($"Game has no round for problem '{problemId}'.");

    public void AttachScheduledExpiry(long sequenceNumber)
    {
        ScheduledExpirySequenceNumber = sequenceNumber;
    }

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

    public long? ScheduledExpirySequenceNumber { get; private set; }

    public Guid PoolId { get; private set; }

    public IReadOnlyCollection<GameParticipant> Participants => _participants.AsReadOnly();

    public IReadOnlyCollection<GameTrack> Tracks => _tracks.AsReadOnly();

    public IReadOnlyCollection<GameRound> Rounds => _rounds.AsReadOnly();

    private readonly List<GameParticipant> _participants = [];
    private readonly List<GameTrack> _tracks = [];
    private readonly List<GameRound> _rounds = [];
}