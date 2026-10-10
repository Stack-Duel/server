using StackDuel.Domain.Games.Enums;
using StackDuel.Domain.Games.Events;
using GameEntity = StackDuel.Domain.Games.Entities.Game;

namespace StackDuel.Domain.Tests.Games.Entities;

public class GameTests
{
    private static readonly Guid GameModeId = Guid.NewGuid();
    private static readonly Guid HostId = Guid.NewGuid();
    private static readonly Guid OpponentId = Guid.NewGuid();

    private static readonly Guid TrackId = Guid.NewGuid();

    private static GameEntity CreateRunningDuel()
    {
        GameEntity game = new(GameModeId, Guid.NewGuid(), [TrackId], [HostId], timeLimitInSeconds: 300);
        game.Join(OpponentId, maxPlayers: 2);
        game.Start();
        game.PopDomainEvents();
        return game;
    }

    private static GameEntity CreatePendingLobby(Guid hostId) =>
        new(GameModeId, Guid.NewGuid(), [TrackId], [hostId], timeLimitInSeconds: 300);

    [Fact]
    public void Constructor_GeneratesASevenCharacterJoinCode()
    {
        GameEntity game = CreatePendingLobby(HostId);

        Assert.Equal(7, game.JoinCode.Length);
    }

    [Fact]
    public void Constructor_DefaultsSkipsEnabledToTrue()
    {
        GameEntity game = CreatePendingLobby(HostId);

        Assert.True(game.SkipsEnabled);
    }

    [Fact]
    public void Constructor_SkipsEnabledFalse_IsHonored()
    {
        GameEntity game = new(
            GameModeId,
            Guid.NewGuid(),
            [TrackId],
            [HostId],
            timeLimitInSeconds: 300,
            skipsEnabled: false
        );

        Assert.False(game.SkipsEnabled);
    }

    [Fact]
    public void Start_WithCountdownSeconds_DelaysStartedAtByThatMany()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.Join(OpponentId, maxPlayers: 2);
        DateTime beforeStart = DateTime.UtcNow;

        game.Start(countdownSeconds: 6);

        Assert.True(game.StartedAt >= beforeStart.AddSeconds(6));
    }

    [Fact]
    public void Start_RaisesGameStartedAndGameLobbyUpdatedDomainEvents()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.Join(OpponentId, maxPlayers: 2);
        game.PopDomainEvents();

        game.Start();

        var domainEvents = game.PopDomainEvents();
        Assert.Equal(game.Id, domainEvents.OfType<GameStartedDomainEvent>().Single().GameId);
        // participants still sitting in the lobby need a push to learn the game just started,
        // not just the players who joined/left it
        Assert.Equal(game.Id, domainEvents.OfType<GameLobbyUpdatedDomainEvent>().Single().GameId);
    }

    [Fact]
    public void RecordProblemSolved_IncrementsOnlyThatParticipantsScore()
    {
        GameEntity game = CreateRunningDuel();

        game.RecordProblemSolved(OpponentId);

        Assert.Equal(1, game.Participants.Single(p => p.UserId == OpponentId).Score);
        Assert.Equal(0, game.Participants.Single(p => p.UserId == HostId).Score);
    }

    [Fact]
    public void RecordProblemSolved_RaisesGameProgressUpdatedDomainEvent()
    {
        GameEntity game = CreateRunningDuel();

        game.RecordProblemSolved(OpponentId);

        var domainEvent = game.PopDomainEvents().OfType<GameProgressUpdatedDomainEvent>().Single();
        Assert.Equal(game.Id, domainEvent.GameId);
    }

    [Fact]
    public void RecordProblemSolved_AccumulatesAcrossMultipleCalls()
    {
        GameEntity game = CreateRunningDuel();

        game.RecordProblemSolved(OpponentId);
        game.RecordProblemSolved(OpponentId);
        game.RecordProblemSolved(OpponentId);

        Assert.Equal(3, game.Participants.Single(p => p.UserId == OpponentId).Score);
    }

    [Fact]
    public void RecordProblemSolved_GameNotRunning_Throws()
    {
        GameEntity game = CreatePendingLobby(HostId);

        Assert.Throws<InvalidOperationException>(() => game.RecordProblemSolved(HostId));
    }

    [Fact]
    public void RecordProblemSolved_UnknownUser_Throws()
    {
        GameEntity game = CreateRunningDuel();

        Assert.Throws<InvalidOperationException>(() => game.RecordProblemSolved(Guid.NewGuid()));
    }

    [Fact]
    public void Forfeit_OneOfTwoParticipants_MarksThatParticipantForfeited_GameStaysRunning()
    {
        GameEntity game = CreateRunningDuel();

        game.Forfeit(OpponentId);

        Assert.Equal(GameStatus.Running, game.Status);
        Assert.Null(game.EndedAt);
        Assert.True(game.Participants.Single(p => p.UserId == OpponentId).HasForfeited);
        Assert.False(game.Participants.Single(p => p.UserId == HostId).HasForfeited);
    }

    [Fact]
    public void Forfeit_OneOfTwoParticipants_DoesNotRaiseGameCompletedDomainEvent()
    {
        GameEntity game = CreateRunningDuel();

        game.Forfeit(OpponentId);

        Assert.Empty(game.PopDomainEvents());
    }

    [Fact]
    public void Forfeit_EveryParticipant_CompletesTheGame()
    {
        GameEntity game = CreateRunningDuel();

        game.Forfeit(OpponentId);
        game.Forfeit(HostId);

        Assert.Equal(GameStatus.Completed, game.Status);
        Assert.NotNull(game.EndedAt);
    }

    [Fact]
    public void Forfeit_EveryParticipant_RaisesGameCompletedDomainEvent()
    {
        GameEntity game = CreateRunningDuel();

        game.Forfeit(OpponentId);
        game.Forfeit(HostId);

        var domainEvent = game.PopDomainEvents().OfType<GameCompletedDomainEvent>().Single();
        Assert.Equal(GameStatus.Completed, domainEvent.Status);
    }

    [Fact]
    public void Forfeit_GameNotRunning_Throws()
    {
        GameEntity game = new(GameModeId, Guid.NewGuid(), [TrackId], [HostId], timeLimitInSeconds: 300);

        Assert.Throws<InvalidOperationException>(() => game.Forfeit(HostId));
    }

    [Fact]
    public void Forfeit_UnknownUser_Throws()
    {
        GameEntity game = CreateRunningDuel();

        Assert.Throws<InvalidOperationException>(() => game.Forfeit(Guid.NewGuid()));
    }

    [Fact]
    public void Forfeit_SameParticipantTwice_Throws()
    {
        GameEntity game = CreateRunningDuel();
        game.Forfeit(OpponentId);

        Assert.Throws<InvalidOperationException>(() => game.Forfeit(OpponentId));
    }

    [Fact]
    public void Forfeit_ParticipantWhoAlreadyFinishedProblems_Throws()
    {
        GameEntity game = CreateRunningDuel();
        game.FinishProblemsFor(OpponentId);

        Assert.Throws<InvalidOperationException>(() => game.Forfeit(OpponentId));
    }

    [Fact]
    public void FinishProblemsFor_OneOfTwoParticipants_MarksThatParticipantFinished_GameStaysRunning()
    {
        GameEntity game = CreateRunningDuel();

        game.FinishProblemsFor(OpponentId);

        Assert.Equal(GameStatus.Running, game.Status);
        Assert.Null(game.EndedAt);
        Assert.True(game.Participants.Single(p => p.UserId == OpponentId).HasFinishedProblems);
        Assert.False(game.Participants.Single(p => p.UserId == HostId).HasFinishedProblems);
    }

    [Fact]
    public void FinishProblemsFor_EveryParticipant_CompletesTheGame()
    {
        GameEntity game = CreateRunningDuel();

        game.FinishProblemsFor(OpponentId);
        game.FinishProblemsFor(HostId);

        Assert.Equal(GameStatus.Completed, game.Status);
        Assert.NotNull(game.EndedAt);
    }

    [Fact]
    public void FinishProblemsFor_EveryParticipant_RaisesGameCompletedDomainEvent()
    {
        GameEntity game = CreateRunningDuel();

        game.FinishProblemsFor(OpponentId);
        game.FinishProblemsFor(HostId);

        var domainEvent = game.PopDomainEvents().OfType<GameCompletedDomainEvent>().Single();
        Assert.Equal(GameStatus.Completed, domainEvent.Status);
    }

    [Fact]
    public void FinishProblemsFor_MixedWithForfeit_CoveringEveryone_CompletesTheGame()
    {
        GameEntity game = CreateRunningDuel();

        game.Forfeit(OpponentId);
        game.FinishProblemsFor(HostId);

        Assert.Equal(GameStatus.Completed, game.Status);
    }

    [Fact]
    public void FinishProblemsFor_GameNotRunning_Throws()
    {
        GameEntity game = new(GameModeId, Guid.NewGuid(), [TrackId], [HostId], timeLimitInSeconds: 300);

        Assert.Throws<InvalidOperationException>(() => game.FinishProblemsFor(HostId));
    }

    [Fact]
    public void FinishProblemsFor_UnknownUser_Throws()
    {
        GameEntity game = CreateRunningDuel();

        Assert.Throws<InvalidOperationException>(() => game.FinishProblemsFor(Guid.NewGuid()));
    }

    [Fact]
    public void FinishProblemsFor_SameParticipantTwice_Throws()
    {
        GameEntity game = CreateRunningDuel();
        game.FinishProblemsFor(OpponentId);

        Assert.Throws<InvalidOperationException>(() => game.FinishProblemsFor(OpponentId));
    }

    [Fact]
    public void FinishProblemsFor_ParticipantWhoAlreadyForfeited_Throws()
    {
        GameEntity game = CreateRunningDuel();
        game.Forfeit(OpponentId);

        Assert.Throws<InvalidOperationException>(() => game.FinishProblemsFor(OpponentId));
    }

    [Fact]
    public void CompleteIfEveryoneStopped_NotEveryoneStopped_ReturnsFalse_GameStaysRunning()
    {
        GameEntity game = CreateRunningDuel();
        game.Forfeit(OpponentId);

        bool completed = game.CompleteIfEveryoneStopped();

        Assert.False(completed);
        Assert.Equal(GameStatus.Running, game.Status);
    }

    [Fact]
    public void CompleteIfEveryoneStopped_EveryoneStopped_CompletesGame_AndReturnsTrue()
    {
        GameEntity game = CreateRunningDuel();
        // Simulates the race this method exists to close: two concurrent Forfeit/FinishProblemsFor
        // calls, each reading the game before the other's write lands, so neither one's own
        // internal "is everyone done" check ever sees the full picture — mutating the participants
        // directly here (bypassing Game.Forfeit/FinishProblemsFor's own auto-complete) reproduces
        // that end state: everyone individually stopped, but the game itself never got completed.
        game.Participants.Single(p => p.UserId == OpponentId).Forfeit();
        game.Participants.Single(p => p.UserId == HostId).FinishProblems();

        bool completed = game.CompleteIfEveryoneStopped();

        Assert.True(completed);
        Assert.Equal(GameStatus.Completed, game.Status);
    }

    [Fact]
    public void CompleteIfEveryoneStopped_GameNotRunning_ReturnsFalse()
    {
        GameEntity game = new(GameModeId, Guid.NewGuid(), [TrackId], [HostId], timeLimitInSeconds: 300);

        Assert.False(game.CompleteIfEveryoneStopped());
    }

    [Fact]
    public void HostUserId_IsWhoeverHasTheLowestSeat()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.Join(OpponentId, maxPlayers: 3);

        Assert.Equal(HostId, game.HostUserId);
    }

    [Fact]
    public void Join_RaisesGameLobbyUpdatedDomainEvent()
    {
        GameEntity game = CreatePendingLobby(HostId);

        game.Join(OpponentId, maxPlayers: 2);

        var domainEvent = game.PopDomainEvents().OfType<GameLobbyUpdatedDomainEvent>().Single();
        Assert.Equal(game.Id, domainEvent.GameId);
    }

    [Fact]
    public void Leave_WithRemainingParticipants_RaisesGameLobbyUpdatedDomainEvent()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.Join(OpponentId, maxPlayers: 2);
        game.PopDomainEvents();

        game.Leave(OpponentId);

        var domainEvent = game.PopDomainEvents().OfType<GameLobbyUpdatedDomainEvent>().Single();
        Assert.Equal(game.Id, domainEvent.GameId);
    }

    [Fact]
    public void Leave_LastParticipant_DoesNotRaiseGameLobbyUpdatedDomainEvent()
    {
        GameEntity game = CreatePendingLobby(HostId);

        game.Leave(HostId);

        Assert.Empty(game.PopDomainEvents().OfType<GameLobbyUpdatedDomainEvent>());
    }

    [Fact]
    public void Leave_NonHostParticipant_RemovesThem_HostUnchanged()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.Join(OpponentId, maxPlayers: 2);

        game.Leave(OpponentId);

        Assert.DoesNotContain(OpponentId, game.Participants.Select(p => p.UserId));
        Assert.Equal(HostId, game.HostUserId);
        Assert.Equal(GameStatus.Pending, game.Status);
    }

    [Fact]
    public void Leave_HostParticipant_HandsHostToNextLowestSeat_WithoutRenumbering()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.Join(OpponentId, maxPlayers: 3);
        Guid thirdPlayerId = Guid.NewGuid();
        game.Join(thirdPlayerId, maxPlayers: 3);

        game.Leave(HostId);

        Assert.Equal(OpponentId, game.HostUserId);
        // seats are never renumbered — the new host keeps the seat they already had
        Assert.Equal(2, game.Participants.Single(p => p.UserId == OpponentId).SeatNo);
    }

    [Fact]
    public void Leave_LastParticipant_CancelsTheGame()
    {
        GameEntity game = CreatePendingLobby(HostId);

        game.Leave(HostId);

        Assert.Equal(GameStatus.Cancelled, game.Status);
        Assert.Empty(game.Participants);
        Assert.Null(game.HostUserId);
    }

    [Fact]
    public void Join_AfterHostLeaves_AssignsASeatThatDoesNotCollide()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.Join(OpponentId, maxPlayers: 3);
        // OpponentId is seat 2. HostId (seat 1) leaves, freeing seat 1 — but it must not be
        // reissued, since seat 2 is still taken and _participants.Count + 1 would collide with it.
        game.Leave(HostId);

        Guid thirdPlayerId = Guid.NewGuid();
        game.Join(thirdPlayerId, maxPlayers: 3);

        var seats = game.Participants.Select(p => p.SeatNo).ToList();
        Assert.Equivalent(new[] { 2, 3 }, seats, strict: true);
        Assert.Distinct(seats);
    }

    [Fact]
    public void Leave_RunningGame_Throws()
    {
        GameEntity game = CreateRunningDuel();

        Assert.Throws<InvalidOperationException>(() => game.Leave(HostId));
    }

    [Fact]
    public void Leave_UnknownUser_Throws()
    {
        GameEntity game = CreatePendingLobby(HostId);

        Assert.Throws<InvalidOperationException>(() => game.Leave(Guid.NewGuid()));
    }

    [Fact]
    public void CloseLobby_CancelsTheGame_EvenWithParticipantsRemaining()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.Join(OpponentId, maxPlayers: 2);

        game.CloseLobby();

        Assert.Equal(GameStatus.Cancelled, game.Status);
        Assert.NotNull(game.EndedAt);
        // closing doesn't clear the roster, just ends the game
        Assert.Equal(2, game.Participants.Count);
    }

    [Fact]
    public void CloseLobby_RaisesGameLobbyUpdatedDomainEvent()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.Join(OpponentId, maxPlayers: 2);
        game.PopDomainEvents();

        game.CloseLobby();

        var domainEvent = game.PopDomainEvents().OfType<GameLobbyUpdatedDomainEvent>().Single();
        Assert.Equal(game.Id, domainEvent.GameId);
    }

    [Fact]
    public void CloseLobby_RunningGame_Throws()
    {
        GameEntity game = CreateRunningDuel();

        Assert.Throws<InvalidOperationException>(() => game.CloseLobby());
    }

    [Fact]
    public void CloseLobby_AlreadyClosed_Throws()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.CloseLobby();

        Assert.Throws<InvalidOperationException>(() => game.CloseLobby());
    }

    [Fact]
    public void ProblemIdAtPosition_NothingAppendedYet_ReturnsNull()
    {
        GameEntity game = CreatePendingLobby(HostId);

        Assert.Null(game.ProblemIdAtPosition(0));
    }

    [Fact]
    public void AppendProblem_FirstCall_OccupiesPositionZero()
    {
        GameEntity game = CreatePendingLobby(HostId);
        var problemId = Guid.NewGuid();

        game.AppendProblem(problemId);

        Assert.Equal(problemId, game.ProblemIdAtPosition(0));
    }

    [Fact]
    public void AppendProblem_SecondCall_OccupiesTheNextPosition()
    {
        GameEntity game = CreatePendingLobby(HostId);
        var firstProblemId = Guid.NewGuid();
        var secondProblemId = Guid.NewGuid();

        game.AppendProblem(firstProblemId);
        game.AppendProblem(secondProblemId);

        Assert.Equal(firstProblemId, game.ProblemIdAtPosition(0));
        Assert.Equal(secondProblemId, game.ProblemIdAtPosition(1));
    }

    [Fact]
    public void AppendProblem_EmptyGuid_Throws()
    {
        GameEntity game = CreatePendingLobby(HostId);

        Assert.Throws<ArgumentException>(() => game.AppendProblem(Guid.Empty));
    }

    [Fact]
    public void EvaluateProblemAttempt_UserNotParticipant_ReturnsParticipantNotFound()
    {
        GameEntity game = CreateRunningDuel();

        var attempt = game.EvaluateProblemAttempt(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ParticipantAction.CompleteProblem,
            DateTime.UtcNow
        );

        Assert.Equal(ProblemAttemptEligibility.ParticipantNotFound, attempt.Eligibility);
    }

    [Fact]
    public void EvaluateProblemAttempt_GameNotRunning_ReturnsGameNotRunning()
    {
        GameEntity game = CreatePendingLobby(HostId);

        var attempt = game.EvaluateProblemAttempt(
            HostId,
            Guid.NewGuid(),
            ParticipantAction.CompleteProblem,
            DateTime.UtcNow
        );

        Assert.Equal(ProblemAttemptEligibility.GameNotRunning, attempt.Eligibility);
    }

    [Fact]
    public void EvaluateProblemAttempt_ParticipantOnCurrentProblem_ReturnsEligible()
    {
        GameEntity game = CreateRunningDuel();
        var participant = game.Participants.First(p => p.UserId == HostId);
        var problemId = Guid.NewGuid();
        participant.InitializeProblemSession(problemId);

        var attempt = game.EvaluateProblemAttempt(
            HostId,
            problemId,
            ParticipantAction.CompleteProblem,
            DateTime.UtcNow
        );

        Assert.True(attempt.IsEligible);
        Assert.Same(participant, attempt.Participant);
    }

    [Fact]
    public void EvaluateProblemAttempt_ProblemIdDoesNotMatchCurrentProblem_ReturnsProblemMismatch()
    {
        GameEntity game = CreateRunningDuel();
        var participant = game.Participants.First(p => p.UserId == HostId);
        participant.InitializeProblemSession(Guid.NewGuid());

        var attempt = game.EvaluateProblemAttempt(
            HostId,
            Guid.NewGuid(),
            ParticipantAction.CompleteProblem,
            DateTime.UtcNow
        );

        Assert.Equal(ProblemAttemptEligibility.ProblemMismatch, attempt.Eligibility);
    }

    [Fact]
    public void EvaluateProblemAttempt_GameClockHasElapsed_ReportsGameJustExpiredAndGameNotRunning()
    {
        GameEntity game = CreateRunningDuel();
        DateTime pastExpiry = game.StartedAt!.Value.AddSeconds(301);

        var attempt = game.EvaluateProblemAttempt(
            HostId,
            Guid.NewGuid(),
            ParticipantAction.CompleteProblem,
            pastExpiry
        );

        Assert.True(attempt.GameJustExpired);
        Assert.Equal(ProblemAttemptEligibility.GameNotRunning, attempt.Eligibility);
        Assert.Equal(GameStatus.Completed, game.Status);
    }
}