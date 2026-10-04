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

    [Test]
    public void Constructor_GeneratesASevenCharacterJoinCode()
    {
        GameEntity game = CreatePendingLobby(HostId);

        Assert.That(game.JoinCode, Has.Length.EqualTo(7));
    }

    [Test]
    public void Constructor_DefaultsSkipsEnabledToTrue()
    {
        GameEntity game = CreatePendingLobby(HostId);

        Assert.That(game.SkipsEnabled, Is.True);
    }

    [Test]
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

        Assert.That(game.SkipsEnabled, Is.False);
    }

    [Test]
    public void Start_WithCountdownSeconds_DelaysStartedAtByThatMany()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.Join(OpponentId, maxPlayers: 2);
        DateTime beforeStart = DateTime.UtcNow;

        game.Start(countdownSeconds: 6);

        Assert.That(game.StartedAt, Is.GreaterThanOrEqualTo(beforeStart.AddSeconds(6)));
    }

    [Test]
    public void Start_RaisesGameStartedAndGameLobbyUpdatedDomainEvents()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.Join(OpponentId, maxPlayers: 2);
        game.PopDomainEvents();

        game.Start();

        var domainEvents = game.PopDomainEvents();
        Assert.That(domainEvents.OfType<GameStartedDomainEvent>().Single().GameId, Is.EqualTo(game.Id));
        Assert.That(
            domainEvents.OfType<GameLobbyUpdatedDomainEvent>().Single().GameId,
            Is.EqualTo(game.Id),
            "participants still sitting in the lobby need a push to learn the game just started, "
                + "not just the players who joined/left it"
        );
    }

    [Test]
    public void RecordProblemSolved_IncrementsOnlyThatParticipantsScore()
    {
        GameEntity game = CreateRunningDuel();

        game.RecordProblemSolved(OpponentId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.Participants.Single(p => p.UserId == OpponentId).Score, Is.EqualTo(1));
            Assert.That(game.Participants.Single(p => p.UserId == HostId).Score, Is.EqualTo(0));
        }
    }

    [Test]
    public void RecordProblemSolved_RaisesGameProgressUpdatedDomainEvent()
    {
        GameEntity game = CreateRunningDuel();

        game.RecordProblemSolved(OpponentId);

        var domainEvent = game.PopDomainEvents().OfType<GameProgressUpdatedDomainEvent>().Single();
        Assert.That(domainEvent.GameId, Is.EqualTo(game.Id));
    }

    [Test]
    public void RecordProblemSolved_AccumulatesAcrossMultipleCalls()
    {
        GameEntity game = CreateRunningDuel();

        game.RecordProblemSolved(OpponentId);
        game.RecordProblemSolved(OpponentId);
        game.RecordProblemSolved(OpponentId);

        Assert.That(game.Participants.Single(p => p.UserId == OpponentId).Score, Is.EqualTo(3));
    }

    [Test]
    public void RecordProblemSolved_GameNotRunning_Throws()
    {
        GameEntity game = CreatePendingLobby(HostId);

        Assert.Throws<InvalidOperationException>(() => game.RecordProblemSolved(HostId));
    }

    [Test]
    public void RecordProblemSolved_UnknownUser_Throws()
    {
        GameEntity game = CreateRunningDuel();

        Assert.Throws<InvalidOperationException>(() => game.RecordProblemSolved(Guid.NewGuid()));
    }

    [Test]
    public void Forfeit_OneOfTwoParticipants_MarksThatParticipantForfeited_GameStaysRunning()
    {
        GameEntity game = CreateRunningDuel();

        game.Forfeit(OpponentId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.Status, Is.EqualTo(GameStatus.Running));
            Assert.That(game.EndedAt, Is.Null);
            Assert.That(game.Participants.Single(p => p.UserId == OpponentId).HasForfeited, Is.True);
            Assert.That(game.Participants.Single(p => p.UserId == HostId).HasForfeited, Is.False);
        }
    }

    [Test]
    public void Forfeit_OneOfTwoParticipants_DoesNotRaiseGameCompletedDomainEvent()
    {
        GameEntity game = CreateRunningDuel();

        game.Forfeit(OpponentId);

        Assert.That(game.PopDomainEvents(), Is.Empty);
    }

    [Test]
    public void Forfeit_EveryParticipant_CompletesTheGame()
    {
        GameEntity game = CreateRunningDuel();

        game.Forfeit(OpponentId);
        game.Forfeit(HostId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.Status, Is.EqualTo(GameStatus.Completed));
            Assert.That(game.EndedAt, Is.Not.Null);
        }
    }

    [Test]
    public void Forfeit_EveryParticipant_RaisesGameCompletedDomainEvent()
    {
        GameEntity game = CreateRunningDuel();

        game.Forfeit(OpponentId);
        game.Forfeit(HostId);

        var domainEvent = game.PopDomainEvents().OfType<GameCompletedDomainEvent>().Single();
        Assert.That(domainEvent.Status, Is.EqualTo(GameStatus.Completed));
    }

    [Test]
    public void Forfeit_GameNotRunning_Throws()
    {
        GameEntity game = new(GameModeId, Guid.NewGuid(), [TrackId], [HostId], timeLimitInSeconds: 300);

        Assert.Throws<InvalidOperationException>(() => game.Forfeit(HostId));
    }

    [Test]
    public void Forfeit_UnknownUser_Throws()
    {
        GameEntity game = CreateRunningDuel();

        Assert.Throws<InvalidOperationException>(() => game.Forfeit(Guid.NewGuid()));
    }

    [Test]
    public void Forfeit_SameParticipantTwice_Throws()
    {
        GameEntity game = CreateRunningDuel();
        game.Forfeit(OpponentId);

        Assert.Throws<InvalidOperationException>(() => game.Forfeit(OpponentId));
    }

    [Test]
    public void Forfeit_ParticipantWhoAlreadyFinishedProblems_Throws()
    {
        GameEntity game = CreateRunningDuel();
        game.FinishProblemsFor(OpponentId);

        Assert.Throws<InvalidOperationException>(() => game.Forfeit(OpponentId));
    }

    [Test]
    public void FinishProblemsFor_OneOfTwoParticipants_MarksThatParticipantFinished_GameStaysRunning()
    {
        GameEntity game = CreateRunningDuel();

        game.FinishProblemsFor(OpponentId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.Status, Is.EqualTo(GameStatus.Running));
            Assert.That(game.EndedAt, Is.Null);
            Assert.That(game.Participants.Single(p => p.UserId == OpponentId).HasFinishedProblems, Is.True);
            Assert.That(game.Participants.Single(p => p.UserId == HostId).HasFinishedProblems, Is.False);
        }
    }

    [Test]
    public void FinishProblemsFor_EveryParticipant_CompletesTheGame()
    {
        GameEntity game = CreateRunningDuel();

        game.FinishProblemsFor(OpponentId);
        game.FinishProblemsFor(HostId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.Status, Is.EqualTo(GameStatus.Completed));
            Assert.That(game.EndedAt, Is.Not.Null);
        }
    }

    [Test]
    public void FinishProblemsFor_EveryParticipant_RaisesGameCompletedDomainEvent()
    {
        GameEntity game = CreateRunningDuel();

        game.FinishProblemsFor(OpponentId);
        game.FinishProblemsFor(HostId);

        var domainEvent = game.PopDomainEvents().OfType<GameCompletedDomainEvent>().Single();
        Assert.That(domainEvent.Status, Is.EqualTo(GameStatus.Completed));
    }

    [Test]
    public void FinishProblemsFor_MixedWithForfeit_CoveringEveryone_CompletesTheGame()
    {
        GameEntity game = CreateRunningDuel();

        game.Forfeit(OpponentId);
        game.FinishProblemsFor(HostId);

        Assert.That(game.Status, Is.EqualTo(GameStatus.Completed));
    }

    [Test]
    public void FinishProblemsFor_GameNotRunning_Throws()
    {
        GameEntity game = new(GameModeId, Guid.NewGuid(), [TrackId], [HostId], timeLimitInSeconds: 300);

        Assert.Throws<InvalidOperationException>(() => game.FinishProblemsFor(HostId));
    }

    [Test]
    public void FinishProblemsFor_UnknownUser_Throws()
    {
        GameEntity game = CreateRunningDuel();

        Assert.Throws<InvalidOperationException>(() => game.FinishProblemsFor(Guid.NewGuid()));
    }

    [Test]
    public void FinishProblemsFor_SameParticipantTwice_Throws()
    {
        GameEntity game = CreateRunningDuel();
        game.FinishProblemsFor(OpponentId);

        Assert.Throws<InvalidOperationException>(() => game.FinishProblemsFor(OpponentId));
    }

    [Test]
    public void FinishProblemsFor_ParticipantWhoAlreadyForfeited_Throws()
    {
        GameEntity game = CreateRunningDuel();
        game.Forfeit(OpponentId);

        Assert.Throws<InvalidOperationException>(() => game.FinishProblemsFor(OpponentId));
    }

    [Test]
    public void CompleteIfEveryoneStopped_NotEveryoneStopped_ReturnsFalse_GameStaysRunning()
    {
        GameEntity game = CreateRunningDuel();
        game.Forfeit(OpponentId);

        bool completed = game.CompleteIfEveryoneStopped();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(completed, Is.False);
            Assert.That(game.Status, Is.EqualTo(GameStatus.Running));
        }
    }

    [Test]
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

        using (Assert.EnterMultipleScope())
        {
            Assert.That(completed, Is.True);
            Assert.That(game.Status, Is.EqualTo(GameStatus.Completed));
        }
    }

    [Test]
    public void CompleteIfEveryoneStopped_GameNotRunning_ReturnsFalse()
    {
        GameEntity game = new(GameModeId, Guid.NewGuid(), [TrackId], [HostId], timeLimitInSeconds: 300);

        Assert.That(game.CompleteIfEveryoneStopped(), Is.False);
    }

    [Test]
    public void HostUserId_IsWhoeverHasTheLowestSeat()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.Join(OpponentId, maxPlayers: 3);

        Assert.That(game.HostUserId, Is.EqualTo(HostId));
    }

    [Test]
    public void Join_RaisesGameLobbyUpdatedDomainEvent()
    {
        GameEntity game = CreatePendingLobby(HostId);

        game.Join(OpponentId, maxPlayers: 2);

        var domainEvent = game.PopDomainEvents().OfType<GameLobbyUpdatedDomainEvent>().Single();
        Assert.That(domainEvent.GameId, Is.EqualTo(game.Id));
    }

    [Test]
    public void Leave_WithRemainingParticipants_RaisesGameLobbyUpdatedDomainEvent()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.Join(OpponentId, maxPlayers: 2);
        game.PopDomainEvents();

        game.Leave(OpponentId);

        var domainEvent = game.PopDomainEvents().OfType<GameLobbyUpdatedDomainEvent>().Single();
        Assert.That(domainEvent.GameId, Is.EqualTo(game.Id));
    }

    [Test]
    public void Leave_LastParticipant_DoesNotRaiseGameLobbyUpdatedDomainEvent()
    {
        GameEntity game = CreatePendingLobby(HostId);

        game.Leave(HostId);

        Assert.That(game.PopDomainEvents().OfType<GameLobbyUpdatedDomainEvent>(), Is.Empty);
    }

    [Test]
    public void Leave_NonHostParticipant_RemovesThem_HostUnchanged()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.Join(OpponentId, maxPlayers: 2);

        game.Leave(OpponentId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.Participants.Select(p => p.UserId), Does.Not.Contain(OpponentId));
            Assert.That(game.HostUserId, Is.EqualTo(HostId));
            Assert.That(game.Status, Is.EqualTo(GameStatus.Pending));
        }
    }

    [Test]
    public void Leave_HostParticipant_HandsHostToNextLowestSeat_WithoutRenumbering()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.Join(OpponentId, maxPlayers: 3);
        Guid thirdPlayerId = Guid.NewGuid();
        game.Join(thirdPlayerId, maxPlayers: 3);

        game.Leave(HostId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.HostUserId, Is.EqualTo(OpponentId));
            Assert.That(
                game.Participants.Single(p => p.UserId == OpponentId).SeatNo,
                Is.EqualTo(2),
                "seats are never renumbered — the new host keeps the seat they already had"
            );
        }
    }

    [Test]
    public void Leave_LastParticipant_CancelsTheGame()
    {
        GameEntity game = CreatePendingLobby(HostId);

        game.Leave(HostId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.Status, Is.EqualTo(GameStatus.Cancelled));
            Assert.That(game.Participants, Is.Empty);
            Assert.That(game.HostUserId, Is.Null);
        }
    }

    [Test]
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
        using (Assert.EnterMultipleScope())
        {
            Assert.That(seats, Is.EquivalentTo(new[] { 2, 3 }));
            Assert.That(seats, Is.Unique);
        }
    }

    [Test]
    public void Leave_RunningGame_Throws()
    {
        GameEntity game = CreateRunningDuel();

        Assert.Throws<InvalidOperationException>(() => game.Leave(HostId));
    }

    [Test]
    public void Leave_UnknownUser_Throws()
    {
        GameEntity game = CreatePendingLobby(HostId);

        Assert.Throws<InvalidOperationException>(() => game.Leave(Guid.NewGuid()));
    }

    [Test]
    public void CloseLobby_CancelsTheGame_EvenWithParticipantsRemaining()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.Join(OpponentId, maxPlayers: 2);

        game.CloseLobby();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(game.Status, Is.EqualTo(GameStatus.Cancelled));
            Assert.That(game.EndedAt, Is.Not.Null);
            Assert.That(
                game.Participants,
                Has.Count.EqualTo(2),
                "closing doesn't clear the roster, just ends the game"
            );
        }
    }

    [Test]
    public void CloseLobby_RaisesGameLobbyUpdatedDomainEvent()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.Join(OpponentId, maxPlayers: 2);
        game.PopDomainEvents();

        game.CloseLobby();

        var domainEvent = game.PopDomainEvents().OfType<GameLobbyUpdatedDomainEvent>().Single();
        Assert.That(domainEvent.GameId, Is.EqualTo(game.Id));
    }

    [Test]
    public void CloseLobby_RunningGame_Throws()
    {
        GameEntity game = CreateRunningDuel();

        Assert.Throws<InvalidOperationException>(() => game.CloseLobby());
    }

    [Test]
    public void CloseLobby_AlreadyClosed_Throws()
    {
        GameEntity game = CreatePendingLobby(HostId);
        game.CloseLobby();

        Assert.Throws<InvalidOperationException>(() => game.CloseLobby());
    }

    [Test]
    public void ProblemIdAtPosition_NothingAppendedYet_ReturnsNull()
    {
        GameEntity game = CreatePendingLobby(HostId);

        Assert.That(game.ProblemIdAtPosition(0), Is.Null);
    }

    [Test]
    public void AppendProblem_FirstCall_OccupiesPositionZero()
    {
        GameEntity game = CreatePendingLobby(HostId);
        var problemId = Guid.NewGuid();

        game.AppendProblem(problemId);

        Assert.That(game.ProblemIdAtPosition(0), Is.EqualTo(problemId));
    }

    [Test]
    public void AppendProblem_SecondCall_OccupiesTheNextPosition()
    {
        GameEntity game = CreatePendingLobby(HostId);
        var firstProblemId = Guid.NewGuid();
        var secondProblemId = Guid.NewGuid();

        game.AppendProblem(firstProblemId);
        game.AppendProblem(secondProblemId);

        Assert.Multiple(() =>
        {
            Assert.That(game.ProblemIdAtPosition(0), Is.EqualTo(firstProblemId));
            Assert.That(game.ProblemIdAtPosition(1), Is.EqualTo(secondProblemId));
        });
    }

    [Test]
    public void AppendProblem_EmptyGuid_Throws()
    {
        GameEntity game = CreatePendingLobby(HostId);

        Assert.Throws<ArgumentException>(() => game.AppendProblem(Guid.Empty));
    }

    [Test]
    public void EvaluateProblemAttempt_UserNotParticipant_ReturnsParticipantNotFound()
    {
        GameEntity game = CreateRunningDuel();

        var attempt = game.EvaluateProblemAttempt(
            Guid.NewGuid(),
            Guid.NewGuid(),
            ParticipantAction.CompleteProblem,
            DateTime.UtcNow
        );

        Assert.That(attempt.Eligibility, Is.EqualTo(ProblemAttemptEligibility.ParticipantNotFound));
    }

    [Test]
    public void EvaluateProblemAttempt_GameNotRunning_ReturnsGameNotRunning()
    {
        GameEntity game = CreatePendingLobby(HostId);

        var attempt = game.EvaluateProblemAttempt(
            HostId,
            Guid.NewGuid(),
            ParticipantAction.CompleteProblem,
            DateTime.UtcNow
        );

        Assert.That(attempt.Eligibility, Is.EqualTo(ProblemAttemptEligibility.GameNotRunning));
    }

    [Test]
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

        Assert.Multiple(() =>
        {
            Assert.That(attempt.IsEligible, Is.True);
            Assert.That(attempt.Participant, Is.SameAs(participant));
        });
    }

    [Test]
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

        Assert.That(attempt.Eligibility, Is.EqualTo(ProblemAttemptEligibility.ProblemMismatch));
    }

    [Test]
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

        Assert.Multiple(() =>
        {
            Assert.That(attempt.GameJustExpired, Is.True);
            Assert.That(attempt.Eligibility, Is.EqualTo(ProblemAttemptEligibility.GameNotRunning));
            Assert.That(game.Status, Is.EqualTo(GameStatus.Completed));
        });
    }
}